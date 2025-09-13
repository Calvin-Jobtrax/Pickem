using System.Net;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pickem.Services;

namespace Pickem.Models;

public sealed class LoginViewModel : INotifyPropertyChanged
{
  // ---- Bindable state ----
  private string _username = Preferences.Get("savedUsername", string.Empty);
  public string Username { get => _username; set { _username = value; OnPropertyChanged(); } }

  private string _password = Preferences.Get("savedPassword", string.Empty);
  public string Password { get => _password; set { _password = value; OnPropertyChanged(); } }

  private string _statusMessage = string.Empty;
  public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

  private bool _isLoggingIn;
  public bool IsLoggingIn { get => _isLoggingIn; set { _isLoggingIn = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoggingIn)); OnPropertyChanged(nameof(LoginButtonText)); } }
  public bool IsNotLoggingIn => !IsLoggingIn;
  public string LoginButtonText => IsLoggingIn ? "Logging in..." : "Login";

  public string VersionFooter
  {
    get
    {
#if ANDROID
      const string platform = "Android";
#elif IOS
            const string platform = "iOS";
#else
            const string platform = "MAUI";
#endif
      var ver = AppInfo.Current.VersionString;
      var bld = AppInfo.Current.BuildString;
      var name = AppInfo.Current.Name;
      return $"{name} v{ver} ({bld}) • {platform}";
    }
  }

  // ---- Commands ----
  public ICommand LoginCommand { get; }
  public ICommand ExitCommand { get; }

  // ---- HTTP (cookie-capable) ----
  private static readonly CookieContainer CookieJar = new();
  private static readonly HttpClient Http;

  static LoginViewModel()
  {
    var handler = new HttpClientHandler
    {
      UseCookies = true,
      CookieContainer = CookieJar,
      AllowAutoRedirect = true
    };
#if ANDROID || IOS
    // If you need to accept dev certs while testing (DEBUG only!)
#if DEBUG
    handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
#endif
#endif
    Http = new HttpClient(handler)
    {
      Timeout = TimeSpan.FromSeconds(10)
    };
  }

  public LoginViewModel()
  {
    LoginCommand = new Command(async () => await LoginAsync(), () => true);
    ExitCommand = new Command(ExitApp);

    // Persist "last seen version" similar to @AppStorage in Swift
    var current = $"{AppInfo.Current.VersionString}({AppInfo.Current.BuildString})";
    var lastSeen = Preferences.Get("lastSeenVersion", string.Empty);
    if (!string.Equals(current, lastSeen, StringComparison.Ordinal))
      Preferences.Set("lastSeenVersion", current);
  }

  public event PropertyChangedEventHandler? PropertyChanged;
  private void OnPropertyChanged([CallerMemberName] string? name = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

  // ---- Logic ----
  private async Task LoginAsync()
  {
    StatusMessage = string.Empty;

    if (!NetworkMonitor.Shared.IsOnWiFi)
    {
      StatusMessage = "Local server requires Wi-Fi.";
      await Application.Current!.MainPage!.DisplayAlert(
          "Wi-Fi Required",
          "Connect to the office Wi-Fi to use the local server.",
          "OK");
      return;
    }

    if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
    {
      StatusMessage = "Please enter both username and password";
      return;
    }

    // Normalize base URL
    Uri baseUri;
    try
    {
      var baseUrl = AppConfig.Shared.PickemBaseUrl ?? string.Empty;
      if (!baseUrl.EndsWith("/")) baseUrl += "/";
      baseUri = new Uri(baseUrl);
    }
    catch
    {
      StatusMessage = "Invalid login URL";
      return;
    }

    var loginUri = new Uri(baseUri, "login");

    var form = new Dictionary<string, string>
    {
      ["username"] = Username,
      ["pwd"] = Password
    };
    using var content = new FormUrlEncodedContent(form);

    IsLoggingIn = true;
    try
    {
      using var resp = await Http.PostAsync(loginUri, content);

      if (resp.StatusCode != HttpStatusCode.OK)
      {
        var err = await resp.Content.ReadAsStringAsync();
        if (err.Length > 300) err = err[..300] + "…";
        StatusMessage = $"❌ Login failed (HTTP {(int)resp.StatusCode}). {err}";
        return;
      }

      // Try Set-Cookie first
      string? token = null;
      if (resp.Headers.TryGetValues("Set-Cookie", out var setCookies))
      {
        var m = System.Text.RegularExpressions.Regex.Match(
            string.Join("; ", setCookies), @"\btoken=([^;,\s]+)");
        if (m.Success) token = m.Groups[1].Value;
      }

      // Read body
      var body = await resp.Content.ReadAsStringAsync();

      bool okFlag = false;
      int? userId = null;
      string? name = null;

      try
      {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("ok", out var okEl) && okEl.ValueKind == JsonValueKind.True)
          okFlag = true;

        if (root.TryGetProperty("userId", out var idEl) && idEl.TryGetInt32(out var idVal))
          userId = idVal;

        if (root.TryGetProperty("name", out var nameEl))
          name = nameEl.GetString();

        if (string.IsNullOrEmpty(token) && root.TryGetProperty("token", out var tokEl))
          token = tokEl.GetString();
      }
      catch
      {
        // Not JSON — handled below
      }

      // Require either token OR ok:true
      if (string.IsNullOrEmpty(token) && !okFlag)
      {
        var preview = body?.Length > 300 ? body[..300] + "…" : body ?? "(empty)";
        StatusMessage = $"⚠️ Failed to parse login response (HTTP {(int)resp.StatusCode}). Body preview: {preview}";
        return;
      }

      // If we got a token, add it to CookieJar
      if (!string.IsNullOrEmpty(token))
      {
        var host = loginUri.Host;
        var cookie = new Cookie("token", token, "/", host)
        {
          Secure = loginUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase),
          Expires = DateTime.UtcNow.AddDays(1)
        };
        CookieJar.Add(new Uri($"{loginUri.Scheme}://{host}"), cookie);
      }

      var session = ServiceHelper.GetService<SessionService>();

      if (userId.HasValue)
      {
        Preferences.Set("currentUserId", userId.Value);
        session.PlayerId = userId.Value;   // keep session in sync
      }

      if (!string.IsNullOrEmpty(name))
      {
        Preferences.Set("currentUserName", name);
        session.UserName = name;           // keep session in sync
      }

      // Persist creds
      Preferences.Set("savedUsername", Username);
      Preferences.Set("savedPassword", Password);

      await GoToMainAsync();
    }
    catch (TaskCanceledException)
    {
      StatusMessage = "❌ Network timeout";
    }
    catch (Exception ex)
    {
      StatusMessage = $"❌ Network error: {ex.Message}";
    }
    finally
    {
      IsLoggingIn = false;
    }
  }


  private static async Task GoToMainAsync()
  {
    // If you use Shell:
    // await Shell.Current.GoToAsync("//main");
    // Or replace root page:
    Application.Current!.MainPage = new NavigationPage(new Pages.MainPage());
    await Task.CompletedTask;
  }

  private void ExitApp()
  {
    // iOS discourages programmatic exit; this matches your Swift behavior for internal apps.
#if ANDROID
    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#elif IOS
        // Not recommended for App Store apps, but mirrors exit(0) in Swift sample
        System.Diagnostics.Process.GetCurrentProcess().Kill();
#else
        Environment.Exit(0);
#endif
  }
}

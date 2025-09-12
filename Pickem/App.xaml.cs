using Pickem.Services;

namespace Pickem;

public partial class App : Application
{
    public App(ApiService api) // (ok to inject, even if unused here)
    {
        InitializeComponent();

#if DEBUG
        // Remove any old saved root and env var that force 10.0.2.2
        Preferences.Default.Remove("Pickem.BaseRoot");
        Environment.SetEnvironmentVariable("PICKEM_BASEROOT", null);

        // Hard set to prod root (ensure trailing slash)
        AppConfig.Shared.SetBaseRoot("https://jobtraxweb.com/api/");

        System.Diagnostics.Debug.WriteLine($"[App] BaseRoot = {AppConfig.Shared.BaseRoot}");
#endif

        UserAppTheme = AppTheme.Light;
        MainPage = new NavigationPage(new Pages.LoginPage());

        // Optional: quick popup if unreachable
        _ = CheckApiAsync(api);
    }

    private async Task CheckApiAsync(ApiService api)
    {
        var (ok, url, status, error) = await api.ProbeAsync();
        if (!ok)
        {
            var msg = $"Could not reach:\n{url}\n\n";
            if (status > 0) msg += $"HTTP {status}\n";
            if (!string.IsNullOrWhiteSpace(error)) msg += $"{error}\n";
            if (MainPage != null)
            {
                await MainPage.DisplayAlert("Connection problem", msg, "OK");
            }
        }
    }
}

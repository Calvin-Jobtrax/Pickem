using Pickem.Services;

namespace Pickem.Pages;

public partial class MainPage : ContentPage
{
  private readonly ApiService? _api;

  // Bindable header name
  private string _playerName = "Player";
  public string PlayerName
  {
    get => _playerName;
    set
    {
      if (_playerName == value) return;
      _playerName = value;
      // keep SessionService in sync
      ServiceHelper.GetService<SessionService>().UserName = _playerName;
      OnPropertyChanged();
    }
  }

  // Works with new MainPage()
  public MainPage()
  {
    InitializeComponent();
    BindingContext = this;
  }

  // Also supports DI if you ever call new MainPage(api)
  public MainPage(ApiService api) : this()
  {
    _api = api;
  }

  protected override void OnAppearing()
  {
    base.OnAppearing();

    var session = ServiceHelper.GetService<SessionService>();

    // Prefer live session value, fall back to saved prefs
    PlayerName = !string.IsNullOrWhiteSpace(session.UserName)
        ? session.UserName
        : Preferences.Get("currentUserName",
            Preferences.Get("savedUsername", "Player"));
  }

  private static int GetPlayerId()
  {
    var prefId = Preferences.Get("currentUserId", 0);
    return prefId;
  }

  // ---- Navigation handlers ----

  private async void OnWagers(object sender, EventArgs e)
  {
    var api = ServiceHelper.GetService<ApiService>();
    await Navigation.PushAsync(new WagersPage(api, GetPlayerId()));
  }

  private async void OnSchedule(object sender, EventArgs e)
  {
    await Navigation.PushAsync(new SchedulePage());
  }

  private async void OnStatus(object sender, EventArgs e)
  {
    var api = ServiceHelper.GetService<ApiService>();
    await Navigation.PushAsync(new StatusPage(api, GetPlayerId()));
  }

  private async void OnStandings(object sender, EventArgs e)
      => await Navigation.PushAsync(new StandingsPage());

  private async void OnRecord(object sender, EventArgs e)
      => await Navigation.PushAsync(new RecordPage());

  private async void OnExit(object sender, EventArgs e)
  {
    // Optional best-effort logout
    try
    {
      var http = ServiceHelper.GetService<IHttpClientFactory>().CreateClient("Api");
      await http.PostAsync("api/user/logout", content: null);
    }
    catch { /* ignore in debug */ }

#if ANDROID || WINDOWS
    Application.Current?.Quit();
#else
        Application.Current!.MainPage = new NavigationPage(new LoginPage());
#endif
    await Task.CompletedTask;
  }
}

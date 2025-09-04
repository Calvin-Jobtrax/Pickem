using Pickem.Helpers;
using Pickem.Services;

namespace Pickem.Pages;

public partial class MainPage : ContentPage
{
  // Optional: only needed if you want to use it later
  private readonly ApiService? _api;

  private string _playerName = "Player";
  public string PlayerName
  {
    get => _playerName;
    set
    {
      if (_playerName == value) return;
      _playerName = value;
      ServiceHelper.GetService<SessionService>().UserName = _playerName;
      OnPropertyChanged(); // MAUI's built-in notifier
    }
  }

  // Works with new MainPage()
  public MainPage()
  {
    InitializeComponent();
    BindingContext = this;
    //TitleHelper.AttachUserChip(this);  // <- adds the upper-right username chip
  }

  // Also supports DI if you ever call new MainPage(api)
  public MainPage(ApiService api) : this()
  {
    _api = api;
  }

  protected override void OnAppearing()
  {
    base.OnAppearing();
    // Show the login name you already save on successful login
    PlayerName = Preferences.Get("savedUsername", "Player");
  }

  private async void OnWagers(object sender, EventArgs e)
      => await Navigation.PushAsync(new WagerPage());

  private async void OnPool(object sender, EventArgs e)
      => await Navigation.PushAsync(new PoolPage());

  private async void OnStatus(object sender, EventArgs e)
      => await Navigation.PushAsync(new ResultsPage());

  private async void OnStandings(object sender, EventArgs e)
      => await Navigation.PushAsync(new StandingPage());

  private async void OnRecord(object sender, EventArgs e)
      => await Navigation.PushAsync(new RecordPage());

  private async void OnExit(object sender, EventArgs e)
  {
    // Optional: best-effort logout if you have an API endpoint
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

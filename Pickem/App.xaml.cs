using Pickem.Pages;

namespace Pickem;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        UserAppTheme = AppTheme.Light;

        MainPage = new NavigationPage(new LoginPage());

        _ = AppConfig.Shared.AutoSelectAsync();
    }
}


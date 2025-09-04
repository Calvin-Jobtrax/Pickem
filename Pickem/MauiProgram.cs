using System.Net.Http.Headers;
using Pickem.Services;

#if IOS
using UIKit;
#endif

namespace Pickem; // file-scoped

public static class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>()
           .ConfigureFonts(f => f.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

    // Register the named HttpClient
    var httpClientBuilder = builder.Services.AddHttpClient("Api", c =>
    {
      c.BaseAddress = new Uri(AppConfig.Shared.BaseRoot); // e.g. https://10.0.2.2:7037/
      c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });

    // Accept dev cert on Android DEBUG only
#if ANDROID && DEBUG
    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
      new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
      });
#endif

#if IOS
    // Standard (short) title — applies on pages with a TitleView (e.g., your username chip)
    // Bigger/bolder standard nav-bar title (the short bar used when TitleView is present)
    UIKit.UINavigationBar.Appearance.SetTitleTextAttributes(
        new UIKit.UITextAttributes { Font = UIKit.UIFont.BoldSystemFontOfSize(18) }
    );

    // Back button text
    var backAttrs = new UIStringAttributes { Font = UIFont.BoldSystemFontOfSize(17) };
    UIBarButtonItem.Appearance.SetTitleTextAttributes(backAttrs, UIControlState.Normal);
    UIBarButtonItem.Appearance.SetTitleTextAttributes(backAttrs, UIControlState.Highlighted);
    UIBarButtonItem.Appearance.SetTitleTextAttributes(backAttrs, UIControlState.Disabled);
#endif

    builder.Services.AddSingleton<ApiService>();
    builder.Services.AddSingleton<SessionState>();
    builder.Services.AddSingleton<NetworkMonitor>();
    builder.Services.AddSingleton<AppShell>();
    builder.Services.AddSingleton<SessionService>();

    builder.Services.AddTransient<Pages.LoginPage>();
    builder.Services.AddTransient<Pages.MainPage>();
    builder.Services.AddTransient<Pages.PoolPage>();
    builder.Services.AddTransient<Pages.RecordPage>();
    builder.Services.AddTransient<Pages.ResultsPage>();
    builder.Services.AddTransient<Pages.StandingPage>();
    builder.Services.AddTransient<Pages.WagerPage>();
    builder.Services.AddTransient<Pages.RecordPage>();

    return builder.Build();
  }
}

using System.Net.Http.Headers;
using Microsoft.Maui.Storage;            // for Preferences
using Pickem.Services;

#if IOS
using UIKit;
#endif

namespace Pickem;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .ConfigureFonts(f => f.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        // ---- FORCE PROD BASE ROOT *BEFORE* AddHttpClient ----
#if DEBUG
        // remove any old saved base root or env override that may point to 10.0.2.2
        Preferences.Default.Remove("Pickem.BaseRoot");
        Environment.SetEnvironmentVariable("PICKEM_BASEROOT", null);
#endif
        // either hardcode OR set via AppConfig
        // (A) Hardcode here (bullet-proof):
        var baseRoot = new Uri("https://jobtraxweb.com/api/");
        // (B) Or, if you want to keep AppConfig:
        // AppConfig.Shared.SetBaseRoot("https://jobtraxweb.com/api/");
        // var baseRoot = new Uri(AppConfig.Shared.BaseRoot);

        // ---- HttpClient pipeline (single registration) ----
        builder.Services.AddTransient<HttpLoggingHandler>();

        var apiClient = builder.Services.AddHttpClient("Api", c =>
        {
            c.BaseAddress = baseRoot; // now guaranteed to be prod
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.Timeout = TimeSpan.FromSeconds(30);
        });
        apiClient.AddHttpMessageHandler<HttpLoggingHandler>();

#if ANDROID && DEBUG
        apiClient.ConfigurePrimaryHttpMessageHandler(() =>
          new HttpClientHandler
          {
              ServerCertificateCustomValidationCallback =
              HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
          });
#endif

#if IOS
    UINavigationBar.Appearance.SetTitleTextAttributes(
      new UITextAttributes { Font = UIFont.BoldSystemFontOfSize(18) });
    var backAttrs = new UIStringAttributes { Font = UIFont.BoldSystemFontOfSize(17) };
    UIBarButtonItem.Appearance.SetTitleTextAttributes(backAttrs, UIControlState.Normal);
    UIBarButtonItem.Appearance.SetTitleTextAttributes(backAttrs, UIControlState.Highlighted);
    UIBarButtonItem.Appearance.SetTitleTextAttributes(backAttrs, UIControlState.Disabled);
#endif

        // Services & Pages
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<SessionState>();
        builder.Services.AddSingleton<NetworkMonitor>();
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<Pages.LoginPage>();
        builder.Services.AddTransient<Pages.MainPage>();
        builder.Services.AddTransient<Pages.SchedulePage>();
        builder.Services.AddTransient<Pages.RecordPage>();
        builder.Services.AddTransient<Pages.StatusPage>();
        builder.Services.AddTransient<Pages.StandingsPage>();
        builder.Services.AddTransient<Pages.WagersPage>();

        var app = builder.Build();

        return app;
    }
}

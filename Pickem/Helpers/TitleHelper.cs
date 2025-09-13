using Pickem.Services;

namespace Pickem.Helpers;

public static class TitleHelper
{
  public static void AttachUserChip(ContentPage page)
  {
    var session = ServiceHelper.GetService<SessionService>();

    // Slightly larger fonts on Android (no “large title” there)
    double titleSize = DeviceInfo.Platform == DevicePlatform.Android ? 20 : 17;
    double chipSize = DeviceInfo.Platform == DevicePlatform.Android ? 14 : 13;

    var grid = new Grid
    {
      ColumnDefinitions =
      {
        new ColumnDefinition(GridLength.Star),
        new ColumnDefinition(GridLength.Auto)
      },
      Padding = new Thickness(12, 0),
      // match Material toolbar height on Android so bigger text isn’t cramped
      HeightRequest = DeviceInfo.Platform == DevicePlatform.Android ? 56 : 44
    };

    var title = new Label
    {
      Text = page.Title,
      FontSize = titleSize,
      FontAttributes = FontAttributes.Bold,
      TextColor = Colors.Black,
      VerticalTextAlignment = TextAlignment.Center,
      LineBreakMode = LineBreakMode.TailTruncation
    };
    grid.Add(title, 0, 0);

    var frame = new Frame
    {
      BackgroundColor = Color.FromArgb("#EEF2FF"),
      BorderColor = Color.FromArgb("#6366F1"),
      Padding = new Thickness(10, 6),
      CornerRadius = 14,
      HasShadow = false,
      VerticalOptions = LayoutOptions.Center
    };

    var nameLabel = new Label
    {
      FontSize = chipSize,
      FontAttributes = FontAttributes.Bold,
      VerticalTextAlignment = TextAlignment.Center,
      LineBreakMode = LineBreakMode.TailTruncation,
      TextColor = Colors.Black
    };
    nameLabel.BindingContext = session;
    nameLabel.SetBinding(Label.TextProperty, nameof(SessionService.UserName));

    var tap = new TapGestureRecognizer();
    tap.Tapped += async (_, __) =>
    {
      var choice = await page.DisplayActionSheet(session.UserName, "Cancel", null, "Profile", "Logout");
      if (choice == "Logout")
      {
#if ANDROID || WINDOWS
        Application.Current?.Quit();
#else
        Application.Current!.MainPage = new NavigationPage(new Pickem.Pages.LoginPage());
#endif
      }
    };
    frame.GestureRecognizers.Add(tap);

    frame.Content = nameLabel;
    grid.Add(frame, 1, 0);

    // Support both Shell and NavigationPage
    Shell.SetTitleView(page, grid);            // ✅ correct for Shell
    NavigationPage.SetTitleView(page, grid);   // ✅ works if hosted in a NavigationPage
  }
}

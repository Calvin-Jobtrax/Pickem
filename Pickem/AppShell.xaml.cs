namespace Pickem;

public partial class AppShell : Shell
{
  public AppShell()
  {
    InitializeComponent();

    Routing.RegisterRoute("login", typeof(Pages.LoginPage));
    Routing.RegisterRoute("main", typeof(Pages.MainPage));
    Routing.RegisterRoute("pool", typeof(Pages.SchedulePage));
    Routing.RegisterRoute("record", typeof(Pages.RecordPage));
    Routing.RegisterRoute("results", typeof(Pages.StatusPage));
    Routing.RegisterRoute("standing", typeof(Pages.StandingsPage));
    Routing.RegisterRoute("wager", typeof(Pages.WagersPage));
  }
}

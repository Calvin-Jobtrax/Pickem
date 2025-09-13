namespace Pickem;

public partial class AppShell : Shell
{
  public AppShell()
  {
    InitializeComponent();

    Routing.RegisterRoute("login", typeof(Pages.LoginPage));
    Routing.RegisterRoute("main", typeof(Pages.MainPage));
    Routing.RegisterRoute("pool", typeof(Pages.PoolPage));
    Routing.RegisterRoute("record", typeof(Pages.RecordPage));
    Routing.RegisterRoute("results", typeof(Pages.ResultsPage));
    Routing.RegisterRoute("standing", typeof(Pages.StandingPage));
    Routing.RegisterRoute("wager", typeof(Pages.WagerPage));
  }
}

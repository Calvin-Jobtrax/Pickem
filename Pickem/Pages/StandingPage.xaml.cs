using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

public partial class StandingPage : ContentPage
{
  private readonly ApiService _api;
    public int Year { get; set; }
    public int Week { get; set; }

    private const int MaxWeek = 18; // align with PoolPage behavior

    public StandingPage() : this(GetApiFromDi()) { }

  public StandingPage(ApiService api)
  {
    InitializeComponent();
    _api = api;

    Year = AppConfig.SeasonYear;   // keep your default
    Week = WeekHelper.GetCurrentWeek();
  }



  protected override async void OnAppearing()
  {
    base.OnAppearing();
    WeekLabel.Text = Week.ToString();
    await LoadAsync();
  }

  private async void OnPrevWeek(object sender, EventArgs e)
  {
    if (Week > 1)
    {
      Week--;
      WeekLabel.Text = Week.ToString();
      await LoadAsync();
    }
  }

  private async void OnNextWeek(object sender, EventArgs e)
  {
    if (Week < MaxWeek)
    {
      Week++;
      WeekLabel.Text = Week.ToString();
      await LoadAsync();
    }
  }

  private async void OnReload(object sender, EventArgs e) => await LoadAsync();

  private async Task LoadAsync()
  {
    try
    {
      Busy.IsVisible = Busy.IsRunning = true;

      var rows = await _api.GetStandingsAsync(Year, Week) ?? new List<StandingRow>();

      // Compute TieBreakDiff if missing (unchanged)
      foreach (var r in rows)
      {
        r.TieBreakDiff = (r.TieBreakGuess.HasValue && r.TieBreakActual.HasValue)
            ? Math.Abs(r.TieBreakGuess.Value - r.TieBreakActual.Value)
            : (int?)null;
      }

      // alternating stripe like PoolPage vibe
      for (int i = 0; i < rows.Count; i++)
        rows[i].IsAlternate = (i % 2 == 1);

      // header
      //HeaderLabel.Text = $"Week {Week}";
      var tbActual = rows.FirstOrDefault()?.TieBreakActual;
      TieBreakActualLabel.Text = $"TB Actual • {(tbActual.HasValue ? tbActual.Value.ToString() : "•")}";

      RowsList.ItemsSource = rows;
    }
    catch (Exception ex)
    {
      await DisplayAlert("Error", ex.Message, "OK");
    }
    finally
    {
      Busy.IsVisible = Busy.IsRunning = false;
    }
  }

  private static ApiService GetApiFromDi()
  {
    var provider = Application.Current?.Handler?.MauiContext?.Services
                   ?? throw new InvalidOperationException("DI not available. Ensure services are registered in MauiProgram.");
    return provider.GetRequiredService<ApiService>();
  }
}

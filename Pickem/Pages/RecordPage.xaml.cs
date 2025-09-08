using Microsoft.Maui.Graphics; // make sure this using is present
using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

public partial class RecordPage : ContentPage
{
  private readonly ApiService _api;
  private bool _loaded;

  public RecordPage(ApiService api) { InitializeComponent(); _api = api; }
  public RecordPage() : this(Pickem.ServiceHelper.GetService<ApiService>()) { }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    if (_loaded) return;
    _loaded = true;

    try
    {
      const int year = AppConfig.SeasonYear;
      var maxWeek = await _api.GetMaxWeekAsync(year);
      var rows = await _api.GetPodiumAsync(year, maxWeek) ?? new List<PodiumRow>();

      var vm = rows.Select((r, i) => new PodiumRowVm
      {
        PlayerName = r.PlayerName,
        Firsts = r.Firsts,
        Seconds = r.Seconds,
        Thirds = r.Thirds,
        WeeksPlayed = r.WeeksPlayed,
        SeasonPoints = r.SeasonPoints,
        SeasonCorrect = r.SeasonCorrect,
        RowBackground = (i % 2 == 0)
                    ? Color.FromArgb("#E0FFFF")  // light aqua
                    : Color.FromArgb("#FCE4EC") // Pink

      }).ToList();

      PodiumList.ItemsSource = vm;
    }
    catch (Exception ex)
    {
      _loaded = false;
      await DisplayAlert("Error", ex.Message, "OK");
    }
  }

  private sealed class PodiumRowVm
  {
    public string PlayerName { get; set; } = "";
    public int Firsts { get; set; }
    public int Seconds { get; set; }
    public int Thirds { get; set; }
    public int WeeksPlayed { get; set; }
    public int SeasonPoints { get; set; }
    public int SeasonCorrect { get; set; }
    public Color RowBackground { get; set; } = Colors.Transparent;
  }
}

//private async Task Load2024Async()
//  {
//    try
//    {
//      // 1) get MaxWeek for 2024
//      var wk = await _api.GetFromJsonAsync<PickemWeekMax>($"/pickem/maxweek/{Year}",
//                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

//      var maxWeek = wk?.MaxWeek ?? 0;

//      // 2) fetch podium through that week
//      var url = $"/pickem/podium?year={Year}&maxWeek={maxWeek}";
//      var rows = await _api.GetFromJsonAsync<List<PodiumRow>>(url,
//                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
//                 ?? new List<PodiumRow>();

//      // 3) bind (UI shows no PlayerId)
//      PodiumList.ItemsSource = rows;
//    }
//    catch (Exception ex)
//    {
//      await DisplayAlert("Error", ex.Message, "OK");
//    }
//  }

//}

using System.Collections.ObjectModel;
using System.Globalization;
using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

public partial class SchedulePage : ContentPage
{
  private readonly ApiService _api;
  private int _year = 2024;   // default to 2024 for testing
  private int _week;
  private int _maxWeek = 18;

  // Flat items (kept if you still need it elsewhere)
  private readonly ObservableCollection<PoolItem> _items = new();

  // NEW: groups for CollectionView
  private readonly ObservableCollection<PoolGroup> _groups = new();

  public SchedulePage(ApiService api)
  {
    InitializeComponent();
    _api = api;

    // We now bind the grouped source
    PoolList.ItemsSource = _groups;
 
    _week = WeekHelper.GetCurrentWeek();
  }

  public SchedulePage() : this(ServiceHelper.GetService<ApiService>()) { }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    await RefreshYearAndWeekAsync();
  }

  private async Task RefreshYearAndWeekAsync()
  {
    _maxWeek = await _api.GetMaxWeekAsync(2024);
    if (_maxWeek <= 0) _maxWeek = 1;

    if (_week < 1) _week = 1;
    if (_week > _maxWeek) _week = _maxWeek;

    WeekLabel.Text = $"{_week}";
    await LoadAsync();
  }

  private async Task LoadAsync()
  {
    try
    {
      Busy.IsVisible = Busy.IsRunning = true;

      _year = AppConfig.SeasonYear;
      WeekLabel.Text = $"{_week}";

      var rows = await _api.GetPoolAsync(_year, _week) ?? new List<PoolRow>();

      // Ensure tie-break row has scores (and copy date/time so it groups cleanly)
      PrepareTieBreak(rows);

      // Map to PoolItem
      _items.Clear();
      var i = 0;
      foreach (var r in rows)
      {
        _items.Add(new PoolItem
        {
          RowIndex = i++,
          Record = r.Record,
          Year = r.Year,
          Week = r.Week,
          GameNumber = r.GameNumber,
          Visitor = r.Visitor,
          VisitorScore = r.VisitorScore,
          Home = r.Home,
          HomeScore = r.HomeScore,
          VisitorWin = r.VisitorWin,
          HomeWin = r.HomeWin,
          Result = r.Result,
          Date = r.Date,
          Time = r.Time
        });
      }

      // Build groups by GameDate
      _groups.Clear();
      var grouped = _items
        .GroupBy(p => p.GameDate)                  // DateTime date-only
        .OrderBy(g => g.Key);

      foreach (var g in grouped)
      {
        // Header like "Wed. 10"
        var header = g.Key == DateTime.MinValue
          ? ""
          : g.Key.ToString("ddd. MMM'.' d", CultureInfo.InvariantCulture);

        _groups.Add(new PoolGroup(header, g));
      }
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

  private async void OnPrevWeek(object sender, EventArgs e)
  {
    if (_week > 1) { _week--; await LoadAsync(); }
  }

  private async void OnNextWeek(object sender, EventArgs e)
  {
    if (_week < _maxWeek) { _week++; await LoadAsync(); }
  }

  private async void OnReload(object sender, EventArgs e) => await LoadAsync();

  private async void OnYearCompleted(object sender, EventArgs e)
    {
    _maxWeek = await _api.GetMaxWeekAsync(_year);
    _week = Math.Min(_week, _maxWeek);
    await LoadAsync();
  }

  private static void PrepareTieBreak(IList<PoolRow> items)
  {
    if (items == null || items.Count == 0) return;

    var tie = items.FirstOrDefault(r =>
      string.Equals(r.Visitor, "Tie Break", StringComparison.OrdinalIgnoreCase));
    if (tie == null) return;

    var last = items
      .Where(r => !string.Equals(r.Visitor, "Tie Break", StringComparison.OrdinalIgnoreCase))
      .OrderByDescending(r => r.GameNumber)
      .FirstOrDefault();

    if (last?.VisitorScore.HasValue == true && last.HomeScore.HasValue == true)
    {
      tie.VisitorScore = last.VisitorScore;
      tie.HomeScore = last.HomeScore;

      // NEW: copy date/time so the tie row appears under the same date group
      if (!string.IsNullOrWhiteSpace(last.Date)) tie.Date = last.Date;
      if (!string.IsNullOrWhiteSpace(last.Time)) tie.Time = last.Time;
    }
    else
    {
      items.Remove(tie);
    }
  }
}

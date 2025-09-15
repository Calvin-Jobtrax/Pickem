using System.Collections.ObjectModel;
using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

public partial class StatusPage : ContentPage
{
  private readonly ApiService _api;
  private int _year = AppConfig.SeasonYear;
  private int _week;
  private int _playerId;
  private int _maxWeek = 18;

  private readonly ObservableCollection<PlayerCardRow> _rows = new();

  public StatusPage() : this(ServiceHelper.GetService<ApiService>()) { }

  public StatusPage(ApiService api, int playerId = 1)
  {
    InitializeComponent();
    _api = api;
    _playerId = playerId;
    _week = WeekHelper.GetCurrentWeek();
    RowsListCtl.ItemsSource = _rows; // bind once
  }

  // Safe element accessors
  private Label WeekLabelCtl => this.FindByName<Label>("WeekLabel")!;
  private Label PlayerLabelCtl => this.FindByName<Label>("PlayerLabel")!;
  private Label HeaderLabelCtl => this.FindByName<Label>("HeaderLabel")!;
  private CollectionView RowsListCtl => this.FindByName<CollectionView>("RowsList")!;
  private ActivityIndicator BusyCtl => this.FindByName<ActivityIndicator>("Busy")!;

  protected override async void OnAppearing()
  {
    base.OnAppearing();

    // Pull MaxWeek from API (matches other pages)
    try
    {
      BusyCtl.IsVisible = BusyCtl.IsRunning = true;
      _maxWeek = await _api.GetMaxWeekAsync(_year);
      if (_maxWeek < 1) _maxWeek = 18;
    }
    catch
    {
      _maxWeek = 18;
    }
    finally
    {
      BusyCtl.IsVisible = BusyCtl.IsRunning = false;
    }

    ClampAndRenderHeaderValues();
    await LoadAsync();
  }

  private void ClampAndRenderHeaderValues()
  {
    _week = Math.Clamp(_week, 1, _maxWeek);
    if (_playerId < 1) _playerId = 1;
    WeekLabelCtl.Text = _week.ToString();
    PlayerLabelCtl.Text = _playerId.ToString();
  }

  private async Task LoadAsync()
  {
    try
    {
      BusyCtl.IsVisible = BusyCtl.IsRunning = true;

      var dto = await _api.GetPlayerCardAsync(_year, _week, _playerId);

      HeaderLabelCtl.Text = dto?.Header ?? $"Player {_playerId} — Week {_week}";

      _rows.Clear();
      if (dto?.Rows != null)
      {
        foreach (var r in dto.Rows) _rows.Add(r);

        // Total points = sum of wagers for rows you WON (exclude the tie/total row)
        var total = _rows
          .Where(r => !r.IsTotal && r.Won == true)
          .Sum(r => r.Value ?? 0);

        // Put that total into the "total" row's Value
        var totalRow = _rows.FirstOrDefault(r => r.IsTotal);
        if (totalRow != null)
        {
          totalRow.Value = total;
          totalRow.Won = null; // avoid red/green background on total
        }
      }
    }
    catch (Exception ex)
    {
      await DisplayAlert("Error", ex.Message, "OK");
    }
    finally
    {
      BusyCtl.IsVisible = BusyCtl.IsRunning = false;
    }
  }

  // --- Button handlers (Week) ---
  private async void OnPrevWeek(object? sender, EventArgs e)
  {
    if (_week > 1)
    {
      _week--;
      WeekLabelCtl.Text = _week.ToString();
      await LoadAsync();
    }
  }

  private async void OnNextWeek(object? sender, EventArgs e)
  {
    if (_week < _maxWeek)
    {
      _week++;
      WeekLabelCtl.Text = _week.ToString();
      await LoadAsync();
    }
  }

  // --- Button handlers (Player) ---
  private async void OnPrevPlayer(object? sender, EventArgs e)
  {
    if (_playerId > 1)
    {
      _playerId--;
      PlayerLabelCtl.Text = _playerId.ToString();
      await LoadAsync();
    }
  }

  private async void OnNextPlayer(object? sender, EventArgs e)
  {
    _playerId++;
    PlayerLabelCtl.Text = _playerId.ToString();
    await LoadAsync();
  }

  private async void OnReload(object? sender, EventArgs e) => await LoadAsync();
}

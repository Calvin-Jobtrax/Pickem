using System.Collections.ObjectModel;
using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

[QueryProperty(nameof(Year), "year")]
[QueryProperty(nameof(Week), "week")]
[QueryProperty(nameof(PlayerId), "playerId")]
public partial class ResultsPage : ContentPage
{
  private readonly ApiService _api;

  public int Year { get; set; } = 2024; // DateTime.Now.Year;
  public int Week { get; set; } = 1;
  public int PlayerId { get; set; } = 1;

  private int _maxWeek = 18;

  private readonly ObservableCollection<PlayerCardRow> _rows = new();

  public ResultsPage() : this(ServiceHelper.GetService<ApiService>()) { }

  public ResultsPage(ApiService api)
  {
    InitializeComponent();
    _api = api;
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
      _maxWeek = await _api.GetMaxWeekAsync(Year);
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
    Week = Math.Clamp(Week, 1, _maxWeek);
    if (PlayerId < 1) PlayerId = 1;
    WeekLabelCtl.Text = Week.ToString();
    PlayerLabelCtl.Text = PlayerId.ToString();
  }

  private async Task LoadAsync()
  {
    try
    {
      BusyCtl.IsVisible = BusyCtl.IsRunning = true;

      var dto = await _api.GetPlayerCardAsync(Year, Week, PlayerId);

      HeaderLabelCtl.Text = dto?.Header ?? $"Player {PlayerId} — Week {Week}";

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
    if (Week > 1)
    {
      Week--;
      WeekLabelCtl.Text = Week.ToString();
      await LoadAsync();
    }
  }

  private async void OnNextWeek(object? sender, EventArgs e)
  {
    if (Week < _maxWeek)
    {
      Week++;
      WeekLabelCtl.Text = Week.ToString();
      await LoadAsync();
    }
  }

  // --- Button handlers (Player) ---
  private async void OnPrevPlayer(object? sender, EventArgs e)
  {
    if (PlayerId > 1)
    {
      PlayerId--;
      PlayerLabelCtl.Text = PlayerId.ToString();
      await LoadAsync();
    }
  }

  private async void OnNextPlayer(object? sender, EventArgs e)
  {
    PlayerId++;
    PlayerLabelCtl.Text = PlayerId.ToString();
    await LoadAsync();
  }

  private async void OnReload(object? sender, EventArgs e) => await LoadAsync();
}

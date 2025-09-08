using System.Collections.ObjectModel;
using System.Diagnostics;
using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

public partial class WagerPage : ContentPage
{
  private readonly ApiService _api;
  private readonly int _playerId; // TODO: set from login/session
  private int _year = AppConfig.SeasonYear; // DateTime.Now.Year;
  private int _week = 1;
  private int _maxWeek = 18;

  private readonly ObservableCollection<WagerItem> _items = new();
  private int _maxGamesThisWeek = 0; // excludes Tie Break

  public WagerPage() : this(ServiceHelper.GetService<ApiService>(), 1) { }
  public WagerPage(ApiService api, int playerId = 1)
  {
    InitializeComponent();
    _api = api;
    _playerId = playerId;
    WagerList.ItemsSource = _items;
  }

  protected override async void OnAppearing()
  {
    base.OnAppearing();
    try
    {
      Busy.IsVisible = Busy.IsRunning = true;

      // Get and apply MaxWeek (drop-in: same behavior as PoolPage)
      _maxWeek = await _api.GetMaxWeekAsync(_year);
      if (_maxWeek < 1) _maxWeek = 18;

      // Clamp week within range
      if (_week < 1) _week = 1;
      if (_week > _maxWeek) _week = _maxWeek;

      WeekLabel.Text = _week.ToString();

      await LoadAsync();
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

  private async Task LoadAsync()
  {
    try
    {
      Busy.IsVisible = Busy.IsRunning = true;

      var rows = await _api.GetWagersAsync(_year, _week, _playerId) ?? new List<WagerRow>();

      // Detach old handlers
      foreach (var old in _items)
        old.PropertyChanged -= Row_PropertyChanged;

      _items.Clear();

      var i = 0;
      foreach (var r in rows)
      {
        var item = new WagerItem
        {
          RowIndex = i++,
          Record = r.Record,
          Year = r.Year,
          Week = r.Week,
          Game = r.Game,
          Wager = r.Wager,
          Visitor = r.Visitor,
          Home = r.Home,
          VisitorWin = r.VisitorWin,
          HomeWin = r.HomeWin
        };

        item.MarkClean();
        item.PropertyChanged += Row_PropertyChanged;
        _items.Add(item);
      }

      ValidateAndShowBanner();
      UpdateButtonsEnabled();
    }
    finally
    {
      Busy.IsVisible = Busy.IsRunning = false;
    }
  }

  private void UpdateButtonsEnabled()
  {
    var anyDirty = _items.Any(x => x.IsDirty);
    SaveAllBtn.IsEnabled = anyDirty;
    CancelBtn.IsEnabled = anyDirty;
  }

  private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (e.PropertyName is nameof(WagerItem.IsDirty) ||
        e.PropertyName is nameof(WagerItem.Wager) ||
        e.PropertyName is nameof(WagerItem.VisitorWin) ||
        e.PropertyName is nameof(WagerItem.HomeWin))
    {
      UpdateButtonsEnabled();
    }
  }

  private void OnWagerEdited(object? sender, TextChangedEventArgs e) => ValidateAndShowBanner();

  private void ValidateAndShowBanner()
  {
    // (unchanged) ... keep your validation logic here
    // NOTE: no changes required; it still uses _items and sets WarnLabel text/visibility
    // --- BEGIN existing block (unchanged) ---
    foreach (var it in _items)
    {
      it.IsDuplicate = false;
      it.IsOutOfRange = false;
      it.IsNoPick = false;
      it.OnPropertyChanged(nameof(WagerItem.IsDuplicate));
      it.OnPropertyChanged(nameof(WagerItem.IsOutOfRange));
      it.OnPropertyChanged(nameof(WagerItem.IsNoPick));
    }

    var realGames = _items.Where(it => !it.IsTieBreak).ToList();
    var tbRow = _items.FirstOrDefault(it => it.IsTieBreak);

    _maxGamesThisWeek = realGames.Count;

    var used = new Dictionary<int, List<WagerItem>>();
    foreach (var it in realGames)
    {
      int w = it.Wager;
      if (w < 1 || w > _maxGamesThisWeek)
      {
        it.IsOutOfRange = true;
        it.OnPropertyChanged(nameof(WagerItem.IsOutOfRange));
        continue;
      }
      if (!used.TryGetValue(w, out var list))
      {
        list = new List<WagerItem>();
        used[w] = list;
      }
      list.Add(it);
    }

    foreach (var kv in used)
      if (kv.Value.Count > 1)
        foreach (var it in kv.Value)
        {
          it.IsDuplicate = true;
          it.OnPropertyChanged(nameof(WagerItem.IsDuplicate));
        }

    var missing = Enumerable
      .Range(1, Math.Max(_maxGamesThisWeek, 0))
      .Where(n => !used.ContainsKey(n))
      .ToList();

    bool hasNoPick = false;
    foreach (var it in realGames)
    {
      if (it.VisitorWin == true || it.HomeWin == true) continue;
      it.IsNoPick = true;
      it.OnPropertyChanged(nameof(WagerItem.IsNoPick));
      hasNoPick = true;
    }

    bool tieBad = false;
    if (tbRow is not null)
    {
      int tw = tbRow.Wager;
      if (tw <= 2)
      {
        tieBad = true;
        tbRow.IsOutOfRange = true;
        tbRow.OnPropertyChanged(nameof(WagerItem.IsOutOfRange));
      }
    }

    var parts = new List<string>();
    if (realGames.Any(it => it.IsOutOfRange))
      parts.Add($"• Wagers must be between 1 and {_maxGamesThisWeek}.");
    if (realGames.Any(it => it.IsDuplicate))
      parts.Add("• Duplicate wager numbers found.");
    if (missing.Count > 0)
      parts.Add($"• Missing numbers: {string.Join(", ", missing)}.");
    if (hasNoPick)
      parts.Add("• Each game must have a pick (Visitor or Home).");
    if (tieBad)
      parts.Add("• Tie Break must be greater than 2.");

    var msg = string.Join("\n", parts);
    WarnLabel.IsVisible = parts.Count > 0;
    WarnLabel.Text = WarnLabel.IsVisible
      ? "⚠️ Please review:\n" + msg
      : $"Assign each number 1..{_maxGamesThisWeek} exactly once. Tie Break can be any number greater than 2.";
    WarnBorder.IsVisible = WarnLabel.IsVisible;
    // --- END existing block ---
  }

  private async void OnCancelAll(object sender, EventArgs e)
  {
    foreach (var it in _items)
      it.RevertToClean();

    ValidateAndShowBanner();
    UpdateButtonsEnabled();
    await DisplayAlert("Canceled", "Changes have been reverted.", "OK");
  }

    private async void OnSaveAll(object sender, EventArgs e)
    {
        ValidateAndShowBanner();

        Busy.IsVisible = Busy.IsRunning = true;

        try
        {
            foreach (var r in _items)
            {
                try
                {
                    // fire update and ignore errors
                    await _api.UpdateGameAsync(r.Record, r.Wager, r.VisitorWin, r.HomeWin);
                }
                catch
                {
                    // swallow any exception, keep going
                }

                r.MarkClean();
            }

            UpdateButtonsEnabled();

            ValidateAndShowBanner();

            // optional: force collection view/grid refresh
            WagerList.ItemsSource = null;
            WagerList.ItemsSource = _items;

            if (WarnLabel.IsVisible)
                await DisplayAlert("Saved (with warnings)", "Saved, but review the notes above.", "OK");
            else
                await DisplayAlert("Saved", "All wagers saved.", "OK");


        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
        }
    }


    // NEW: Button handlers (mirroring PoolPage)
    private async void OnPrevWeek(object sender, EventArgs e)
  {
    if (_week > 1)
    {
      _week--;
      WeekLabel.Text = _week.ToString();
      await LoadAsync();
    }
  }

  private async void OnNextWeek(object sender, EventArgs e)
  {
    if (_week < _maxWeek)
    {
      _week++;
      WeekLabel.Text = _week.ToString();
      await LoadAsync();
    }
  }

  // Checkbox mutual exclusivity (unchanged)
  private void OnVisitorCheckedChanged(object sender, CheckedChangedEventArgs e)
  {
    if (sender is not CheckBox cb) return;
    if (cb.BindingContext is not WagerItem item) return;
    if (item.IsTieBreak) return;

    if (e.Value) // Visitor turned ON -> turn Home OFF
    {
      if (item.HomeWin == true) item.HomeWin = false;
    }
  }

  private void OnHomeCheckedChanged(object sender, CheckedChangedEventArgs e)
  {
    if (sender is not CheckBox cb) return;
    if (cb.BindingContext is not WagerItem item) return;
    if (item.IsTieBreak) return;

    if (e.Value) // Home turned ON -> turn Visitor OFF
    {
      if (item.VisitorWin == true) item.VisitorWin = false;
    }
  }
}

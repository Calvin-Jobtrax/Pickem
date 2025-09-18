using System.Collections.ObjectModel;
using Pickem.Models;
using Pickem.Services;

namespace Pickem.Pages;

public partial class WagersPage : ContentPage
{
  private readonly ApiService _api;
  private readonly int _playerId; // TODO: set from login/session
  private int _year = AppConfig.SeasonYear; // DateTime.Now.Year;
  private int _week = 1;
  private int _maxWeek = 18;

  private readonly ObservableCollection<WagerItem> _items = new();
  private int _maxGamesThisWeek = 0; // excludes Tie Break

  public WagersPage() : this(ServiceHelper.GetService<ApiService>(), 1) { }
  public WagersPage(ApiService api, int playerId = 1)
  {
    InitializeComponent();
    _api = api;
    _playerId = playerId;
    WagerList.ItemsSource = _items;
    _week = WeekHelper.GetCurrentWeek();
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

            // 1) Pull wagers (user picks)
            var wagerRows = await _api.GetWagersAsync(_year, _week, _playerId) ?? new List<WagerRow>();

            // 2) Pull pool (has real scores/results)
            var poolRows = await _api.GetPoolAsync(_year, _week) ?? new List<PoolRow>();

            // (Optional but harmless) ensure TB row in pool has scores — mirrors your SchedulePage
            PrepareTieBreak(poolRows);

            // 3) Build score index from pool (by GameNumber, Record, and team names)
            var (scoreByGame, scoreByRecord, scoreByTeams) = BuildScoreIndex(poolRows);

            // Detach old handlers
            foreach (var old in _items)
                old.PropertyChanged -= Row_PropertyChanged;

            _items.Clear();

            // 4) Map wagers -> WagerItem and copy actual scores so IsFinal/UserPickCorrect work
            var i = 0;
            foreach (var r in wagerRows)
            {
                // Try matches: by Game, then by Record, then by team names
                (int? v, int? h) scores = (null, null);
                if (r.Game > 0 && scoreByGame.TryGetValue(r.Game, out var gScores))
                    scores = gScores;
                else if (r.Record > 0 && scoreByRecord.TryGetValue(r.Record, out var recScores))
                    scores = recScores;
                else if (!string.IsNullOrWhiteSpace(r.Visitor) && !string.IsNullOrWhiteSpace(r.Home))
                {
                    var key = (r.Visitor.Trim(), r.Home.Trim());
                    if (scoreByTeams.TryGetValue(key, out var teamScores))
                        scores = teamScores;
                }

                var item = new WagerItem
                {
                    RowIndex = i++,

                    // base WagerRow fields (you already have these)
                    Record = r.Record,
                    Year = r.Year,
                    Week = r.Week,
                    Game = r.Game,
                    Wager = r.Wager,
                    Visitor = r.Visitor,
                    Home = r.Home,
                    VisitorWin = r.VisitorWin,
                    HomeWin = r.HomeWin,

                    // IMPORTANT: copy real scores
                    ActualVisitorScore = scores.v,
                    ActualHomeScore = scores.h
                };

                item.MarkClean();
                item.PropertyChanged += Row_PropertyChanged;
                _items.Add(item);
            }

            ValidateAndShowBanner();
            UpdateButtonsEnabled();

            // (Optional) rebind to force re-eval of RowBackground converter if your UI is stubborn
            // WagerList.ItemsSource = null; WagerList.ItemsSource = _items;
        }
        finally
        {
            Busy.IsVisible = Busy.IsRunning = false;
        }
    }


    private void UpdateButtonsEnabled()
  {
    var pastCutoff = WeekHelper.IsCurrentWeekPastCutoff(_week);

    // Save is only enabled if NOT past cutoff and there are dirty rows
    var anyDirty = _items.Any(i => i.IsDirty);

    SaveAllBtn.IsEnabled = !pastCutoff && anyDirty;
    SaveAllBtn.Text = pastCutoff ? "Locked" : "Save";

    // Cancel remains useful for local changes even if locked
    CancelBtn.IsEnabled = anyDirty;

    // Optional: show a banner explaining the lock
    if (pastCutoff)
    {
      WarnLabel.Text = $"Week {_week} is locked after Thu 4:00 PM Central.";
      WarnLabel.IsVisible = true;
      WarnBorder.IsVisible = true;
    }
    else
    {
      // keep any existing validation text if you have it,
      // or clear the lock message
      if (WarnLabel.Text?.StartsWith("Week") == true && WarnLabel.Text.Contains("locked"))
      {
        WarnLabel.IsVisible = false;
        WarnBorder.IsVisible = false;
      }
    }
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
    // HARD STOP if locked
    if (WeekHelper.IsCurrentWeekPastCutoff(_week))
    {
      await DisplayAlert("Locked", $"Week {_week} is locked after Thu 4:00 PM Central. Saves are disabled.", "OK");
      UpdateButtonsEnabled();
      return;
    }

    ValidateAndShowBanner();
    Busy.IsVisible = Busy.IsRunning = true;

    try
    {
      foreach (var r in _items)
      {
        try
        {
          await _api.UpdateGameAsync(r.Record, r.Wager, r.VisitorWin, r.HomeWin);
        }
        catch { /* ignore per your original */ }

        r.MarkClean();
      }

      UpdateButtonsEnabled();     // <- refresh buttons post-save
      ValidateAndShowBanner();

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
    UpdateButtonsEnabled();
  }

  private async void OnNextWeek(object sender, EventArgs e)
  {
    if (_week < _maxWeek)
    {
      _week++;
      WeekLabel.Text = _week.ToString();
      await LoadAsync();
    }
    UpdateButtonsEnabled();
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

    private static (Dictionary<int, (int? v, int? h)> byGame,
                   Dictionary<int, (int? v, int? h)> byRecord,
                   Dictionary<(string v, string h), (int? v, int? h)> byTeams)
      BuildScoreIndex(IEnumerable<PoolRow> poolRows)
    {
        var byGame = new Dictionary<int, (int? v, int? h)>();
        var byRecord = new Dictionary<int, (int? v, int? h)>();
        var byTeams = new Dictionary<(string v, string h), (int? v, int? h)>();

        foreach (var pr in poolRows)
        {
            // ignore rows without teams
            var vName = pr.Visitor?.Trim();
            var hName = pr.Home?.Trim();
            var tup = (pr.VisitorScore, pr.HomeScore);

            if (pr.GameNumber > 0)
                byGame[pr.GameNumber] = tup;

            if (pr.Record > 0)
                byRecord[pr.Record] = tup;

            if (!string.IsNullOrEmpty(vName) && !string.IsNullOrEmpty(hName))
                byTeams[(vName, hName)] = tup;
        }

        return (byGame, byRecord, byTeams);
    }

    // Add this to WagersPage.cs
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

            // copy date/time so TB groups/aligns with the last game
            if (!string.IsNullOrWhiteSpace(last.Date)) tie.Date = last.Date;
            if (!string.IsNullOrWhiteSpace(last.Time)) tie.Time = last.Time;
        }
        else
        {
            // if we don't have scores yet, drop the TB row so it doesn't interfere
            items.Remove(tie);
        }
    }

}

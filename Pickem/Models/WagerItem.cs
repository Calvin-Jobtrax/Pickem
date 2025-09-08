// Models/WagerItem.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pickem.Models;

public sealed class WagerItem : WagerRow, INotifyPropertyChanged
{
  public int RowIndex { get; set; }
  //public bool IsTieBreakRow => string.Equals(Visitor, "Tie Break", StringComparison.OrdinalIgnoreCase);

  // validation flags
  public bool IsDuplicate { get; set; }
  public bool IsOutOfRange { get; set; }
  public bool IsNoPick { get; set; }

  // ---- DIRTY TRACKING ----
  private int _origWager;
  private bool? _origVisitorWin;
  private bool? _origHomeWin;

  private bool _isDirty;
  public bool IsDirty
  {
    get => _isDirty;
    private set { if (_isDirty == value) return; _isDirty = value; OnPropertyChanged(); }
  }

  public void MarkClean()
  {
    _origWager = base.Wager;
    _origVisitorWin = base.VisitorWin;
    _origHomeWin = base.HomeWin;
    IsDirty = false;
  }

  public void RevertToClean()
  {
    // set backing fields directly to avoid triggering exclusivity logic
    base.Wager = _origWager;
    base.VisitorWin = _origVisitorWin;
    base.HomeWin = _origHomeWin;

    // refresh UI
    OnPropertyChanged(nameof(Wager));
    OnPropertyChanged(nameof(VisitorWin));
    OnPropertyChanged(nameof(HomeWin));

    // if original had no pick on a real game, keep it as-is (you can auto-default if you prefer)
    UpdateDirty();
  }

  private void UpdateDirty()
      => IsDirty = base.Wager != _origWager || base.VisitorWin != _origVisitorWin || base.HomeWin != _origHomeWin;

  // Intercept writes so we can flag IsDirty and keep exclusivity (non–TB)
  public new int Wager
  {
    get => base.Wager;
    set { if (base.Wager == value) return; base.Wager = value; OnPropertyChanged(); UpdateDirty(); }
  }

  public new bool? VisitorWin
  {
    get => base.VisitorWin;
    set
    {
      if (base.VisitorWin == value) return;
      base.VisitorWin = value; OnPropertyChanged(); UpdateDirty();

      if (value == true) { base.HomeWin = false; OnPropertyChanged(nameof(HomeWin)); UpdateDirty(); }
      if (!IsTieBreak && base.VisitorWin != true && base.HomeWin != true) { base.VisitorWin = true; OnPropertyChanged(nameof(VisitorWin)); UpdateDirty(); }
    }
  }

  public new bool? HomeWin
  {
    get => base.HomeWin;
    set
    {
      if (base.HomeWin == value) return;
      base.HomeWin = value; OnPropertyChanged(); UpdateDirty();

      if (value == true) { base.VisitorWin = false; OnPropertyChanged(nameof(VisitorWin)); UpdateDirty(); }
      if (!IsTieBreak && base.VisitorWin != true && base.HomeWin != true) { base.HomeWin = true; OnPropertyChanged(nameof(HomeWin)); UpdateDirty(); }
    }
  }

  public event PropertyChangedEventHandler? PropertyChanged;
  public void OnPropertyChanged([CallerMemberName] string? name = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

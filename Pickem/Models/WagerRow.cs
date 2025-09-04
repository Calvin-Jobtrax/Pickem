namespace Pickem.Models;

public class WagerRow
{
  public int Record { get; set; }     // PickemGames.Record (needed for PUT)
  public int Year { get; set; }
  public int Week { get; set; }
  public int Game { get; set; }
  public string Visitor { get; set; } = "";
  public string Home { get; set; } = "";
  public bool? VisitorWin { get; set; }
  public bool? HomeWin { get; set; }
  public int Wager { get; set; }

  public bool IsTieBreak =>
    string.Equals(Visitor, "Tie Break", StringComparison.OrdinalIgnoreCase);
}

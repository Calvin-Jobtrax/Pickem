namespace Pickem.Models
{
  public class StandingRow
  {
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty; // blanks excluded in SP
    public int TotalPoints { get; set; }
    public int CorrectCount { get; set; }
    public int? TieBreakGuess { get; set; }
    public int? TieBreakActual { get; set; }

    // computed in API or client
    public int? TieBreakDiff { get; set; }

    // UI helpers
    public bool IsLeader { get; set; }
    public bool IsAlternate { get; set; }
  }
}

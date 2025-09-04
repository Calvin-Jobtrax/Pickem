namespace Pickem.Models
{
  public class PoolRow
  {
    public int Record { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }
    public int GameNumber { get; set; }
    public string Visitor { get; set; } = "";
    public string Home { get; set; } = "";
    public int? VisitorScore { get; set; }
    public int? HomeScore { get; set; }
    public bool? VisitorWin { get; set; }
    public bool? HomeWin { get; set; }
    public string? Result { get; set; }

    // Wire-friendly fields (strings). Parse client-side if needed.
    public string? Date { get; set; }   // "2025-09-07"
    public string? Time { get; set; }   // "19:20:00" or "19:20"

    // --- Helpers ---

    // Date-only for grouping (returns MinValue if missing/bad)
    public DateTime GameDate
      => TryParseDate(Date, out var d) ? d.Date : DateTime.MinValue;

    // Optional: full DateTime if you want to combine Date + Time
    public DateTime? GameDateTime
      => (TryParseDate(Date, out var d) && TryParseTime(Time, out var t))
         ? d.Date.Add(t) : null;

    // Robust parsers (invariant)
    private static bool TryParseDate(string? s, out DateTime d)
      => DateTime.TryParseExact(s ?? "", "yyyy-MM-dd",
           System.Globalization.CultureInfo.InvariantCulture,
           System.Globalization.DateTimeStyles.None, out d);

    private static bool TryParseTime(string? s, out TimeSpan t)
      => TimeSpan.TryParseExact(s ?? "",
           new[] { @"hh\:mm\:ss", @"hh\:mm" },  // accepts "19:20:00" or "19:20"
           System.Globalization.CultureInfo.InvariantCulture, out t);
  }
}

// Pickem/Models/PlayerCard.cs
namespace Pickem.Models
{
  public sealed class PlayerCardRow
  {
    public int GameNumber { get; set; }
    public string Matchup { get; set; } = "";  // NEW
    public string Label { get; set; } = "";
    public int? Value { get; set; }
    public bool? Won { get; set; }
    public bool IsTotal { get; set; }
  }

  public sealed class PlayerCardDto
  {
    public string Header { get; set; } = "";
    public List<PlayerCardRow> Rows { get; set; } = new();
  }
}

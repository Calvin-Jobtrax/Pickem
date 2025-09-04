namespace Pickem.Models
{
  public class PodiumRow
  {
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public int Firsts { get; set; }
    public int Seconds { get; set; }
    public int Thirds { get; set; }
    public int WeeksPlayed { get; set; }
    public int SeasonPoints { get; set; }
    public int SeasonCorrect { get; set; }
  }
}

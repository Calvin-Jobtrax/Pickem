namespace Pickem.Services;

public sealed class SessionState
{
  // set these during login; defaults for now
  public int Year { get; set; } = DateTime.Now.Year;
  public int PlayerId { get; set; } = 1;
  public string PlayerName { get; set; } = "Calvin";
}

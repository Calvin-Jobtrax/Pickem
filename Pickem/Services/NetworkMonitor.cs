namespace Pickem.Services;

public sealed class NetworkMonitor
{
  public static NetworkMonitor Shared { get; } = new();

  public bool IsOnWiFi
  {
    get
    {
      var profiles = Connectivity.Current.ConnectionProfiles;
      return profiles.Contains(ConnectionProfile.WiFi);
    }
  }

  public bool HasInternet =>
      Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}

namespace Pickem

{
  public static class ServiceHelper
  {
    public static T GetService<T>() where T : notnull =>
        Current.GetRequiredService<T>();

    public static IServiceProvider Current =>
        Application.Current?.Handler?.MauiContext?.Services
        ?? throw new InvalidOperationException("Service provider not available yet.");
  }
}

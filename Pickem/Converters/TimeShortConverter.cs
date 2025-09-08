using System.Globalization;

namespace Pickem.Converters
{
  public sealed class TimeShortConverter : IValueConverter
  {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value is string s && DateTime.TryParse(s, out var dt))
      {
        // Example: 4:30 pm
        return dt.ToString("h:mm tt", CultureInfo.InvariantCulture).ToLower();
      }
      return value ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
  }
}

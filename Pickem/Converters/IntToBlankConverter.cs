using System.Globalization;

namespace Pickem.Converters;

public sealed class IntToBlankConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is int i) return i == 0 ? string.Empty : i.ToString(culture);
    return string.Empty;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var s = (value as string)?.Trim();
    return int.TryParse(s, NumberStyles.Integer, culture, out var i) ? i : 0;
  }
}

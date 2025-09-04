using System.Globalization;

namespace Pickem.Converters;

public sealed class NameScoreConverter : IMultiValueConverter
{
  // Input: [0]=team name (string), [1]=score (int? or null)
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    var name = values.Length > 0 ? values[0]?.ToString() : "";
    if (string.IsNullOrWhiteSpace(name)) return string.Empty;

    if (values.Length > 1 && values[1] != null && int.TryParse(values[1].ToString(), out var s))
      return $"{name} ({s})";

    return name!;
  }

  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
      => throw new NotImplementedException();
}

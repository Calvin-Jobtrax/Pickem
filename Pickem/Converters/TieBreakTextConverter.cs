// Converters/TieBreakTextConverter.cs
using System.Globalization;

namespace Pickem.Converters;

public sealed class TieBreakTextConverter : IMultiValueConverter
{
  // Input: [0]=visitorScore, [1]=homeScore (from the *last* game)
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    int v, h;
    if (values.Length > 1 &&
        values[0] != null && int.TryParse(values[0].ToString(), out v) &&
        values[1] != null && int.TryParse(values[1].ToString(), out h))
    {
      return $"Tie Break ({v + h})";
    }
    return "Tie Break";
  }

  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
      => throw new NotImplementedException();
}

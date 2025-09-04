using Microsoft.Maui.Controls;
using System.Globalization;

namespace Pickem.Converters;
public sealed class BoolToFontAttr : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    => (value is bool b && b) ? FontAttributes.Bold : FontAttributes.None;

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    => throw new NotImplementedException();
}

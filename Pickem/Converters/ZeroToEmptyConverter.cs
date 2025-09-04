using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Pickem.Converters
{
  public sealed class ZeroToEmptyConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null) return string.Empty;

      // Handle int
      if (value.GetType() == typeof(int))
      {
        var i = (int)value;
        return i == 0 ? string.Empty : i.ToString(culture);
      }

      // Handle int? (Nullable<int>)
      if (value.GetType() == typeof(int?))
      {
        var ni = (int?)value;
        if (!ni.HasValue || ni.Value == 0) return string.Empty;
        return ni.Value.ToString(culture);
      }

      // Pass strings through
      if (value is string s) return s;

      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var s = (value as string)?.Trim();
      if (string.IsNullOrEmpty(s)) return 0; // return null instead if your target is int?
      int n;
      return int.TryParse(s, NumberStyles.Integer, culture, out n) ? n : 0;
    }
  }
}

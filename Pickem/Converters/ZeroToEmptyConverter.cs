using System.Globalization;

namespace Pickem.Converters
{
    public sealed class ZeroToEmptyConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i) return i == 0 ? string.Empty : i.ToString(culture);
            return value?.ToString() ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim();
            if (string.IsNullOrEmpty(s)) return 0;

            return int.TryParse(s, NumberStyles.Integer, culture, out var n) ? n : 0;
        }
    }
}

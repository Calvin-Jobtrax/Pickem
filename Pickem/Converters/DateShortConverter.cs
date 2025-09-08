using System.Globalization;

namespace Pickem.Converters
{
    public sealed class DateShortConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s && DateTime.TryParse(s, out var dt))
            {
                // Example: Wed. 10
                return dt.ToString("ddd. MMM d", CultureInfo.InvariantCulture);
            }
            return value ?? string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
          => throw new NotImplementedException();
    }
}

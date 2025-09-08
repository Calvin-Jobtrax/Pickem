using System.Globalization;
using Microsoft.Maui.Controls;
using Pickem.Models;

namespace Pickem.Converters
{
  public sealed class RowBackgroundConverter : IValueConverter
  {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      // PoolPage rows
      if (value is PoolItem p)
        return (p.RowIndex % 2 == 0) ? Color.FromArgb("#FFF9C4") : Color.FromArgb("#E0F2F1");

      // WagerPage rows (with validations)
      if (value is WagerItem w)
      {
        if (w.IsOutOfRange || w.IsNoPick) return Color.FromArgb("#FFE4E1"); // error
        if (w.IsDuplicate) return Color.FromArgb("#FFF4CC");  // warning
        return (w.RowIndex % 2 == 0) ? Color.FromArgb("#FFF9C4") : Color.FromArgb("#E0F2F1");
      }

      // (Optional) if you ever bind RowIndex directly
      if (value is int i)
        return (i % 2 == 0) ? Color.FromArgb("#FFF9C4") : Color.FromArgb("#E0F2F1");

      return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
      => throw new NotImplementedException();
  }
}

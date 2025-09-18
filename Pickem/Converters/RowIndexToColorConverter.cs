// Converters/RowBackgroundConverter.cs
using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Pickem.Models;

namespace Pickem.Converters
{
    public sealed class RowBackgroundConverter : IValueConverter
    {
        // palette
        private static readonly Color LightOrange = Color.FromArgb("#FFE8CC"); // error
        private static readonly Color LightBlue = Color.FromArgb("#EAF2FF"); // pre-final alt
        private static readonly Color LightYellow = Color.FromArgb("#FFF9D6"); // pre-final alt
        private static readonly Color LightGreen = Color.FromArgb("#E6F6E6"); // correct pick
        private static readonly Color LightRed = Color.FromArgb("#FDE8E8"); // wrong pick
        private static readonly Color LightPurple = Color.FromArgb("#EAD9F5"); // tie-break
        private static readonly Color WarnYellow = Color.FromArgb("#FFF4CC"); // duplicate

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Pool page: simple alternating (kept for compatibility)
            if (value is PoolItem p)
                return (p.RowIndex % 2 == 0) ? LightYellow : LightBlue;

            if (value is WagerItem w)
            {
                // tie-break always purple
                if (w.IsTieBreak)
                    return LightPurple;

                // validation
                if (w.IsOutOfRange || w.IsNoPick)
                    return LightOrange;
                if (w.IsDuplicate)
                    return WarnYellow;

                // final -> color by user correctness
                if (w.IsFinal && w.UserPickCorrect.HasValue)
                    return w.UserPickCorrect.Value ? LightGreen : LightRed;

                // not final -> alternate
                return (w.RowIndex % 2 == 0) ? LightYellow : LightBlue;
            }

            if (value is int i)
                return (i % 2 == 0) ? LightYellow : LightBlue;

            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
          => throw new NotImplementedException();
    }
}

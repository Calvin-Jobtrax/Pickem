using System.Runtime.InteropServices;

namespace Pickem
{
    public static class WeekHelper
    {
        private static readonly DateTime _baseDate = new(2025, 9, 3);

        private static TimeZoneInfo GetCentralTz()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }

        public static int GetCurrentWeek()
        {
            var today = DateTime.Today;
            if (today < _baseDate) return 1;
            var diff = today - _baseDate.Date;
            return (int)(diff.TotalDays / 7) + 1;
        }

        public static bool IsCurrentWeekPastCutoff(int week)
        {
            if (week < 1) week = 1;

            var weekStart = _baseDate.Date.AddDays(7 * (week - 1));
            int daysToThu = ((int)DayOfWeek.Thursday - (int)weekStart.DayOfWeek + 7) % 7;
            var thu = weekStart.AddDays(daysToThu);

            var centralTz = GetCentralTz();
            var cutoffCentral = new DateTime(thu.Year, thu.Month, thu.Day, 16, 0, 0, DateTimeKind.Unspecified);
            var cutoffUtc = TimeZoneInfo.ConvertTimeToUtc(cutoffCentral, centralTz);

            return DateTime.UtcNow >= cutoffUtc;
        }
    }
}

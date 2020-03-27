using System;
using System.Text.RegularExpressions;

namespace GitLabTimeManager.Helpers
{
    internal static class TimeHelper
    {
        private const string MonthUnit = "mo";
        private const string WeekUnit = "w";
        private const string DayUnit = "d";
        private const string HourUnit = "h";
        private const string MinuteUnit = "m";
        private const string SecondUnit = "s";

        private static string TimeSpentKeyPhrase => "of time spent";
        private static string EstimateKeyPhrase => "changed time estimate to";
        private static string SubtractedKeyPhrase => "subtracted";
        private static string AddedKeyPhrase => "added";

        private const double HoursPerDay = 8;
        private const double DaysPerWeek = 5;
        private const double WeeksPerMonth = 4;

        // Parse spent time in hours
        public static double ParseSpent(this string textDate) => ParseTime(textDate, TimeSpentKeyPhrase);

        // Parse estimate time in hours
        public static double ParseEstimate(this string textDate) => ParseTime(textDate, EstimateKeyPhrase);

        private static double ParseTime(string textDate, string keyPhase)
        {
            var sign = SpendSign(textDate);

            if (!textDate.Contains(keyPhase)) return 0;
            var months = ParseNumberBefore(textDate, MonthUnit);
            var weeks = ParseNumberBefore(textDate, WeekUnit);
            var days = ParseNumberBefore(textDate, DayUnit);
            var hours = ParseNumberBefore(textDate, HourUnit);
            var minutes = ParseNumberBefore(textDate, MinuteUnit);
            var seconds = ParseNumberBefore(textDate, SecondUnit);

            return sign * 
                   (MonthsToHours(months) +
                   WeeksToHours(weeks) +
                   DaysToHours(days) +
                   hours +
                   MinutesToHours(minutes) +
                   SecondsToHours(seconds));
        }

        private static int SpendSign(string textDate)
        {
            var isAdded = textDate.Contains(AddedKeyPhrase);
            var isSubtracted = textDate.Contains(SubtractedKeyPhrase);
            if (isAdded)
                return +1;
            if (isSubtracted)
                return -1;
            return 0;
        }

        private static int ParseNumberBefore(string source, string before)
        {
            var match = new Regex($"[0-9]+{before}").Match(source);
            if (!match.Success) return 0;
            try
            {
                var l = match.Value.Length - before.Length; 
                return Convert.ToInt32(match.Value.Substring(0, l));
            }
            catch
            {
                return 0;
            }
        }

        public static double SecondsToHours(this double s) => TimeSpan.FromSeconds(s).TotalHours;

        private static double MinutesToHours(double m) => TimeSpan.FromMinutes(m).TotalHours;

        private static double DaysToHours(double d) => d * HoursPerDay;
                       
        private static double WeeksToHours(double w) => DaysToHours(w * DaysPerWeek);
                       
        private static double MonthsToHours(double mo) => WeeksToHours(mo * WeeksPerMonth);

        public static double HoursToDays(double day) => day / HoursPerDay;

        public static string ConvertSpent(this TimeSpan ts)
        {
            return $@"/spend {ts.TotalMinutes:####}m";
        }

        public static TimeSpan GetWeekdaysTime(DateTime startDate, DateTime endDate)
        {
            var curDate = startDate;
            var holidays = 0;
            var allDays = (int)(endDate - startDate).TotalDays;
            while (curDate <= endDate)
            {
                if (curDate.DayOfWeek == DayOfWeek.Saturday ||
                    curDate.DayOfWeek == DayOfWeek.Sunday)
                    holidays++;
                curDate = curDate.AddDays(1);
            }

            return TimeSpan.FromHours((allDays - holidays) * HoursPerDay);
        }

        public static bool IsNightBreak
        {
            get
            {
                const int nightHour = 10;
                var nightTime = DateTime.Today.AddHours(nightHour);

                return DateTime.Now > nightTime;
            }
        }
    }
}

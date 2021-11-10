using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitLabTimeManager.Helpers
{
    internal static class TimeHelper
    {
        private const double HoursPerDay = 8;
        private const double DaysPerWeek = 5;
        private const double WeeksPerMonth = 4;

        public static DateTime Today => DateTime.Today;
        public static DateTime StartDate => DateTime.Today.AddDays(-DateTime.Today.Day + 1);
        public static DateTime EndDate => StartDate.AddMonths(1);
        public static DateTime MonthAgo => Today.AddMonths(-1);
        public static DateTime StartPastDate => StartDate.AddMonths(-3);
        
        public static double SecondsToHours(this double s) => TimeSpan.FromSeconds(s).TotalHours;

        public static double MinutesToHours(double m) => TimeSpan.FromMinutes(m).TotalHours;

        public static double DaysToHours(double d) => d * HoursPerDay;

        public static double WeeksToHours(double w) => DaysToHours(w * DaysPerWeek);

        public static double MonthsToHours(double mo) => WeeksToHours(mo * WeeksPerMonth);

        public static double HoursToDays(double hours) => hours / HoursPerDay;

        public static string ConvertSpent(this TimeSpan ts)
        {
            return $@"/spend {ts.TotalMinutes:####}m";
        }

        public static string ConvertEstimate(this TimeSpan ts)
        {
            return $@"/estimate {ts.TotalSeconds:####}s";
        }

        public static TimeSpan GetWeekdaysTime(DateTime startDate, DateTime endDate)
        {
            var curDate = startDate;
            var holidays = 0;
            var allDays = Math.Ceiling((endDate - startDate).TotalDays);
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
                const int nightHour = 22;
                var nightTime = DateTime.Today.AddHours(nightHour);

                return DateTime.Now > nightTime;
            }
        }
    }

    public static class ParseHelper
    {
        private static string TimeSpentKeyPhrase => "of time spent";
        private static string EstimateKeyPhrase => "changed time estimate to";
        private static string SubtractedKeyPhrase => "subtracted";
        private static string AddedKeyPhrase => "added";
        private static string Commit => "mentioned in commit";

        private const string MonthUnit = "mo";
        private const string WeekUnit = "w";
        private const string DayUnit = "d";
        private const string HourUnit = "h";
        private const string MinuteUnit = "m";

        private const string SecondUnit = "s";

        public static bool IsCommit(this string text)
        {
            return text.Contains(Commit);
        }

        // Parse spent time in hours
        public static double ParseSpent(this string textDate)
        {
            var sign = SpendSign(textDate);
            return sign * ParseTimeInternal(textDate, TimeSpentKeyPhrase);
        }

        // Parse estimate time in hours
        public static double ParseEstimate(this string textDate)
        {
            return ParseTimeInternal(textDate, EstimateKeyPhrase);
        }

        private static double ParseTimeInternal(string textDate, string keyPhase)
        {
            if (!textDate.Contains(keyPhase)) return 0;
            var months = textDate.ParseNumberBefore(MonthUnit);
            var weeks = textDate.ParseNumberBefore(WeekUnit);
            var days = textDate.ParseNumberBefore(DayUnit);
            var hours = textDate.ParseNumberBefore(HourUnit);
            var minutes = textDate.ParseNumberBefore(MinuteUnit);
            var seconds = textDate.ParseNumberBefore(SecondUnit);

            return TimeHelper.MonthsToHours(months) +
                   TimeHelper.WeeksToHours(weeks) +
                   TimeHelper.DaysToHours(days) +
                   hours +
                   TimeHelper.MinutesToHours(minutes) +
                   TimeHelper.SecondsToHours(seconds);
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

        private static int ParseNumberBefore(this string source, string before)
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
    }

    public static class LinqEx
    {
        public static TimeSpan Sum(this IEnumerable<TimeSpan> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Aggregate(TimeSpan.Zero, (current, span) => current + span);
        }
    }
}
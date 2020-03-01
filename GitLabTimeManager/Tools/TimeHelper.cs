using System;
using System.Text.RegularExpressions;

namespace GitLabTimeManager.Tools
{
    internal static class TimeHelper
    {
        private static readonly string monthUnit = "mo";
        private static readonly string weekUnit = "w";
        private static readonly string dayUnit = "d";
        private static readonly string hourUnit = "h";
        private static readonly string minuteUnit = "m";
        private static readonly string secondUnit = "s";

        // Parse spent time in hours
        public static double ParseSpent(this string textDate) => ParseTime(textDate, "of time spent");

        // Parse estimate time in hours
        public static double ParseEstimate(this string textDate) => ParseTime(textDate, "changed time estimate to");

        private static double ParseTime(string textDate, string keyPhase)
        {
            if (!textDate.Contains(keyPhase)) return 0;
            var months = ParseNumberBefore(textDate, monthUnit);
            var weeks = ParseNumberBefore(textDate, weekUnit);
            var days = ParseNumberBefore(textDate, dayUnit);
            var hours = ParseNumberBefore(textDate, hourUnit);
            var minutes = ParseNumberBefore(textDate, minuteUnit);
            var seconds = ParseNumberBefore(textDate, secondUnit);

            return MonthsToHours(months) +
                   WeeksToHours(weeks) +
                   DaysToHours(days) +
                   hours +
                   MinutesToHours(minutes) +
                   SecondsToHours(seconds);
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

        public static double SecondsToHours(this double s) => MinutesToHours(s / 60);

        private static double MinutesToHours(double m) => m / 60;
        
        private static double DaysToHours(double d) => d * 8;
                       
        private static double WeeksToHours(double w) => DaysToHours(w * 5);
                       
        private static double MonthsToHours(double mo) => WeeksToHours(mo * 4);

        public static string ConvertSpent(this TimeSpan ts)
        {
            return $@"/spend {ts.TotalMinutes:####}m";
        }
    }
}

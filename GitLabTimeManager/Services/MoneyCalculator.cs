using System;
using System.Linq;
using GitLabTimeManager.Helpers;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public class MoneyCalculator : IMoneyCalculator
    {
        [NotNull] private ICalendar Calendar { get; }
        private DevelopLevel DevelopLevel { get; } = DevelopLevel.Middle;
        public double MinimalEarning { get; } = 20_000;
        public double DesiredEstimate { get; } = 100;

        private readonly double _workingHours;
        private readonly double _totalHours;

        public MoneyCalculator([NotNull] ICalendar calendar)
        {
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));

            var holidayHours = Calendar.GetHolidays()
                .Where(x => x.Key >= TimeHelper.StartDate && x.Key < TimeHelper.EndDate)
                .Select(x => x.Value)
                .Sum(x => x.TotalHours);

            _workingHours = Calendar.GetWorkingTime(TimeHelper.StartDate, TimeHelper.EndDate).TotalHours;

            _totalHours = _workingHours + holidayHours;
        }

        public double Calculate(TimeSpan estimates)
        {
            //=(70 +(baseEarning-100))*1000
            var baseEarning = DevelopLevel switch
            {
                DevelopLevel.Junior => 40_000,
                DevelopLevel.Middle => 70_000,
                DevelopLevel.Senior => 100_000,
                _ => 0
            };

            var desiredWithHolidays = DesiredEstimate * (_workingHours / _totalHours);

            var earning = baseEarning + (estimates.TotalHours - desiredWithHolidays) * 1000;
            earning = Math.Max(earning, MinimalEarning);
            return earning;
        }
    }

    public interface IMoneyCalculator
    {
        double DesiredEstimate { get; }
        double Calculate(TimeSpan estimates);
        double MinimalEarning { get; }
    }

    public enum DevelopLevel : byte
    {
        Junior, Middle, Senior
    }
}

using System;
using GitLabTimeManager.Helpers;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services;

public class MoneyCalculator([NotNull] ICalendar calendar) : IMoneyCalculator
{
    [NotNull] private ICalendar Calendar { get; } = calendar ?? throw new ArgumentNullException(nameof(calendar));
    private DevelopLevel DevelopLevel { get; } = DevelopLevel.Middle;
    public double MinimalEarning { get; } = 20_000;
    public double DesiredEstimate { get; } = 100;

    public double Calculate(TimeSpan estimates)
    {
        var baseEarning = DevelopLevel switch
        {
            DevelopLevel.Junior => 40_000,
            DevelopLevel.Middle => 70_000,
            DevelopLevel.Senior => 100_000,
            _ => 0
        };

        var holidayHours = Calendar.GetHolidays(TimeHelper.StartDate, TimeHelper.EndDate).TotalHours;
        var workingHours = Calendar.GetWorkingTime(TimeHelper.StartDate, TimeHelper.EndDate).TotalHours;

        var totalHours = workingHours + holidayHours;
        var desiredWithHolidays = DesiredEstimate * (workingHours / totalHours);

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
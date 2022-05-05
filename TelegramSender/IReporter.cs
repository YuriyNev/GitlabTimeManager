using GitLabTimeManager.Services;

namespace TelegramSender;

public interface IReporter
{
    string Name { get; }

    bool CanShow(ReportCollection data);

    bool NeedData(DateTime time);

    string GenerateHtmlReport(ReportCollection data);

    void Reset();

    Schedule Schedule { get; }
}

public class Schedule
{
    public IReadOnlyList<DateTime> Times { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; }

    public bool IsHit(DateTime time)
    {
        if (!DaysOfWeek.Contains(time.DayOfWeek))
            return false;

        if (!Times.Any(x => x.Hour == time.Hour && x.Minute == time.Minute && x.Second == time.Second))
            return false;

        return true;
    }
}
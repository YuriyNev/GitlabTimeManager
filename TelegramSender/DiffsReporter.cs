using System.Text;
using GitLabTimeManager.Services;

namespace TelegramSender;

public class DiffsReporter : IReporter
{
    private ReportCollection _oldReportCollection;

    public string Name { get; }

    public bool CanShow(ReportCollection data)
    {
        if (data.IsEmpty())
            return false;

        // no changes -> ignore
        //if (sortedReportCollection.SequenceEqual(_oldReports[@group], new ReportCollection()))
        //    continue;
        return true;
    }

    public bool NeedData(DateTime time)
    {
        return Schedule.IsHit(time);
    }

    public string GenerateHtmlReport(ReportCollection data)
    {
        var diffs = data.Except(_oldReportCollection, new ReportCollection()).ToList();

        var formattedReportHtml = CreateFormattedReport(data, diffs);

        _oldReportCollection = data.Clone();
        return formattedReportHtml;
    }

    public DiffsReporter(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _oldReportCollection = new ReportCollection();
    }

    public void Reset()
    {
        _oldReportCollection = new ReportCollection();
    }

    public Schedule Schedule { get; } = new Schedule
    {
        Times = new List<DateTime> {new(1970, 1, 1, 12, 00, 0), new(1970, 1, 1, 17, 30, 0),},
        DaysOfWeek = new List<DayOfWeek>() { DayOfWeek.Monday, DayOfWeek.Thursday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Friday},
    };

    private static string CreateFormattedReport(IReadOnlyList<ReportIssue> sortedReportCollection, IReadOnlyList<ReportIssue> changesCollection)
    {
        var stringBuilder = new StringBuilder();
        var monoFormat = "<pre>{0}</pre>";

        try
        {
            var maxIssueUser = sortedReportCollection.MaxBy(x => x.User.Length);
            if (maxIssueUser == null)
                return "<pre>empty list</pre>";

            var maxNameSize = maxIssueUser.User.Length;
            var tabUserSize = maxNameSize + 4;

            //stringBuilder.AppendLine($"{WithDynamicTab("user", tabSize)}{WithDynamicTab("commits", 7)}");

            foreach (var reportIssue in sortedReportCollection)
            {
                stringBuilder.Append($"{reportIssue.User.FitTo(tabUserSize)}");
                stringBuilder.Append($"{$"+{Math.Min(reportIssue.CommitChanges.Additions, 999)}/-{Math.Min(reportIssue.CommitChanges.Deletions, 999)}".FitTo(10)}");
                stringBuilder.Append($"{$"{reportIssue.Comments}".FitTo(3)}");

                var userHasChanged = changesCollection.Any(x => x.User == reportIssue.User);

                if (userHasChanged && reportIssue.HasChanges)
                    stringBuilder.Append($"{$"🔸".FitTo(3)}");

                stringBuilder.AppendLine();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }

        return string.Format(monoFormat, stringBuilder);
    }
}
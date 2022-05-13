using System.Text;
using GitLabTimeManager.Services;

namespace TelegramSender;

public class ChangesReporter : IReporter
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

    public string GenerateHtmlReport(ReportCollection data)
    {
        var diffs = data.Except(_oldReportCollection, new ReportCollection()).ToList();

        var formattedReportHtml = CreateFormattedReport(data, diffs);

        _oldReportCollection = data.Clone();
        return formattedReportHtml;
    }

    public ChangesReporter(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _oldReportCollection = new ReportCollection();
    }

    public void Reset()
    {
        _oldReportCollection = new ReportCollection();
    }

    private static string CreateFormattedReport(IReadOnlyList<ReportIssue> sortedReportCollection, IReadOnlyList<ReportIssue> changesCollection)
    {
        var stringBuilder = new StringBuilder();
        var monoFormat = "<code>{0}</code>";

        try
        {
            var maxIssueUser = sortedReportCollection.MaxBy(x => x.User.Length);
            if (maxIssueUser == null)
                return "<code>empty list</code>";

            var maxNameSize = maxIssueUser.User.Length;
            var tabUserSize = maxNameSize + 4;

            //stringBuilder.AppendLine($"{WithDynamicTab("user", tabSize)}{WithDynamicTab("commits", 7)}");
            var bestUser = sortedReportCollection.MaxBy(x => x.CommitChanges.Additions + x.CommitChanges.Deletions);
            foreach (var reportIssue in sortedReportCollection)
            {
                stringBuilder.Append($"{reportIssue.User.FitTo(tabUserSize)}");
                stringBuilder.Append($"+{Math.Min(reportIssue.CommitChanges.Additions, 999)}/-{Math.Min(reportIssue.CommitChanges.Deletions, 999)}".FitTo(10));
                //stringBuilder.Append($"{reportIssue.Comments}".FitTo(3));

                var userHasChanged = changesCollection.Any(x => x.User == reportIssue.User);

                if (userHasChanged && reportIssue.HasChanges)
                {
                    if (reportIssue == bestUser)
                        stringBuilder.Append($"{"🚀".FitTo(3)}");
                    else 
                        stringBuilder.Append($"{"⬆️".FitTo(5)}");
                }
                else
                {
                    stringBuilder.Append($"{"🙈".FitTo(5)}");
                }
                
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

public class SummaryReporter : IReporter
{
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

    public string GenerateHtmlReport(ReportCollection data)
    {
        var formattedReportHtml = CreateFormattedReport(data, null);

        return formattedReportHtml;
    }

    public SummaryReporter(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void Reset()
    {
    }

    private static string CreateFormattedReport(IReadOnlyList<ReportIssue> sortedReportCollection, IReadOnlyList<ReportIssue> _)
    {
        var stringBuilder = new StringBuilder();
        var monoFormat = "<code>{0}</code>";

        try
        {
            var collection = sortedReportCollection
                .OrderBy(x => x.User)
                .ToList();

            var maxIssueUser = collection.MaxBy(x => x.User.Length);
            if (maxIssueUser == null)
                return "<code>empty list</code>";

            var maxNameSize = maxIssueUser.User.Length;
            var tabUserSize = maxNameSize + 4;

            var prevUser = "";

            foreach (var reportIssue in collection)
            {
                if (prevUser != reportIssue.User)
                {
                    stringBuilder.AppendLine($"{reportIssue.User.FitTo(tabUserSize)}");
                }

                //stringBuilder.Append("</code>");
                //stringBuilder.Append($"<a href=\"{reportIssue.WebUri}\">{reportIssue.Iid}</a>");
                //stringBuilder.Append("<code>");
                if (reportIssue.Iid == 0)
                    stringBuilder.Append("-");
                else
                {
                    stringBuilder.Append($"</code><a href=\"{reportIssue.WebUri}\">#{reportIssue.Iid}</a><code>{new string(' ', 5 - reportIssue.Iid.ToString("D").Length)}{$"{reportIssue.Commits}".FitTo(3)}{$"{reportIssue.Comments}".FitTo(3)}");
                }

                if (reportIssue.Commits + reportIssue.Comments > 0) 
                    stringBuilder.Append("👍");
                
                stringBuilder.AppendLine();

                prevUser = reportIssue.User;
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("issue/commits/report");

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }

        return string.Format(monoFormat, stringBuilder);
    }
}

public class WithoutWorkReporter : IReporter
{
    public string Name { get; }

    public bool CanShow(ReportCollection data)
    {
        if (data.IsEmpty())
            return false;

        return true;
    }

    public string GenerateHtmlReport(ReportCollection data)
    {
        var formattedReportHtml = CreateFormattedReport(data, null);

        return formattedReportHtml;
    }

    public WithoutWorkReporter(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void Reset()
    {
    }

    private static string CreateFormattedReport(IReadOnlyList<ReportIssue> sortedReportCollection, IReadOnlyList<ReportIssue> _)
    {
        var stringBuilder = new StringBuilder();
        var monoFormat = "<code>{0}</code>";

        try
        {
            var collection = sortedReportCollection
                .OrderBy(x => x.User)
                .ToList();

            var maxIssueUser = collection.MaxBy(x => x.User.Length);
            if (maxIssueUser == null)
                return "<code>empty list</code>";

            var maxNameSize = maxIssueUser.User.Length;
            var tabUserSize = maxNameSize + 4;

            //stringBuilder.AppendLine("Без работы:");
            foreach (var reportIssue in collection)
            {
                stringBuilder.AppendLine($"{reportIssue.User.FitTo(tabUserSize)} 😴");
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
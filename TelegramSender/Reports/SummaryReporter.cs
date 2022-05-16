using System.Text;
using GitLabTimeManager.Services;
using TelegramSender.Reports;

namespace TelegramSender;

public class SummaryReporter : IReporter<IssuesReportItem>
{
    public string Name { get; }

    public bool CanShow(IReadOnlyList<IssuesReportItem> data)
    {
        if (data.Count == 0)
            return false;

        // no changes -> ignore
        //if (sortedReportCollection.SequenceEqual(_oldReports[@group], new ReportCollection()))
        //    continue;
        return true;
    }

    public string GenerateHtmlReport(IReadOnlyList<IssuesReportItem> data)
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

    private static string CreateFormattedReport(IReadOnlyList<IssuesReportItem> sortedReportCollection, IReadOnlyList<IssuesReportItem> _)
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
                if (reportIssue.IsEmpty)
                {
                    stringBuilder.Append("🤷‍♂️");
                }
                else
                {
                    if (reportIssue.IsOut)
                        stringBuilder.Append(
                            $"-{new string(' ', 5 - "-".Length)}{$"{reportIssue.Commits}".FitTo(3)}");
                    else
                        stringBuilder.Append(
                            $"</code><a href=\"{reportIssue.WebUri}\">#{reportIssue.Iid}</a><code>{new string(' ', 5 - reportIssue.Iid.ToString("D").Length)}{$"{reportIssue.Commits}".FitTo(3)}{$"{reportIssue.Comments}".FitTo(3)}");
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
using System.Text;
using TelegramSender.Reports;

namespace TelegramSender;

public class ChangesReporter : IReporter<ChangesReportItem>
{
    private ChangesReportCollection _oldReportCollection;

    public string Name { get; }

    public bool CanShow(IReadOnlyList<ChangesReportItem> data)
    {
        if (data.Count == 0)
            return false;

        // no changes -> ignore
        //if (sortedReportCollection.SequenceEqual(_oldReports[@group], new ReportCollection()))
        //    continue;
        return true;
    }

    public string GenerateHtmlReport(IReadOnlyList<ChangesReportItem> data)
    {
        var diffs = data.Except(_oldReportCollection).ToList();

        var formattedReportHtml = CreateFormattedReport(data, diffs);

        _oldReportCollection = new ChangesReportCollection(data.Select(x => x.Clone()));
        return formattedReportHtml;
    }
    
    public ChangesReporter(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _oldReportCollection = new ChangesReportCollection();
    }

    public void Reset()
    {
        _oldReportCollection = new ChangesReportCollection();
    }

    private static string CreateFormattedReport(IReadOnlyList<ChangesReportItem> sortedReportCollection, IReadOnlyList<ChangesReportItem> changesCollection)
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
            var bestUser = sortedReportCollection.MaxBy(x => x.Changes.Additions + x.Changes.Deletions);
            foreach (var reportIssue in sortedReportCollection)
            {
                stringBuilder.Append($"{reportIssue.User.FitTo(tabUserSize)}");
                stringBuilder.Append($"+{Math.Min(reportIssue.Changes.Additions, 999)}/-{Math.Min(reportIssue.Changes.Deletions, 999)}".FitTo(10));
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

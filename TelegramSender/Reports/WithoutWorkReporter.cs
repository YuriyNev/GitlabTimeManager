using System.Text;
using TelegramSender.Reports;

namespace TelegramSender;

public class WithoutWorkReporter : IReporter<UnemployedReportItem>
{
    public string Name { get; }

    public bool CanShow(IReadOnlyList<UnemployedReportItem> data)
    {
        if (data.Count == 0)
            return false;

        return true;
    }

    public string GenerateHtmlReport(IReadOnlyList<UnemployedReportItem> data)
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

    private static string CreateFormattedReport(IReadOnlyList<UnemployedReportItem> sortedReportCollection, IReadOnlyList<UnemployedReportItem> _)
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
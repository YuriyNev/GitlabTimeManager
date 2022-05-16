using GitLabTimeManager.Services;
using TelegramSender.Reports;

namespace TelegramSender;

public interface IReporter<T> where T : IReportItem<T>
{
    public string Name { get; }

    public bool CanShow(IReadOnlyList<T> data);

    public string GenerateHtmlReport(IReadOnlyList<T> data);

    public void Reset();
}
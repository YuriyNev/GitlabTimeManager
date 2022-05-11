using GitLabTimeManager.Services;

namespace TelegramSender;

public interface IReporter
{
    string Name { get; }

    bool CanShow(ReportCollection data);

    string GenerateHtmlReport(ReportCollection data);

    void Reset();
}
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Office.Interop.Excel;

namespace GitLabTimeManager.Services;

public class ExcelExporter
{
    private readonly string _path = Directory.GetCurrentDirectory() + "\\Resources\\ReportTemplate.xltx";

    private const string IidTag = "Iid";
    private const string IssueNameTag = "IssueName";
    private const string IssueEstimateTag = "IssueEstimate";
    private const string IssueSpendTag = "IssueSpend";
    private const string IssuePeriodTag = "IssuePeriod";
    private const string EstimateTag = "Estimate";
    private const string ClosedEstimateTag = "ClosedEstimate";
    private const string OpenEstimateTag = "OpenEstimate";
    private const string SpendStartedInPeriodTag = "SpendStartedInPeriod";
    private const string SpendInPeriodTag = "SpendInPeriod";
    private const string WorkingTimeTag = "WorkingTime";

    public Task<bool> SaveAsync([NotNull] string outPath, [NotNull] ExportData data)
    {
        return Task.Run(() => Save(outPath, data));
    }

    private bool Save([NotNull] string outPath, [NotNull] ExportData data)
    {
        Application excelApp = null;
        Workbook workbook = null;
        try
        {

            excelApp = new Application {DisplayAlerts = false, Visible = false};
            workbook = excelApp.Workbooks.Open(_path);
            dynamic workSheet = workbook.Worksheets[1];

            var issues = data.Issues;
            var stats = data.Statistics;
            var hours = data.WorkingTime;

            for (int i = 0; i < issues.Count - 1; i++)
            {
                var issue = issues[i];

                workSheet.Range[IidTag].Offset(i, 0).Value = issue.Iid;
                workSheet.Range[IssueNameTag].Offset(i, 0).Value = issue.Title;
                workSheet.Range[IssueSpendTag].Offset(i, 0).Value = issue.SpendForPeriod;
                workSheet.Range[IssueEstimateTag].Offset(i, 0).Value = issue.Estimate;

                if (issue.StartTime is null) continue;
                var date = issue.StartTime.Value.Date.AddDays(1 - issue.StartTime.Value.Date.Day);
                workSheet.Range[IssuePeriodTag].Offset(i, 0).Value = date;
            }

            workSheet.Range[EstimateTag].Value = stats.AllEstimatesStartedInPeriod;
            workSheet.Range[ClosedEstimateTag].Value = stats.ClosedEstimatesStartedInPeriod;
            workSheet.Range[SpendStartedInPeriodTag].Value = stats.AllSpendsStartedInPeriod;
            workSheet.Range[SpendInPeriodTag].Value = stats.AllSpendsForPeriod;
            workSheet.Range[WorkingTimeTag].Value = hours.TotalHours;

            workbook.SaveAs(outPath);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (excelApp != null)
            {
                workbook?.Close(0);

                excelApp.Quit();
                Marshal.ReleaseComObject(excelApp);
            }
        }
    }
}

public class ExportData
{
    [NotNull] 
    public GitStatistics Statistics { get; set; }
    [NotNull]
    public ObservableCollection<ReportIssue> Issues { get; set; }
    public TimeSpan WorkingTime { get; set; }
}
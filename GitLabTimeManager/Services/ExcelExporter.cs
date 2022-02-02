using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GitLabApiClient.Models.Users.Responses;
using JetBrains.Annotations;
using Microsoft.Office.Interop.Excel;

namespace GitLabTimeManager.Services
{
    public class ExcelExporter : IExporter
    {
        private readonly string _path = Directory.GetCurrentDirectory() + "\\Resources\\ReportTemplateModified.xltx";

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
        private const string UserTag = "User";
        private const string TaskStateTag = "TaskState";
        /// <summary> Интервал отчета </summary>
        private const string TimeIntervalTag = "BeginTime";//
        private const string IterationsTag = "Iterations";
        /// <summary> Время задачи </summary>
        private const string TimeStartTag = "StartTime";
        private const string CloseTimeTag = "CloseTime";
        private const string DueTimeTag = "DueTime";

        public Task<bool> SaveAsync([NotNull] string outPath, [NotNull] ExportData data,[NotNull] string timeInterval)
        {
            return Task.Run(() => Save(outPath, data,timeInterval));
        }

        private bool Save([NotNull] string outPath, [NotNull] ExportData data,[NotNull] string timeInterval)
        {
            Application excelApp = null;
            Workbook workbook = null;
            try
            {
                excelApp = new Application {DisplayAlerts = false, Visible = false};
                workbook = excelApp.Workbooks.Open(_path);
                var workSheet = workbook.Worksheets[1];
                
                var issues = data.Issues;
                
                var users = data.Users;
                
                int offset = 0;
                workSheet.Range[TimeIntervalTag].Offset(offset, 0).Value = timeInterval;
                
                for (int j = 0; j < users.Count ; j++)
                {

                    if (offset > 0)
                    {
                        offset += 2;
                        workSheet.Range[UserTag].Offset(offset + 1, 0).Value = users[j];
                    }
                    else
                    {
                        offset++;
                        workSheet.Range[UserTag].Offset(offset, 0).Value = users[j];
                    }
                    
                    
                    var userIssues = issues
                        .Where(x => x.User == users[j])
                        .ToList();

                    for (int i = 0; i < userIssues.Count; i++)
                    {
                        var issue = userIssues[i];
                        workSheet.Range[IidTag].Offset(offset, 0).Value = issue.Iid;
                        workSheet.Range[IssueNameTag].Offset(offset, 0).Value = issue.Title;
                        workSheet.Range[IssueEstimateTag].Offset(offset, 0).Value = issue.Estimate;
                        
                        if(issue.TaskState==null)
                            workSheet.Range[TaskStateTag].Offset(offset, 0).Value = "-";
                        else
                            workSheet.Range[TaskStateTag].Offset(offset, 0).Value = issue.TaskState.Name;
                        
                        workSheet.Range[IterationsTag].Offset(offset, 0).Value = issue.Iterations;
                        
                        if (issue.StartTime == null)
                            workSheet.Range[TimeStartTag].Offset(offset, 0).Value = "-";
                        else
                            workSheet.Range[TimeStartTag].Offset(offset, 0).Value = issue.StartTime;

                        if (issue.EndTime == null)
                            workSheet.Range[CloseTimeTag].Offset(offset, 0).Value = "-";
                        else
                            workSheet.Range[CloseTimeTag].Offset(offset, 0).Value = issue.EndTime;
                        
                        if 
                            (issue.DueTime == null) workSheet.Range[DueTimeTag].Offset(offset, 0).Value = "-";
                        else
                            workSheet.Range[DueTimeTag].Offset(offset, 0).Value = issue.DueTime;
                        
                        offset++;
                    }
                }
                
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

    public interface IExporter
    {
        Task<bool> SaveAsync([NotNull] string outPath, [NotNull] ExportData data, [NotNull] string timeInterval);
    }

    public class ExportData
    {
        [NotNull] 
        public GitStatistics Statistics { get; set; }
        [NotNull]
        public ObservableCollection<ReportIssue> Issues { get; set; }
        public TimeSpan WorkingTime { get; set; }
        public IReadOnlyList<string> Users { get; set; }
    }
}
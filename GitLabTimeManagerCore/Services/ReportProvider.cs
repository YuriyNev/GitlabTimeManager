using System.Collections.ObjectModel;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;

namespace GitLabTimeManagerCore.Services
{
    public class ReportProvider : IReportProvider
    {
        private ILabelService LabelService { get; }

        public ReportProvider(ILabelService labelService)
        {
            LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
        }

        public ObservableCollection<ReportIssue> CreateCollection(IEnumerable<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endDate)
        {
            var issues = wrappedIssues.ToList();
            //var issuesCopy = issues.ToList();

            //foreach (var issue in issues)
            //{
            //    var duplicates = issuesCopy
            //        //.Where(x => x.Issue.Iid != issue.Issue.Iid)
            //        .SelectMany(x => x.Commits
            //            .Where(info => issue.Commits
            //            .Any(y => y.CommitId == info.CommitId)));
                
            //    // has common commits for some issues -> reset changes for one 
            //    var commitInfos = duplicates.ToList();
            //    if (commitInfos.Count > 1)
            //    {
            //        var notZeroChanges = commitInfos.Where(x => x.Changes.Additions > 0 || x.Changes.Deletions > 0);
            //        var zeroChanges = notZeroChanges.ToList();
            //        if (zeroChanges.Count == 1)
            //        {
            //            // its ok
            //        }
            //        else
            //        {
            //            // need reset other
            //            //notZeroChanges
            //            foreach (var commitInfo in zeroChanges.Skip(1))
            //            {
            //                commitInfo.Changes = new CommitChanges();
            //            }
            //        }
            //    }
            //}

            return new(
                issues
                    .Where(issue => issue.Issue.Assignee != null)
                    .SelectMany(issue => CreateReportIssue(issue, startDate, endDate)));
        }

        private IEnumerable<ReportIssue> CreateReportIssue(WrappedIssue issue, DateTime startDate, DateTime endDate)
        {
            if (issue.Issue.Assignees.Count <= 0)
                throw new Exception("Empty assignees!");

            foreach (var assignee in issue.Issue.Assignees)
            {
                
                var metrics = issue.GetMetric(LabelService, startDate, endDate);
                
                var reportIssue = new ReportIssue
                {
                    Iid = issue.Issue.Iid,
                    Title = issue.Issue.Title,
                    Estimate = TimeHelper.SecondsToHours(issue.Issue.TimeStats.TimeEstimate),
                    SpendForPeriodByStage = metrics.Duration.TotalHours,
                    Iterations = metrics.Iterations,
                    SpendForPeriod = StatisticsExtractor.SpendsSum(issue, startDate, endDate),
                    Activity = StatisticsExtractor.SpendsSumForPeriod(issue, startDate, endDate),
                    StartTime = issue.StartTime == DateTime.MaxValue ? null : issue.StartTime,
                    EndTime = issue.EndTime == DateTime.MinValue ? null : issue.EndTime,
                    DueTime = issue.DueTime,
                    Commits = issue.Commits
                        .Where(commitInfo => commitInfo.Time >= startDate && commitInfo.Time <= endDate)
                        .ToList(),
                    Comments = issue.Comments
                        .Where(d => d.Author.Username == assignee.Username)
                        .Count(d => d.CreatedAt >= startDate && d.CreatedAt <= endDate),
                    CommitsCount = issue.Commits
                        .Where(commitInfo => commitInfo.Author == assignee.Username)
                        .Count(commitInfo => commitInfo.Time >= startDate && commitInfo.Time <= endDate),
                    CommitChanges = new CommitChanges
                    {
                        Additions = issue.Commits
                            .Where(commitInfo => commitInfo.Author == assignee.Username)
                            .Where(commitInfo => commitInfo.Time >= startDate && commitInfo.Time <= endDate)
                            .Sum(commitInfo => commitInfo.Changes.Additions),
                        Deletions = issue.Commits
                            .Where(commitInfo => commitInfo.Author == assignee.Username)
                            .Where(commitInfo => commitInfo.Time >= startDate && commitInfo.Time <= endDate)
                            .Sum(commitInfo => commitInfo.Changes.Deletions),
                    },
                    User = assignee.Name,
                    //Epic = x.Issue.Epic?.Title,
                    WebUri = issue.Issue.WebUrl,
                    TaskState = issue.Status,
                };
                
                yield return reportIssue;
            }
        }
    }
    
    public interface IReportProvider
    {
        ObservableCollection<ReportIssue> CreateCollection(IEnumerable<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endDate);
    }
}

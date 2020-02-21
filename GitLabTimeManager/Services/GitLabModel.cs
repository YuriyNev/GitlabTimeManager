using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    class GitLabModel : ISourceControl
    {
        private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 14, 16 };
        private const string Token = "xgNy_ZRyTkBTY8o1UyP6";
        private const string Uri = "http://gitlab.domination";

        //private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 16992252 };
        //private const string Token = "KajKr2cVJ4amosry9p4v";
        //private const string Uri = "https://gitlab.com";

        public DateTime Today => DateTime.Today;
        public DateTime MonthStart => Today.AddDays(-Today.Day).AddDays(1);
        public DateTime MonthEnd => MonthStart.AddMonths(1);

        // Задачи открытые на текущий момент
        public ObservableCollection<Issue> OpenIssues { get; set; } = new ObservableCollection<Issue>();
        
        // Задачи закрытые в этом месяце
        public ObservableCollection<Issue> ClosedIssues { get; set; } = new ObservableCollection<Issue>();

        // Число открытых задач
        public int OpenIssuesCount => OpenIssues.Count;

        // Число закрытых задач
        public int ClosedIssuesCount => ClosedIssues.Count;

        // Полное ОЦЕНОЧНОЕ время всех закрытых задач в этом месяце
        public int ClosedTotalEstimate { get; set; }

        // Полное ОЦЕНОЧНОЕ время всех открытых задач
        public int OpenTotalEstimate { get; set; }

        // Полное ПОТРАЧЕННОЕ время всех открытых задач
        public int OpenTotalSpent { get; set; }

        // Полное ПОТРАЧЕННОЕ время закрытых задач
        public int ClosedTotalSpent { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в текущем месяце
        public int ClosedSpendInPeriod { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на открытые задачи в текущем месяце
        public int OpenSpendInPeriod { get; set; }

        // Полное ПОТРАЧЕННОЕ время по всем задачм
        //public int TotalSpent => ClosedTotalSpent + OpenTotalSpent;

        // Полное ОЦЕНОЧНОЕ время по всем задачм (100 ч)
        public int TotalEstimate => ClosedTotalEstimate + OpenTotalEstimate; 

        // Фактическое ПОТРАЧЕННОЕ время по всем задачам за месяц 
        public int TotalSpendInPeriod => ClosedSpendInPeriod + OpenSpendInPeriod;

        private GitLabClient GitLabClient { get; }

        protected internal GitLabModel()
        {
            GitLabClient = new GitLabClient(Uri, Token);
        }

        public async Task<GitResponse> RequestData()
        {
            await ComputeStatistics();
            var response = new GitResponse
            {
                TotalSpendInPeriod = TotalSpendInPeriod,
                ClosedSpendInPeriod =  ClosedSpendInPeriod,
                OpenSpendInPeriod = OpenSpendInPeriod,
            };
            return response;
        }

        private async Task ComputeStatistics()
        {
            ClosedIssues = await GetClosedIssues();

            OpenIssues = await GetOpenIssues();

            var openDetails = new List<TimeSpan>();
            foreach (var issue in OpenIssues)
            {
                var detailedIssueTime = await CollectSpendTime(Convert.ToInt32(issue.ProjectId), issue.Iid).ConfigureAwait(true);
                openDetails.Add(detailedIssueTime);
            }

            OpenTotalSpent = (int)((double)OpenIssues.Sum(x => x.TimeStats.TotalTimeSpent)).SecondsToHours();
            OpenTotalEstimate = (int)((double)OpenIssues.Sum(x => x.TimeStats.TimeEstimate)).SecondsToHours();

            var closedDetails = new List<TimeSpan>();
            foreach (var issue in ClosedIssues)
            {
                var detailedIssueTime = await CollectSpendTime(Convert.ToInt32(issue.ProjectId), issue.Iid).ConfigureAwait(true);
                closedDetails.Add(detailedIssueTime);
            }

            ClosedSpendInPeriod = closedDetails.Sum(x => (int)x.TotalHours);
            OpenSpendInPeriod = openDetails.Sum(x => (int)x.TotalHours);

            ClosedTotalSpent = (int)((double)ClosedIssues.Sum(x => x.TimeStats.TotalTimeSpent)).SecondsToHours();
            ClosedTotalEstimate = (int)((double)ClosedIssues.Sum(x => x.TimeStats.TimeEstimate)).SecondsToHours();

        }

        private async Task<ObservableCollection<Issue>> GetClosedIssues()
        {
            var allIssues = new ObservableCollection<Issue>();
            foreach (var projectId in ProjectIds)
            {
                var issues = await GitLabClient.Issues.GetAsync(projectId, options =>
                {
                    options.State = IssueState.Closed;
                    options.Scope = Scope.AssignedToMe;
                    options.CreatedAfter = Today.AddMonths(-3);
                });
                allIssues.AddRange(issues.Where(x =>
                    x.ClosedAt != null && x.ClosedAt > MonthStart && x.ClosedAt < MonthEnd));
            }
            return allIssues;
        }

        private async Task<ObservableCollection<Issue>> GetOpenIssues()
        {
            var allIssues = new ObservableCollection<Issue>();
            foreach (var projectId in ProjectIds)
            {
                var issues = await GitLabClient.Issues.GetAsync(projectId, options =>
                {
                    options.State = IssueState.Opened;
                    options.Scope = Scope.AssignedToMe;
                });
                allIssues.AddRange(issues);
            }

            return allIssues;
        }

        private async Task<TimeSpan> CollectSpendTime(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId);
            var secondsList = notes.Select(x => x.Body.ParseSpent());
            var seconds = secondsList.Sum();
            var ts = TimeSpan.FromHours(seconds);
            return ts;
        }
    }
}

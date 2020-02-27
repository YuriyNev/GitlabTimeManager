using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Responses;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    class GitLabModel : ISourceControl
    {
        private const string CanDoLabel = "* Можно выполнять";
        private const string InWorkLabel = "* В работе";
        private const string InDistributiveLabel = "* В дистрибутиве";
        private const string RevisionLabel = "* Ревизия";

        //private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 14, 16 };
        //private const string Token = "xgNy_ZRyTkBTY8o1UyP6";
        //private const string Uri = "http://gitlab.domination";

        private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 17053052 };
        private const string Token = "KajKr2cVJ4amosry9p4v";
        private const string Uri = "https://gitlab.com";

        public static DateTime Today => DateTime.Today;
        //public static DateTime Today => DateTime.Today.AddMonths(-2);
        public static DateTime MonthStart => Today.AddDays(-Today.Day).AddDays(1);
        public static DateTime MonthEnd => MonthStart.AddMonths(1);

        private Dictionary<Issue, IList<Note>> AllNotes { get; set; } = new Dictionary<Issue, IList<Note>>();
        
        public ObservableCollection<WrappedIssue> WrappedIssues { get; private set; } = new ObservableCollection<WrappedIssue>();
        public ObservableCollection<Issue> AllIssues { get; set; } = new ObservableCollection<Issue>();
        // Necessary
        // Оценочное время открытых задач, начатых в этом месяце
        public double OpenEstimatesStartedInPeriod { get; private set; }

        // Оценочное время закрытых задач, начатых в этом месяце
        public double ClosedEstimatesStartedInPeriod { get; set; }

        // Потраченное время открытых задач, начатых в этом месяце
        public double OpenSpendsStartedInPeriod { get; set; }

        // Потраченное время закрытых задач, начатых ранее
        public double ClosedSpendsStartedInPeriod { get; set; }

        // Оценочное время открытых задач, начатых ранее
        public double OpenEstimatesStartedBefore { get; set; }

        // Оценочное время закрытых задач, начатых ранее
        public double ClosedEstimatesStartedBefore { get; set; }

        // Потраченное время открытых задач, начатых ранее
        public double OpenSpendsStartedBefore { get; set; }

        // Потраченное время закрытых задач, начатых ранее
        public double ClosedSpendsStartedBefore { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в текущем месяце
        public double ClosedSpendInPeriod { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на открытые задачи в текущем месяце
        public double OpenSpendInPeriod { get; set; }
        
        // Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в этом месяце открытые ранее
        public double ClosedSpendBefore { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на открытые задачи в этом месяце открытые ранее
        public double OpenSpendBefore { get; set; }

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
                OpenEstimatesStartedInPeriod = OpenEstimatesStartedInPeriod,
                ClosedEstimatesStartedInPeriod = ClosedEstimatesStartedInPeriod,
                OpenSpendsStartedInPeriod = OpenSpendsStartedInPeriod,
                ClosedSpendsStartedInPeriod = ClosedSpendsStartedInPeriod,
                OpenEstimatesStartedBefore = OpenEstimatesStartedBefore,
                ClosedEstimatesStartedBefore = ClosedEstimatesStartedBefore,
                OpenSpendsStartedBefore = OpenSpendsStartedBefore,
                ClosedSpendsStartedBefore = ClosedSpendsStartedBefore,

                ClosedSpendInPeriod = ClosedSpendInPeriod,
                OpenSpendInPeriod = OpenSpendInPeriod,
                ClosedSpendBefore = ClosedSpendBefore,
                OpenSpendBefore = OpenSpendBefore,

                WrappedIssues = WrappedIssues,

            };
            return response;
        }

        private async Task ComputeStatistics()
        {
            AllIssues = await RequestAllIssues();
            AllNotes = await GetNotes(AllIssues);

            var openIssues = AllIssues.Where(x => x.State == IssueState.Opened).ToList();
            var closedIssues = AllIssues.Where(x => x.State == IssueState.Closed).ToList();

            WrappedIssues = ExtentIssues(AllIssues, AllNotes, MonthStart, MonthEnd);

            // Most need issues
            // in month
            OpenEstimatesStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));
            
            OpenSpendsStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));
            
            // before
            OpenEstimatesStartedBefore = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            OpenSpendsStartedBefore = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));


            // Потраченное время только в этом месяце
            // На задачи начатые в этом месяце
            OpenSpendBefore = WrappedIssues.Where(x => x.Issue.State == IssueState.Opened && x.StartedIn == false).Sum(x => x.SpendBefore);
            ClosedSpendBefore = WrappedIssues.Where(x => x.Issue.State == IssueState.Closed && x.StartedIn == false).Sum(x => x.SpendBefore);
            OpenSpendInPeriod = WrappedIssues.Where(x => x.Issue.State == IssueState.Opened && x.StartedIn).Sum(x => x.SpendIn);
            ClosedSpendInPeriod = WrappedIssues.Where(x => x.Issue.State == IssueState.Closed && x.StartedIn).Sum(x => x.SpendIn);
        }

        private static ObservableCollection<WrappedIssue> ExtentIssues(IEnumerable<Issue> sourceIssues, IReadOnlyDictionary<Issue, IList<Note>> notes,
            DateTime monthStart, DateTime monthEnd)
        {
            var issues = new ObservableCollection<WrappedIssue>();
            foreach (var issue in sourceIssues)
            {
                notes.TryGetValue(issue, out var note);
                var startDate = note != null && note.Count > 0 ? (DateTime?)note.Min(x => x.CreatedAt) : null;
                var startedIn = note != null && note.Count > 0 && note.Any(x => x.CreatedAt > monthStart && x.CreatedAt < monthEnd);

                var extIssue = new WrappedIssue
                {
                    Issue = issue,
                    Started = startDate,
                    Finished = issue.ClosedAt,
                    SpendIn = CollectSpendTime(note, monthStart, monthEnd).TotalHours,
                    SpendBefore = CollectSpendTime(note, DateTime.MinValue, monthStart).TotalHours,
                    StartedIn = startedIn,
                };
                issues.Add(extIssue);
            }
            return issues;
        }

        private async Task<Dictionary<Issue, IList<Note>>> GetNotes(IEnumerable<Issue> issues)
        {
            var dict = new Dictionary<Issue, IList<Note>>();
            foreach (var issue in issues)
            {
                var notes = await GetNotes(Convert.ToInt32(issue.ProjectId), issue.Iid);
                dict.Add(issue, notes);
            }

            return dict;
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
                    options.CreatedAfter = Today.AddMonths(-6);
                });
                allIssues.AddRange(issues.Where(x =>
                    x.ClosedAt != null && x.ClosedAt > MonthStart && x.ClosedAt < MonthEnd));
            }
            return allIssues;
        }

        private async Task<ObservableCollection<Issue>> GetOpenIssues()
        {
            return await RequestAllIssues();
        }

        private async Task<ObservableCollection<Issue>> RequestAllIssues()
        {
            var allIssues = new ObservableCollection<Issue>();
            foreach (var projectId in ProjectIds)
            {
                var issues = await GitLabClient.Issues.GetAsync(projectId, options =>
                {
                    options.Scope = Scope.AssignedToMe;
                    options.CreatedAfter = Today.AddMonths(-6);
                    options.State = IssueState.All;
                });

                issues = issues.Where(x => x.State == IssueState.Closed && 
                                           x.ClosedAt != null && 
                                           x.ClosedAt > MonthStart && 
                                           x.ClosedAt < MonthEnd || 
                                           x.State == IssueState.Opened).ToList();

                allIssues.AddRange(issues);
            }

            return allIssues;
        }

        private static TimeSpan CollectSpendTime(IEnumerable<Note> notes, DateTime? startTime = null, DateTime? endTime = null)
        {
            var start = startTime ?? DateTime.MinValue;
            var end = endTime ?? DateTime.MaxValue;

            var hoursList = notes
                .Where(x => x.CreatedAt > start && x.CreatedAt < end)
                .Select(x => x.Body.ParseSpent());

            var seconds = hoursList.Sum();
            var ts = TimeSpan.FromHours(seconds);
            return ts;
        }

        private async Task<IList<Note>> GetNotes(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId);
            return notes;
        }

        private static bool StartedInPeriod(IEnumerable<Note> notes, DateTime startTime, DateTime endTime)
        {
            return notes.Any(x => x.CreatedAt > startTime && x.CreatedAt < endTime);
        }

        private static bool FinishedInPeriod(Issue issue, DateTime startTime, DateTime endTime)
        {
            return issue.ClosedAt != null && issue.ClosedAt > startTime 
                                          && issue.ClosedAt <  endTime 
                   || issue.Labels.Contains(RevisionLabel);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Responses;
using GitLabTimeManager.Tools;
using GitLabTimeManager.ViewModel;

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

        private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 16992252 };
        private const string Token = "KajKr2cVJ4amosry9p4v";
        private const string Uri = "https://gitlab.com";

        public static DateTime Today => DateTime.Today;
        //public static DateTime Today => DateTime.Today.AddMonths(-1);
        public static DateTime MonthStart => Today.AddDays(-Today.Day).AddDays(1);
        public static DateTime MonthEnd => MonthStart.AddMonths(1);

        private Dictionary<Issue, IList<Note>> OpenNotes = new Dictionary<Issue, IList<Note>>();
        private Dictionary<Issue, IList<Note>> ClosedNotes = new Dictionary<Issue, IList<Note>>();

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

        // Necessary
        // Оценочное время открытых задач, начатых в этом месяце
        public double OpenEstimatesStartedInPeriod { get; set; }

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
                OpenSpendBefore = OpenSpendBefore

            };
            return response;
        }

        private async Task ComputeStatistics()
        {
            ClosedIssues = await GetClosedIssues();
            OpenIssues = await GetOpenIssues();
            OpenNotes = await GetNotes(OpenIssues);
            ClosedNotes = await GetNotes(ClosedIssues);

            


            OpenTotalSpent = (int)((double)OpenIssues.Sum(x => x.TimeStats.TotalTimeSpent)).SecondsToHours();
            OpenTotalEstimate = (int)((double)OpenIssues.Sum(x => x.TimeStats.TimeEstimate)).SecondsToHours();

            ClosedTotalSpent = (int)((double)ClosedIssues.Sum(x => x.TimeStats.TotalTimeSpent)).SecondsToHours();
            ClosedTotalEstimate = (int)((double)ClosedIssues.Sum(x => x.TimeStats.TimeEstimate)).SecondsToHours();


            // Most need issues
            // in month
            OpenEstimatesStartedInPeriod = OpenIssues
                .Where(issue => StartedInPeriod(OpenNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedInPeriod = ClosedIssues
                .Where(issue => StartedInPeriod(ClosedNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));
            
            OpenSpendsStartedInPeriod = OpenIssues
                .Where(issue => StartedInPeriod(OpenNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedInPeriod = ClosedIssues
                .Where(issue => StartedInPeriod(ClosedNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));
            
            // before
            OpenEstimatesStartedBefore = OpenIssues
                .Where(issue => StartedInPeriod(OpenNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedBefore = ClosedIssues
                .Where(issue => StartedInPeriod(ClosedNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            OpenSpendsStartedBefore = OpenIssues
                .Where(issue => StartedInPeriod(OpenNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedBefore = ClosedIssues
                .Where(issue => StartedInPeriod(ClosedNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimerHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));


            // Потраченное время только в этом месяце

            // На задачи начатые в этом месяце
            var openDetailsInPeriod = new List<TimeSpan>();
            foreach (var issue in OpenIssues)
            {
                OpenNotes.TryGetValue(issue, out var notes);
                if (!StartedInPeriod(notes, MonthStart, MonthEnd)) continue;

                var detailedIssueTime = CollectSpendTime(notes, MonthStart, MonthEnd);
                openDetailsInPeriod.Add(detailedIssueTime);
            }

            var closedDetailsInPeriod = new List<TimeSpan>();
            foreach (var issue in ClosedIssues)
            {
                ClosedNotes.TryGetValue(issue, out var notes);
                if (!StartedInPeriod(notes, MonthStart, MonthEnd)) continue;

                var detailedIssueTime = CollectSpendTime(notes, MonthStart, MonthEnd);
                closedDetailsInPeriod.Add(detailedIssueTime);
            }

            // На задачи начатые ранее
            var openDetailsBefore = new List<TimeSpan>();
            foreach (var issue in OpenIssues)
            {
                OpenNotes.TryGetValue(issue, out var notes);
                if (!StartedInPeriod(notes, DateTime.MinValue, MonthStart)) continue;

                var detailedIssueTime = CollectSpendTime(notes, MonthStart, MonthEnd);
                openDetailsBefore.Add(detailedIssueTime);
            }

            var closedDetailsBefore = new List<TimeSpan>();
            foreach (var issue in ClosedIssues)
            {
                ClosedNotes.TryGetValue(issue, out var notes);
                if (!StartedInPeriod(notes, DateTime.MinValue, MonthStart)) continue;

                var detailedIssueTime = CollectSpendTime(notes, MonthStart, MonthEnd);
                closedDetailsBefore.Add(detailedIssueTime);
            }

            OpenSpendBefore = openDetailsBefore.Sum(x => x.TotalHours);
            ClosedSpendBefore = closedDetailsBefore.Sum(x => x.TotalHours);
            OpenSpendInPeriod = openDetailsInPeriod.Sum(x => x.TotalHours);
            ClosedSpendInPeriod = closedDetailsInPeriod.Sum(x => x.TotalHours);

            //var open = OpenIssues
            //    .Where(issue => StartedInPeriod(OpenNotes[issue], MonthStart, MonthEnd))
            //    .ToList();

            //var closed = ClosedIssues
            //    .Where(issue => StartedInPeriod(ClosedNotes[issue], MonthStart, MonthEnd) &&
            //                    FinishedInPeriod(issue, MonthStart, MonthEnd))
            //    .ToList();

            //Debug.WriteLine("Open");
            //foreach (var issue in open)
            //{
            //    Debug.WriteLine($"#{issue.Iid} {issue.Title}");
            //    Debug.WriteLine($@"{issue.TimeStats.HumanTotalTimeSpent} / {issue.TimeStats.HumanTimeEstimate}");
            //}

            //Debug.WriteLine("");
            //Debug.WriteLine("Closed");
            //foreach (var issue in closed)
            //{
            //    Debug.WriteLine($"#{issue.Iid} {issue.Title}");
            //    Debug.WriteLine($@"{issue.TimeStats.HumanTotalTimeSpent} / {issue.TimeStats.HumanTimeEstimate}");
            //}

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
                    options.CreatedAfter = Today.AddMonths(-5);
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

        private static TimeSpan CollectSpendTime(IEnumerable<Note> notes, DateTime startTime, DateTime endTime)
        {
            var hoursList = notes
                .Where(x => x.CreatedAt > startTime && x.CreatedAt < endTime)
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

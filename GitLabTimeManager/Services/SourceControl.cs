using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Requests;
using GitLabApiClient.Models.Notes.Responses;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    public interface ISourceControl
    {
        Task<GitResponse> RequestDataAsync();
        Task AddSpendAsync(Issue issue, TimeSpan timeSpan);

        Task<bool> StartIssueAsync(Issue issue);
        Task<bool> PauseIssueAsync(Issue issue);
        Task<bool> FinishIssueAsync(Issue issue);
    }

    internal class SourceControl : ISourceControl
    {
#if DEBUG
        private const string ToDoLabel = "To Do";
        private const string DoingLabel = "Doing";
        private const string DistributiveLabel = "";
        private const string RevisionLabel = "Revision";

        private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 17053052 };
        private const string Token = "KajKr2cVJ4amosry9p4v";
        private const string Uri = "https://gitlab.com";
#else
        private const string ToDoLabel = "* Можно выполнять";
        private const string DoingLabel = "* В работе";
        private const string DistributiveLabel = "* В дистрибутиве";
        private const string RevisionLabel = "* Ревизия";

        private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 14, 16 };
        private const string Token = "xgNy_ZRyTkBTY8o1UyP6";
        private const string Uri = "http://gitlab.domination";
#endif

        private static DateTime Today => DateTime.Today;
        private static DateTime MonthStart => Today.AddDays(-Today.Day).AddDays(1);
        private static DateTime MonthEnd => MonthStart.AddMonths(1);

        private Dictionary<Issue, IList<Note>> AllNotes { get; set; } = new Dictionary<Issue, IList<Note>>();

        private ObservableCollection<WrappedIssue> WrappedIssues { get; set; } = new ObservableCollection<WrappedIssue>();

        private ObservableCollection<Issue> AllIssues { get; set; } = new ObservableCollection<Issue>();

#region Stats Properties
        // Necessary
        // Оценочное время открытых задач, начатых в этом месяце
        private double OpenEstimatesStartedInPeriod { get; set; }

        // Оценочное время закрытых задач, начатых в этом месяце
        private double ClosedEstimatesStartedInPeriod { get; set; }

        // Потраченное время открытых задач, начатых в этом месяце
        private double OpenSpendsStartedInPeriod { get; set; }

        // Потраченное время закрытых задач, начатых ранее
        private double ClosedSpendsStartedInPeriod { get; set; }

        // Оценочное время открытых задач, начатых ранее
        private double OpenEstimatesStartedBefore { get; set; }

        // Оценочное время закрытых задач, начатых ранее
        private double ClosedEstimatesStartedBefore { get; set; }

        // Потраченное время открытых задач, начатых ранее
        private double OpenSpendsStartedBefore { get; set; }

        // Потраченное время закрытых задач, начатых ранее
        private double ClosedSpendsStartedBefore { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в текущем месяце
        private double ClosedSpendInPeriod { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на открытые задачи в текущем месяце
        private double OpenSpendInPeriod { get; set; }
        
        // Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в этом месяце открытые ранее
        private double ClosedSpendBefore { get; set; }

        // Фактическое время ПОТРАЧЕННОЕ на открытые задачи в этом месяце открытые ранее
        private double OpenSpendBefore { get; set; }

#endregion

        private GitLabClient GitLabClient { get; }

        protected internal SourceControl()
        {
            GitLabClient = new GitLabClient(Uri, Token);
        }

        public async Task<GitResponse> RequestDataAsync()
        {
            await ComputeStatistics().ConfigureAwait(true);
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

        public async Task AddSpendAsync(Issue issue, TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.FromSeconds(10))
            {
                await Task.Delay(0);
                return;
            }
            var request = new CreateIssueNoteRequest(timeSpan.ConvertSpent() + "\n" + "[]");
            var note = await GitLabClient.Issues.CreateNoteAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            await GitLabClient.Issues.DeleteNoteAsync(issue.ProjectId, issue.Iid, note.Id).ConfigureAwait(false);
        }

        public async Task<bool> StartIssueAsync(Issue issue)
        {
            if (issue.Labels.Contains(DoingLabel))
            {
                await Task.Delay(0).ConfigureAwait(false);
                return true;
            }
            
            issue.Labels.Remove(ToDoLabel);
            issue.Labels.Remove(RevisionLabel);

            issue.Labels.Add(DoingLabel);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }
        
        public async Task<bool> PauseIssueAsync(Issue issue)
        {
            if (!issue.Labels.Contains(DoingLabel))
            {
                await Task.Delay(0);
                return true;
            }
            
            issue.Labels.Remove(DoingLabel);
            issue.Labels.Add(ToDoLabel);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> FinishIssueAsync(Issue issue)
        {
            if (!issue.Labels.Contains(DoingLabel))
            {
                await Task.Delay(0);
                return true;
            }
            
            issue.Labels.Remove(DoingLabel);
            issue.Labels.Add(RevisionLabel);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }

        private async Task ComputeStatistics()
        {
            AllIssues = await RequestAllIssuesAsync().ConfigureAwait(false);
            AllNotes = await GetNotes(AllIssues).ConfigureAwait(false);

            var openIssues = AllIssues.Where(x => x.State == IssueState.Opened).ToList();
            var closedIssues = AllIssues.Where(x => x.State == IssueState.Closed).ToList();

            WrappedIssues = ExtentIssues(AllIssues, AllNotes, MonthStart, MonthEnd);

            // Most need issues
            // in month
            OpenEstimatesStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));
            
            OpenSpendsStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));
            
            // before
            OpenEstimatesStartedBefore = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            OpenSpendsStartedBefore = openIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));


            // Потраченное время только в этом месяце
            // На задачи начатые в этом месяце
            OpenSpendBefore = WrappedIssues.Where(x => x.Issue.State == IssueState.Opened && x.StartedIn == false).Sum(x => x.SpendIn);
            ClosedSpendBefore = WrappedIssues.Where(x => x.Issue.State == IssueState.Closed && x.StartedIn == false).Sum(x => x.SpendIn);
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
                var notes = await GetNotesAsync(Convert.ToInt32(issue.ProjectId), issue.Iid);
                dict.Add(issue, notes);
            }

            return dict;
        }

        private async Task<ObservableCollection<Issue>> RequestAllIssuesAsync()
        {
            var allIssues = new ObservableCollection<Issue>();
            foreach (var projectId in ProjectIds)
            {
                var issues = await GitLabClient.Issues.GetAsync(projectId, options =>
                {
                    options.Scope = Scope.AssignedToMe;
                    options.CreatedAfter = Today.AddMonths(-6);
                    options.State = IssueState.All;
                }).ConfigureAwait(false);

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

        private async Task<IList<Note>> GetNotesAsync(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId).ConfigureAwait(false);
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

    public class GitResponse
    {
        /// <summary> Most need properties  </summary>
        public double OpenEstimatesStartedInPeriod { get; set; }
        public double ClosedEstimatesStartedInPeriod { get; set; }
        public double ClosedSpendsStartedInPeriod { get; set; }
        public double OpenSpendsStartedInPeriod { get; set; }
        public double OpenEstimatesStartedBefore { get; set; }
        public double ClosedEstimatesStartedBefore { get; set; }
        public double OpenSpendsStartedBefore { get; set; }
        public double ClosedSpendsStartedBefore { get; set; }

        public double ClosedSpendInPeriod { get; set; }
        public double OpenSpendInPeriod { get; set; }
        public double OpenSpendBefore { get; set; }
        public double ClosedSpendBefore { get; set; }

        public ObservableCollection<WrappedIssue> WrappedIssues { get; set; }
    }
}

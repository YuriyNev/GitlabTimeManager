using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Requests;
using GitLabApiClient.Models.Notes.Responses;
using GitLabTimeManager.Helpers;

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
        //private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 17053052 };
        //private const string Token = "KajKr2cVJ4amosry9p4v";
        //private const string Uri = "https://gitlab.com";

        private static int ClientDominationId = 14;
        private static int AnalyticsServerId = 16;


        private static readonly IReadOnlyList<int> ProjectIds = new List<int>
        {
            ClientDominationId, AnalyticsServerId
        };
        private const string Token = "gTUPn2KdhEFUMR3oQL81";
        private const string Uri = "http://gitlab.domination";
#else
        private static int ClientDominationId = 14;
        private static int AnalyticsServerId = 16;

        private static readonly IReadOnlyList<int> ProjectIds = new List<int>
        {
            ClientDominationId, AnalyticsServerId
        };
        private const string Token = "gTUPn2KdhEFUMR3oQL81";
        private const string Uri = "http://gitlab.domination";
#endif
        // dont calc
        private static readonly IReadOnlyList<LabelEx> ExcludeLabels = new List<LabelEx>
        {
            LabelsCollection.ProjectControlLabel
        };

        private static DateTime Today => DateTime.Today;
        private static DateTime MonthStart => Today.AddDays(-Today.Day).AddDays(1);
        private static DateTime MonthEnd => MonthStart.AddMonths(1);

        private Dictionary<Issue, IList<Note>> AllNotes { get; set; } = new Dictionary<Issue, IList<Note>>();

        private ObservableCollection<WrappedIssue> WrappedIssues { get; set; } = new ObservableCollection<WrappedIssue>();

        private ObservableCollection<Issue> AllIssues { get; set; } = new ObservableCollection<Issue>();

#region Stats Properties

        /// <summary> Оценочное время открытых задач, начатых в этом месяце </summary>
        private double OpenEstimatesStartedInPeriod { get; set; }

        /// <summary> Оценочное время закрытых задач, начатых в этом месяце </summary>
        private double ClosedEstimatesStartedInPeriod { get; set; }

        /// <summary> Потраченное время открытых задач, начатых в этом месяце </summary>
        private double OpenSpendsStartedInPeriod { get; set; }

        /// <summary> Потраченное время закрытых задач, начатых ранее </summary>
        private double ClosedSpendsStartedInPeriod { get; set; }

        /// <summary> Оценочное время открытых задач, начатых ранее </summary>
        private double OpenEstimatesStartedBefore { get; set; }

        /// <summary> Оценочное время закрытых задач, начатых ранее </summary>
        private double ClosedEstimatesStartedBefore { get; set; }

        /// <summary> отраченное время открытых задач, начатых ранее </summary>
        private double OpenSpendsStartedBefore { get; set; }

        /// <summary> Потраченное время закрытых задач, начатых ранее </summary>
        private double ClosedSpendsStartedBefore { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в текущем месяце </summary>
        private double ClosedSpendInPeriod { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на открытые задачи в текущем месяце </summary>
        private double OpenSpendInPeriod { get; set; }
        
        /// <summary> Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в этом месяце открытые ранее </summary>
        private double ClosedSpendBefore { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на открытые задачи в этом месяце открытые ранее </summary>
        private double OpenSpendBefore { get; set; }

        /// <summary> Оценочное время за сегодня </summary>
        private double AllTodayEstimates { get; set; }

#endregion

        private GitLabClient GitLabClient { get; }

        public SourceControl()
        {
            GitLabClient = new GitLabClient(Uri, Token);
        }

        public async Task<GitResponse> RequestDataAsync()
        {
            await ComputeStatisticsAsync().ConfigureAwait(true);
            var response = new GitResponse
            {
                StartDate = MonthStart,
                EndDate = MonthEnd,
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

                AllTodayEstimates = AllTodayEstimates,

                WrappedIssues = WrappedIssues,

            };
            return response;
        }

        public async Task AddSpendAsync(Issue issue, TimeSpan timeSpan)
        {
#if DEBUG
            if (timeSpan < TimeSpan.FromSeconds(10)) return;
#else
            if (timeSpan < TimeSpan.FromMinutes(5)) return;
#endif
            var request = new CreateIssueNoteRequest(timeSpan.ConvertSpent() + "\n" + "[]");
            var note = await GitLabClient.Issues.CreateNoteAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            await GitLabClient.Issues.DeleteNoteAsync(issue.ProjectId, issue.Iid, note.Id).ConfigureAwait(false);
        }

        public async Task<bool> StartIssueAsync(Issue issue)
        {
            if (LabelProcessor.IsStarted(issue.Labels)) 
                return true;

            LabelProcessor.StartIssue(issue.Labels);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };

            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }
        
        public async Task<bool> PauseIssueAsync(Issue issue)
        {
            if (LabelProcessor.IsPaused(issue.Labels))
                return true;

            LabelProcessor.PauseIssue(issue.Labels);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> FinishIssueAsync(Issue issue)
        {
            LabelProcessor.FinishIssue(issue.Labels);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }

        private async Task ComputeStatisticsAsync()
        {
            AllIssues = await RequestAllIssuesAsync().ConfigureAwait(false);
            AllNotes = await GetNotesAsync(AllIssues).ConfigureAwait(false);

            var openIssues = AllIssues.Where(IsOpen).ToList();
            var closedIssues = AllIssues.Where(IsClosed).ToList();

            WrappedIssues = ExtentIssues(AllIssues, AllNotes, MonthStart, MonthEnd);

            // Most need issues
            // started in month
            OpenEstimatesStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            OpenSpendsStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], MonthStart, MonthEnd) &&
                                FinishedInPeriod(issue, MonthStart, MonthEnd))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));
            
            // started before this month
            OpenEstimatesStartedBefore = openIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            ClosedEstimatesStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));

            AllTodayEstimates = AllIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], DateTime.Today, DateTime.Today.AddDays(1)))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TimeEstimate));
            
            foreach (var wrappedIssue in closedIssues) Debug.WriteLine($"{wrappedIssue.Iid} {wrappedIssue.TimeStats.HumanTimeEstimate}");


            OpenSpendsStartedBefore = openIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            ClosedSpendsStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(issue, AllNotes[issue], DateTime.MinValue, MonthStart))
                .Sum(x => TimeHelper.SecondsToHours(x.TimeStats.TotalTimeSpent));

            // Потраченное время только в этом месяце
            // На задачи начатые в этом месяце
            var withoutExcludes = WrappedIssues.Where(x => !x.LabelExes.IsExcludeLabels()).ToList();

            OpenSpendBefore = withoutExcludes.
                Where(x => IsOpen(x.Issue) && !x.StartedIn).Sum(x => x.SpendIn);
            ClosedSpendBefore = withoutExcludes.
                Where(x => IsClosed(x.Issue) && !x.StartedIn).Sum(x => x.SpendIn);
            OpenSpendInPeriod = withoutExcludes.
                Where(x => IsOpen(x.Issue) && x.StartedIn).Sum(x => x.SpendIn);
            ClosedSpendInPeriod = withoutExcludes.
                Where(x => IsClosed(x.Issue) && x.StartedIn).Sum(x => x.SpendIn);

            foreach (var wrappedIssue in WrappedIssues) Debug.WriteLine(wrappedIssue);
        }


        /// <summary> Задача открыта и не находится на проверке </summary>
        private static bool IsOpen(Issue issue) => issue.State == IssueState.Opened;

        /// <summary> Задача условно закрыта</summary>
        private static bool IsClosed(Issue issue) => !IsOpen(issue);

        private static DateTime? FinishTime(Issue issue) => issue.ClosedAt;

        private static ObservableCollection<WrappedIssue> ExtentIssues(IEnumerable<Issue> sourceIssues, IReadOnlyDictionary<Issue, IList<Note>> notes,
            DateTime monthStart, DateTime monthEnd)
        {
            var issues = new ObservableCollection<WrappedIssue>();
            var labelsEx = new ObservableCollection<LabelEx>();
            foreach (var issue in sourceIssues)
            {
                notes.TryGetValue(issue, out var note);

                DateTime? startDate = null;
                var startedIn = false;
                double spendIn = 0;
                double spendBefore = 0;

                double totalSpend = TimeHelper.SecondsToHours(issue.TimeStats.TotalTimeSpent);
                if (note != null && note.Count > 0)
                {
                    // if more 0 notes then getting data from notes
                    var lst = note.Where(x => x.Body.ParseEstimate() > 0).ToList();
                    if (lst.Count > 0)
                        startDate = lst.Min(x => x.CreatedAt);
                    startedIn = !note.Any(x => x.CreatedAt < monthStart);

                    spendIn = CollectSpendTime(note, monthStart, monthEnd).TotalHours;
                    spendBefore = CollectSpendTime(note, DateTime.MinValue, monthStart).TotalHours;
                }
                
                // spend is set when issue was created
                var startSpend = totalSpend - (spendIn + spendBefore);
                if (startSpend > 0)
                {
                    if (startedIn)
                        spendIn += startSpend;
                    else
                        spendBefore += startSpend;
                }

                LabelProcessor.UpdateLabelsEx(labelsEx, issue.Labels);

                var extIssue = new WrappedIssue
                {
                    Issue = issue,
                    StartTime = startDate,
                    EndTime = FinishTime(issue),
                    SpendIn = spendIn,
                    SpendBefore = spendBefore,
                    StartedIn = startedIn,
                    LabelExes = new ObservableCollection<LabelEx>(labelsEx),
                };
                
                issues.Add(extIssue);
            }
            return issues;
        }

        private async Task<Dictionary<Issue, IList<Note>>> GetNotesAsync(IEnumerable<Issue> issues)
        {
            var dict = new Dictionary<Issue, IList<Note>>();
            foreach (var issue in issues)
            {
                var notes = await GetNotesAsync(Convert.ToInt32(issue.ProjectId), issue.Iid).ConfigureAwait(false);
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

            var hours = hoursList.Sum();
            return TimeSpan.FromHours(hours);
        }

        private async Task<IList<Note>> GetNotesAsync(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId).ConfigureAwait(false);
            return notes;
        }

        private static bool StartedInPeriod(Issue issue, IEnumerable<Note> notes, DateTime startTime, DateTime endTime)
        {
            var enumerable = notes.ToList();
            if (!enumerable.Any()) return issue.CreatedAt > startTime && issue.TimeStats.TimeEstimate > 0;
            
            foreach (var note in enumerable.Where(note => note.Body.ParseEstimate() > 0))
                return note.CreatedAt > startTime;

            return !enumerable.Any(x => x.CreatedAt < startTime);

        }

        private static bool FinishedInPeriod(Issue issue, DateTime startTime, DateTime endTime)
        {
            return issue.ClosedAt != null && issue.ClosedAt > startTime 
                                          && issue.ClosedAt < endTime;
        }

        public void Dispose()
        {
        }

        public event EventHandler<GitResponse> NewData;
    }

    public class GitResponse
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
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

        public double AllTodayEstimates { get; set; }
        
        public ObservableCollection<WrappedIssue> WrappedIssues { get; set; }
    }
}

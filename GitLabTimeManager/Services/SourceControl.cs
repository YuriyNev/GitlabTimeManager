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
using GitLabApiClient.Models.Projects.Responses;
using GitLabTimeManager.Helpers;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public interface ISourceControl
    {
        [PublicAPI] Task<GitResponse> RequestDataAsync();
        [PublicAPI] Task AddSpendAsync(Issue issue, TimeSpan timeSpan);
        [PublicAPI] Task SetEstimateAsync(Issue issue, TimeSpan timeSpan);
        [PublicAPI] Task<bool> StartIssueAsync(Issue issue);
        [PublicAPI] Task<bool> PauseIssueAsync(Issue issue);
        [PublicAPI] Task<bool> FinishIssueAsync(Issue issue);
        [PublicAPI] Task<IReadOnlyList<Label>> GetAllLabels();
        [PublicAPI] Task<Issue> UpdateIssueAsync(Issue issue, UpdateIssueRequest request);
    }

    internal class SourceControl : ISourceControl
    {
        private IUserProfile UserProfile { get; }
        private GitLabClient GitLabClient { get; }
        
        public SourceControl([NotNull] IUserProfile userProfile)
        {
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            
            GitLabClient = new GitLabClient(UserProfile.Url, UserProfile.Token);
        }

        public async Task<GitResponse> RequestDataAsync()
        {
            var wrappedIssues = await GetPreparedDataAsync().ConfigureAwait(false);

            return new GitResponse
            {
                WrappedIssues = new ObservableCollection<WrappedIssue>(wrappedIssues),
            };
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

        public async Task SetEstimateAsync(Issue issue, TimeSpan timeSpan)
        {
            var request = new CreateIssueNoteRequest(timeSpan.ConvertEstimate() + "\n" + "[]");
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

        public async Task<IReadOnlyList<Label>> GetAllLabels()
        {
            var projects = await GitLabClient.Projects.GetAsync();
            var all = new Collection<Label>();

            foreach (var project in projects)
            {
                var labels = await GitLabClient.Projects.GetLabelsAsync(project.Id);
                all.AddRange(labels);
            }

            var dist = all.Distinct(new Comparison());
            return dist.ToList();
        }

        public Task<Issue> UpdateIssueAsync(Issue issue, UpdateIssueRequest request)
        {
            return GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request);
        }

        //public Task GetLabels()
        //{
        //    var labels = GitLabClient.Projects.GetLabelsAsync();

        //}

        private class Comparison : IEqualityComparer<Label>
        {
            public bool Equals(Label x, Label y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(Label obj)
            {
                unchecked
                {
                    var hashCode = obj.Id;
                    hashCode = (hashCode * 397) ^ (obj.Name != null ? obj.Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.Color != null ? obj.Color.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.Description != null ? obj.Description.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ obj.Priority.GetHashCode();
                    return hashCode;
                }
            }
        }

        private bool _isAction;

        private async Task<IEnumerable<WrappedIssue>> GetPreparedDataAsync()
        {
            if (_isAction)
                return null;

            try
            {
                _isAction = true;

                var allIssues = await RequestAllIssuesAsync().ConfigureAwait(false);
                var allNotes = await GetNotesAsync(allIssues).ConfigureAwait(false);

                return ExtentIssues(allIssues, allNotes);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return Array.Empty<WrappedIssue>();
            }
            finally
            { 
                _isAction = false;
            }
        }

        private static DateTime? FinishTime(Issue issue) => issue.ClosedAt;

        private static IEnumerable<WrappedIssue> ExtentIssues(
            IEnumerable<Issue> sourceIssues, 
            IReadOnlyDictionary<Issue, IList<Note>> notes)
        {
            var issues = new ObservableCollection<WrappedIssue>();
            var labelsEx = new ObservableCollection<LabelEx>();
            foreach (var issue in sourceIssues)
            {
                notes.TryGetValue(issue, out var note);

                DateTime? startDate = null;

                double totalSpend = TimeHelper.SecondsToHours(issue.TimeStats.TotalTimeSpent);
                double totalYieldSpend = 0.0;
                var spendDictionary = new Dictionary<DateRange, double>();

                if (note != null && note.Count > 0)
                {
                    // if more 0 notes then getting data from notes
                    var timeNotes = note.
                        Where(x => x.Body.ParseSpent() > 0 || x.Body.ParseEstimate() > 0).
                        ToList();

                    if (timeNotes.Count > 0)
                        startDate = timeNotes.Min(x => x.CreatedAt);

                    var currentDate = TimeHelper.StartPastDate;
                    var endDate = TimeHelper.EndDate;

                    while (currentDate < endDate)
                    {
                        var endPeriodDate = currentDate.
                            AddDays(1).
                            AddTicks(-1);

                        var spend = CollectSpendTime(timeNotes, currentDate, endPeriodDate).TotalHours;
                        spendDictionary.Add(new DateRange(currentDate, endPeriodDate), spend);
                        totalYieldSpend += spend;
                        currentDate = currentDate.AddDays(1);
                    }
                }
                
                // spend is set when issue was created
                var startSpend = totalSpend - totalYieldSpend;
                if (startSpend > 0 && spendDictionary.Count > 0)
                {
                    var minDate = spendDictionary.Keys.Min(x => x.StartDate);
                    var minKey = spendDictionary.Keys.First(x => x.StartDate == minDate);
                    spendDictionary[minKey] += startSpend;
                }

                LabelProcessor.UpdateLabelsEx(labelsEx, issue.Labels);

                var extIssue = new WrappedIssue
                {
                    Issue = issue,
                    StartTime = startDate,
                    EndTime = FinishTime(issue),
                    Spends = spendDictionary,
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
            var issues = await GitLabClient.Issues.GetAllAsync(projectId: null, groupId: null, options: Options).ConfigureAwait(false);

            var allIssues = new ObservableCollection<Issue>(issues);

            return allIssues;
        }

        private void Options(IssuesQueryOptions options)
        {
            options.Scope = Scope.AssignedToMe;
            options.CreatedAfter = TimeHelper.Today.AddMonths(-UserProfile.RequestMonths);
            options.State = IssueState.All;
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
    }

    public readonly struct DateRange : IEquatable<DateRange>
    {
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }

        public bool Equals(DateRange other)
        {
            return StartDate.Equals(other.StartDate) && EndDate.Equals(other.EndDate);
        }

        public override bool Equals(object obj)
        {
            return obj is DateRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartDate.GetHashCode() * 397) ^ EndDate.GetHashCode();
            }
        }

        public DateRange(DateTime startDate, DateTime endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }

    public class GitResponse
    {
        public ObservableCollection<WrappedIssue> WrappedIssues { get; set; }
    }
}
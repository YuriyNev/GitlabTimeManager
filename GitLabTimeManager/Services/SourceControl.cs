using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Requests;
using GitLabApiClient.Models.Notes.Responses;
using GitLabApiClient.Models.Projects.Responses;
using GitLabApiClient.Models.Users.Responses;
using GitLabTimeManager.Helpers;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public interface ISourceControl
    {
        IReadOnlyList<string> CurrentUsers { get; }
        DateTime StartTime { get; }
        DateTime EndTime { get; }

        [PublicAPI] Task<GitResponse> RequestDataAsync(DateTime start, DateTime end, IReadOnlyList<string> users);
        [PublicAPI] Task<GitResponse> RequestNewestDataAsync();
        [PublicAPI] Task AddSpendAsync(Issue issue, TimeSpan timeSpan);
        [PublicAPI] Task SetEstimateAsync(Issue issue, TimeSpan timeSpan);
        [PublicAPI] Task<WrappedIssue> StartIssueAsync(WrappedIssue issue);
        [PublicAPI] Task<WrappedIssue> PauseIssueAsync(WrappedIssue issue);
        [PublicAPI] Task<WrappedIssue> FinishIssueAsync(WrappedIssue issue);
        [PublicAPI] Task<Issue> UpdateIssueAsync(Issue issue, UpdateIssueRequest request);
        
        [PublicAPI] Task<IReadOnlyList<Label>> FetchLabelsAsync([CanBeNull] IReadOnlyList<ProjectId> projects = null);
        [PublicAPI] IReadOnlyList<Label> GetLabels();
        
        [PublicAPI] Task<IReadOnlyList<GroupLabel>> FetchGroupLabelsAsync([CanBeNull] IReadOnlyList<GroupId> groups = null);
        [PublicAPI] IReadOnlyList<GroupLabel> GetGroupLabels();

        [PublicAPI] IReadOnlyList<ProjectId> FetchActiveProjects(IReadOnlyList<Issue> issues);
        [PublicAPI] IReadOnlyList<ProjectId> GetActiveProjects();

        [PublicAPI] Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync(int projectId, int issueIid);

        [PublicAPI] Task<IList<User>> GetAllUsersAsync();
    }

    internal class SourceControl : ISourceControl
    {
        [NotNull] private IUserProfile UserProfile { get; }
        [NotNull] private ILabelService LabelService { get; }
        [NotNull] private IHttpService HttpService { get; }
        [NotNull] private GitLabClient GitLabClient { get; }

        private IReadOnlyList<Label> CachedLabels { get; set; }
        private IReadOnlyList<GroupLabel> CachedGroupLabels { get; set; }
        private IReadOnlyList<ProjectId> CachedProjects { get; set; }

        public IReadOnlyList<string> CurrentUsers { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        private bool IsSingleUser => CurrentUsers.Count == 1;

        public SourceControl(
            [NotNull] IUserProfile userProfile,
            [NotNull] ILabelService labelService,
            [NotNull] IHttpService httpService)
        {
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
            HttpService = httpService ?? throw new ArgumentNullException(nameof(httpService));

            GitLabClient = new GitLabClient(UserProfile.Url, UserProfile.Token);
        }
        
        public async Task<GitResponse> RequestDataAsync(DateTime start, DateTime end, IReadOnlyList<string> users)
        {
            var wrappedIssues = await GetRawDataAsync(start, end, users).ConfigureAwait(false);

            return new GitResponse
            {
                WrappedIssues = new ObservableCollection<WrappedIssue>(wrappedIssues),
            };
        }

        /// <summary> Unused </summary>
        public async Task<GitResponse> RequestNewestDataAsync()
        {
            var wrappedIssues = await GetActualDataAsync().ConfigureAwait(false);

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

        public async Task<WrappedIssue> StartIssueAsync(WrappedIssue issue)
        {
            if (LabelService.InWork(issue.Issue.Labels)) 
                return issue;

            var newLabels = LabelService.StartIssue(issue.Issue.Labels).ToList();

            return await UpdateIssueLabelsAsync(issue, newLabels).ConfigureAwait(false);
        }

        public async Task<WrappedIssue> PauseIssueAsync(WrappedIssue issue)
        {
            if (LabelService.IsPaused(issue.Issue.Labels))
                return issue;

            var newLabels = LabelService.PauseIssue(issue.Issue.Labels).ToList();

            return await UpdateIssueLabelsAsync(issue, newLabels).ConfigureAwait(false);
        }

        public async Task<WrappedIssue> FinishIssueAsync(WrappedIssue issue)
        {
            var newLabels = LabelService.FinishIssue(issue.Issue.Labels).ToList();

            return await UpdateIssueLabelsAsync(issue, newLabels).ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<Label>> GetLabelsCoreAsync([CanBeNull] IReadOnlyList<ProjectId> projects)
        {
            var all = new Collection<Label>();
            IReadOnlyList<ProjectId> allProjects;
            if (projects != null)
            {
                allProjects = projects;
            }
            else
            {
                var projectList = await GitLabClient.Projects.GetAsync().ConfigureAwait(false);
                allProjects = projectList.Select(x => (ProjectId)x.Id).ToList();
            }

            foreach (var project in allProjects)
            {
                var labels = await GitLabClient.Projects.GetLabelsAsync(project).ConfigureAwait(false);
                all.AddRange(labels);
            }
            var dist = all.Distinct(new Comparison()).ToList();
            return dist;
        }

        public Task<Issue> UpdateIssueAsync(Issue issue, UpdateIssueRequest request)
        {
            return GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request);
        }

        public async Task<IReadOnlyList<Label>> FetchLabelsAsync(IReadOnlyList<ProjectId> projects)
        {
            var labels = await GetLabelsCoreAsync(projects);

            CachedLabels = labels;

            return labels;
        }

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

        private async Task<IReadOnlyList<WrappedIssue>> GetRawDataAsync(DateTime start, DateTime end, IReadOnlyList<string> users)
        {
            void AllDataOptions(IssuesQueryOptions options)
            {
                if (IsSingleUser)
                    options.AssigneeUsername = new List<string> { CurrentUsers.FirstOrDefault() };

                options.UpdatedAfter = StartTime;
                options.UpdatedBefore = EndTime;
                options.State = IssueState.All;
            }

            if (_isAction)
                return null;

            try
            {
                _isAction = true;

                StartTime = start;
                EndTime = end;
                CurrentUsers = users;

                var allIssues = await RequestAllIssuesAsync(AllDataOptions).ConfigureAwait(false);

                var projects = FetchActiveProjects(allIssues);

                var allLabels = await FetchLabelsAsync(projects).ConfigureAwait(false);

                var allNotes = IsSingleUser
                    ? await GetNotesAsync(allIssues).ConfigureAwait(false)
                    : new Dictionary<Issue, IReadOnlyList<Note>>();

                var allEvents = await GetAllLabelActionsAsync(allIssues).ConfigureAwait(false);

                return ExtentIssues(allIssues, allNotes, allLabels, allEvents);
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

        private async Task<IReadOnlyList<WrappedIssue>> GetActualDataAsync()
        {
            if (_isAction)
                return null;

            try
            {
                _isAction = true;

                var allIssues = await RequestAllIssuesAsync(ActualDataOptions).ConfigureAwait(false);

                var projects = FetchActiveProjects(allIssues);

                var allLabels = await FetchLabelsAsync(projects).ConfigureAwait(false);

                var allNotes = IsSingleUser 
                    ? await GetNotesAsync(allIssues).ConfigureAwait(false)
                    : new Dictionary<Issue, IReadOnlyList<Note>>();

                var allEvents = await GetAllLabelActionsAsync(allIssues).ConfigureAwait(false);

                return ExtentIssues(allIssues, allNotes, allLabels, allEvents);
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

        private async Task<WrappedIssue> UpdateIssueLabelsAsync(WrappedIssue issue, IList<string> newLabels)
        {
            var internalIssue = issue.Issue;

            var request = new UpdateIssueRequest
            {
                Labels = newLabels
            };

            var newIssue = await GitLabClient.Issues.UpdateAsync(internalIssue.ProjectId, internalIssue.Iid, request).ConfigureAwait(true);

            var allLabels = GetLabels();

            var issueLabels = LabelService.FilterLabels(allLabels, newIssue.Labels);

            var newWrapIssue = issue.Clone();
            newWrapIssue.Labels = issueLabels;

            return newWrapIssue;
        }

        private static DateTime? FinishTime(Issue issue) => issue.ClosedAt;

        private IReadOnlyList<WrappedIssue> ExtentIssues(
            [NotNull] IReadOnlyList<Issue> sourceIssues,
            [NotNull] IReadOnlyDictionary<Issue, IReadOnlyList<Note>> notes,
            [NotNull] IReadOnlyList<Label> labels,
            [NotNull] IReadOnlyDictionary<Issue, IReadOnlyList<LabelEvent>> events)
        {
            var issues = new ObservableCollection<WrappedIssue>();
            foreach (var issue in sourceIssues)
            {
                notes.TryGetValue(issue, out var note);
                events.TryGetValue(issue, out var labelEvents);

                var wrappedIssue = CreateIssue(issue, labels, note, labelEvents);

                if (wrappedIssue != null)
                    issues.Add(wrappedIssue);
            }

            return issues;
        }

        private WrappedIssue CreateIssue(Issue issue, IReadOnlyList<Label> labels, IReadOnlyList<Note> notes, IReadOnlyList<LabelEvent> events)
        {
            DateTime? startDate = null;
            DateTime? passedDate = null;
            DateTime? endDate = null;

            double totalYieldSpend = 0.0;
            var spendDictionary = new Dictionary<DateRange, double>();

            var hasUserEvents = events.Any(x => CurrentUsers.Any(y => y == x.User.UserName));
            var hasNotes = notes != null && notes.Any();

            // if not exist some activity
            if (!hasNotes && !hasUserEvents)
                return null;

            int commits = 0;
            if (hasNotes)
            {
                // if more 0 notes then getting data from notes
                var timeNotes = notes.Where(x => x.Body.ParseSpent() > 0 || x.Body.ParseEstimate() > 0).ToList();

                //if (timeNotes.Count > 0)
                //    startDate = timeNotes.Min(x => x.CreatedAt);
                
                var currentDate = TimeHelper.StartPastDate;

                while (currentDate < TimeHelper.EndDate)
                {
                    var endPeriodDate = currentDate.AddDays(1).AddTicks(-1);

                    var spend = CollectSpendTime(timeNotes, currentDate, endPeriodDate).TotalHours;
                    spendDictionary.Add(new DateRange(currentDate, endPeriodDate), spend);
                    totalYieldSpend += spend;
                    currentDate = currentDate.AddDays(1);
                }

                commits = notes.Count(x => x.Body.IsCommit());
            }

            if (hasUserEvents)
            {
                startDate = LabelService.GetStartTime(events);
                passedDate = LabelService.GetPassedTime(events);
                endDate = issue.ClosedAt;
            }

            // todo ???
            //if (endDate < UpdatedAfter)
            //    return null;

            var issueLabels = LabelService.FilterLabels(labels, issue.Labels);

            var wrappedIssue = new WrappedIssue(issue)
            {
                StartTime = startDate,
                PassTime = passedDate,
                EndTime = endDate,
                Spends = spendDictionary,
                Labels = issueLabels,
                Events = events,
                Status = GetTaskState(issue, issueLabels),
                Commits = commits,
            };
            return wrappedIssue;
        }

        private TaskStatus GetTaskState(Issue issue, IReadOnlyList<Label> issueLabels)
        {
            if (issue.State == IssueState.Closed)
                return TaskStatus.Ready;

            return LabelService.GetTaskState(issueLabels.Select(x => x.Name).ToList());
        }

        private async Task<Dictionary<Issue, IReadOnlyList<Note>>> GetNotesAsync(IReadOnlyList<Issue> issues)
        {
            var dict = new Dictionary<Issue, IReadOnlyList<Note>>();
            foreach (var issue in issues)
            {
                var notes = await GetNotesAsync(Convert.ToInt32(issue.ProjectId), issue.Iid).ConfigureAwait(false);
                dict.Add(issue, notes);
            }

            return dict;
        }

        private async Task<ObservableCollection<Issue>> RequestAllIssuesAsync(Action<IssuesQueryOptions> options)
        {
            var issues = await GitLabClient.Issues.GetAllAsync(projectId: null, groupId: null, options: options).ConfigureAwait(false);

            var allIssues = new ObservableCollection<Issue>(issues);

            return allIssues;
        }

        private static void ActualDataOptions(IssuesQueryOptions options)
        {
            options.Scope = Scope.AssignedToMe;

            options.CreatedAfter = TimeHelper.Today.AddMonths(-2);

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

        private async Task<IReadOnlyList<Note>> GetNotesAsync(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId).ConfigureAwait(false);

            return notes.Where(x => CurrentUsers.Any(y => y == x.Author.Username)).ToList();
        }
        
        public IReadOnlyList<Label> GetLabels()
        {
            return CachedLabels;
        }

        public async Task<IReadOnlyList<GroupLabel>> FetchGroupLabelsAsync(IReadOnlyList<GroupId> groups = null)
        {
            if (groups != null)
                throw new NotImplementedException();

            var allGroups = await GitLabClient.Groups.GetAsync();

            var allLabels = new List<GroupLabel>();
            foreach (var group in allGroups)
            {
                var labels = await GitLabClient.Groups.GetLabelsAsync(group.Id);
                allLabels.AddRange(labels);
            }

            CachedGroupLabels = allLabels;

            return allLabels;
        }

        public IReadOnlyList<GroupLabel> GetGroupLabels()
        {
            return CachedGroupLabels;
        }

        public IReadOnlyList<ProjectId> FetchActiveProjects(IReadOnlyList<Issue> issues)
        {
            var projects = issues.Select(x => x.ProjectId).Distinct().Select(x => (ProjectId)x).ToList();

            CachedProjects ??= projects;

            return projects;
        }

        public IReadOnlyList<ProjectId> GetActiveProjects()
        {
            return CachedProjects.Select(x => x).ToList();
        }

        public async Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync(int projectId, int issueIid)
        {
            var request = new LabelEventsRequest {ProjectId = projectId, IssueIid = issueIid };
            return await HttpService.GetLabelsEventsAsync(request, CancellationToken.None);
        }

        private async Task<Dictionary<Issue, IReadOnlyList<LabelEvent>>> GetAllLabelActionsAsync(IReadOnlyList<Issue> issues)
        {
            var dictionary = new Dictionary<Issue, IReadOnlyList<LabelEvent>>();
            foreach (var issue in issues)
            {
                var events = await GetLabelsEventsAsync(Convert.ToInt32(issue.ProjectId), issue.Iid);

                dictionary.Add(issue, events);
            }

            return dictionary;
        }

        public async Task<IList<User>> GetAllUsersAsync()
        {
            return await GitLabClient.Users.GetAsync();
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
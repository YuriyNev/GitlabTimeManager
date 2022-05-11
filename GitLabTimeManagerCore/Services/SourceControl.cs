using System.Collections.ObjectModel;
using System.Diagnostics;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Internal.Paths;
using GitLabApiClient.Models.Commits.Responses;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Requests;
using GitLabApiClient.Models.Notes.Responses;
using GitLabApiClient.Models.Projects.Requests;
using GitLabApiClient.Models.Projects.Responses;
using GitLabApiClient.Models.Users.Responses;
using GitLabTimeManager.Helpers;

namespace GitLabTimeManager.Services
{
    public interface ISourceControl
    {
        IReadOnlyList<string> CurrentUsers { get; }

        Task<GitResponse> RequestDataAsync(DateTime start, DateTime end, IReadOnlyList<string> users, IReadOnlyList<string>? labels, Action<string>? requestStatusAction = null);
        Task AddSpendAsync(Issue issue, TimeSpan timeSpan);
        Task SetEstimateAsync(Issue issue, TimeSpan timeSpan);
        Task<WrappedIssue> StartIssueAsync(WrappedIssue issue);
        Task<WrappedIssue> PauseIssueAsync(WrappedIssue issue);
        Task<WrappedIssue> FinishIssueAsync(WrappedIssue issue);
        Task<Issue> UpdateIssueAsync(Issue issue, UpdateIssueRequest request);
        
        Task<IReadOnlyList<Label>> FetchLabelsAsync(IReadOnlyList<ProjectId>? projects = null);
        IReadOnlyList<Label> GetLabels();
        
        Task<IReadOnlyList<GroupLabel>> FetchGroupLabelsAsync(IReadOnlyList<GroupId>? groups = null);
        IReadOnlyList<GroupLabel> GetGroupLabels();

        IReadOnlyList<ProjectId> FetchActiveProjects(IReadOnlyList<Issue> issues);
        IReadOnlyList<ProjectId> GetActiveProjects();
        
        Task<IReadOnlyList<Project>> GetAllProjectsAsync();

        Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync(int projectId, int issueIid);

        Task<IList<User>> FetchAllUsersAsync();
    }

    public class SourceControl : ISourceControl
    {
        private IUserProfile UserProfile { get; }
        private ILabelService LabelService { get; }
        private IHttpService HttpService { get; }
        private GitLabClient GitLabClient { get; }

        private IReadOnlyList<Label> CachedLabels { get; set; }
        private IReadOnlyList<GroupLabel> CachedGroupLabels { get; set; }
        private IReadOnlyList<ProjectId> CachedProjects { get; set; }
        private IReadOnlyList<User> CachedUsers { get; set; }

        public IReadOnlyList<string> CurrentLabels { get; private set; }
        public IReadOnlyList<string> CurrentUsers { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public IReadOnlyList<string> Labels { get; private set; }

        private bool IsSingleUser => CurrentUsers.Count == 1;

        public SourceControl(
            IUserProfile userProfile,
            ILabelService labelService,
            IHttpService httpService)
        {
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
            HttpService = httpService ?? throw new ArgumentNullException(nameof(httpService));

            GitLabClient = new GitLabClient(UserProfile.Url, UserProfile.Token);
        }
        
        public async Task<GitResponse> RequestDataAsync(DateTime start, DateTime end, IReadOnlyList<string> users, IReadOnlyList<string>? labels, Action<string> requestStatusAction)
        {
            var wrappedIssues = await GetRawDataAsync(start, end, users, labels, requestStatusAction).ConfigureAwait(false);

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

        private async Task<IReadOnlyList<Label>> GetLabelsCoreAsync(IReadOnlyList<ProjectId>? projects)
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

        private async Task<IReadOnlyList<WrappedIssue>?> GetRawDataAsync(DateTime start, DateTime end, IReadOnlyList<string> userNames, IReadOnlyList<string>? labels, Action<string>? requestStatusAction = null)
        {
            if (_isAction)
                return null;

            try
            {
                _isAction = true;

                StartTime = start;
                EndTime = end;
                Labels = labels ?? Array.Empty<string>();
                CurrentUsers = userNames;
                var allIssues = new List<Issue>();
                
                foreach (var user in userNames)
                {
                    void AllDataOptions(IssuesQueryOptions options)
                    {
                        options.AssigneeUsername = new List<string> { user };

                        options.UpdatedAfter = StartTime;
                        options.CreatedBefore = EndTime;
                        options.State = IssueState.All;
                        options.Labels = Labels.ToList();
                    }
                    requestStatusAction?.Invoke($"Получение задач для пользователя {user}");

                    var issuesByUser = await RequestAllIssuesAsync(AllDataOptions).ConfigureAwait(false);
                    allIssues.AddRange(issuesByUser);
                }

                allIssues = allIssues
                    .DistinctBy(x => x.Iid)
                    .ToList();
                
                IReadOnlyList<User> users = await GetUsersAsync(userNames).ConfigureAwait(false);
                
                requestStatusAction?.Invoke($"Получение проектов");
                var projects = FetchActiveProjects(allIssues);

                requestStatusAction?.Invoke($"Получение меток");
                var allLabels = await FetchLabelsAsync(projects).ConfigureAwait(false);

                requestStatusAction?.Invoke($"Получение деталей задачи");
                var allNotes = true
                    ? await GetNotesAsync(allIssues).ConfigureAwait(false)
                    : new Dictionary<Issue, IReadOnlyList<Note>>();

                requestStatusAction?.Invoke($"Получение изменений коммитов");
                var allCommits = await GetCommitsAsync(allNotes).ConfigureAwait(false);

                requestStatusAction?.Invoke($"Получение событий меток");
                var allEvents = await GetAllLabelActionsAsync(allIssues).ConfigureAwait(false);

                return ExtentIssues(allIssues, allNotes, allLabels, allEvents, allCommits, users);
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

        private async Task<IReadOnlyList<User>> GetUsersAsync(IEnumerable<string> userNames)
        {
            var list = new List<User>();
            foreach (var userName in userNames)
            {
                var user = await GitLabClient.Users.GetAsync(userName);
                list.Add(user);
            }

            return list;
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

        private IReadOnlyList<WrappedIssue> ExtentIssues(IReadOnlyList<Issue> sourceIssues,
            IReadOnlyDictionary<Issue, IReadOnlyList<Note>> notes,
            IReadOnlyList<Label> labels,
            IReadOnlyDictionary<Issue, IReadOnlyList<LabelEvent>> events, Dictionary<Issue, List<Commit>> allCommits,
            IReadOnlyList<User> users)
        {
            var issues = new ObservableCollection<WrappedIssue>();
            foreach (var issue in sourceIssues)
            {
                notes.TryGetValue(issue, out var note);
                events.TryGetValue(issue, out var labelEvents);
                allCommits.TryGetValue(issue, out var commits);

                var wrappedIssue = CreateIssue(issue, labels, note, labelEvents, commits, users);

                if (wrappedIssue != null)
                    issues.Add(wrappedIssue);
            }

            return issues;
        }

        private WrappedIssue CreateIssue(Issue issue, IReadOnlyList<Label> labels, IReadOnlyList<Note> notes, IReadOnlyList<LabelEvent> events,
            IReadOnlyList<Commit> commits, IReadOnlyList<User> users)
        {
            DateTime? startDate = null;
            DateTime? passedDate = null;
            DateTime? endDate = null;

            double totalYieldSpend = 0.0;
            var spendDictionary = new Dictionary<DateRange, double>();

            var hasUserEvents = events.Any();
            var hasNotes = notes != null && notes.Any();

            // if not exist some activity
            if (!hasNotes && !hasUserEvents)
                return null;

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
                Comments = notes.Where(x => !x.System).ToList(),
                Commits = commits
                    .Where(x =>
                    {
                        var userName = users.FirstOrDefault(user => user.Email == x.CommitterEmail)?.Username;
                        return userName != null;
                    })
                    .Select(x =>
                    {
                        return new CommitInfo
                        {
                            Time = x.CreatedAt,
                            Author = users.First(user => user.Email == x.CommitterEmail).Username,
                            CommitId = x.ShortId,
                            Changes = new CommitChanges
                            {
                                Additions = x.CommitStats.Additions,
                                Deletions = x.CommitStats.Deletions
                            }
                        };
                    })
                    .ToList(),
            };
            return wrappedIssue;
        }
        
        private TaskStatus GetTaskState(Issue issue, IReadOnlyList<Label> issueLabels)
        {
            if (issue.State == IssueState.Closed)
                return TaskFactory.DoneStatus;

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

        private async Task<IReadOnlyList<Issue>> RequestAllIssuesAsync(Action<IssuesQueryOptions> options)
        {
            var issues = await GitLabClient.Issues.GetAllAsync(projectId: null, groupId: null, options: options).ConfigureAwait(false);

            var allIssues = new ObservableCollection<Issue>(issues);

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

        private async Task<IReadOnlyList<Note>> GetNotesAsync(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId).ConfigureAwait(false);

            return notes.Where(x => CurrentUsers.Any(y => y == x.Author.Username)).ToList();
        }

        private async Task<Commit> GetCommitAsync(string projectId, string sha)
        {
            try
            {
                return await GitLabClient.Commits.GetAsync(projectId, sha).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<Dictionary<Issue, List<Commit>>> GetCommitsAsync(Dictionary<Issue, IReadOnlyList<Note>> allNotes)
        {
            var result = new Dictionary<Issue, List<Commit>>();

            foreach (var pair in allNotes)
            {
                var issue = pair.Key;
             
                if (!result.TryGetValue(issue, out _))
                    result.Add(issue, new List<Commit>());

                foreach (var note in pair.Value)
                {
                    if (!note.Body.IsCommit())
                    {
                        continue;
                    }

                    var commitID = note.Body.ParseCommitId();

                    var commitSha = commitID.sha;
                    var projectId = string.IsNullOrEmpty(commitID.path) 
                        ? issue.ProjectId 
                        : await FindProjectByPath(commitID.path).ConfigureAwait(false);

                    var commit = await GetCommitAsync(projectId, commitSha).ConfigureAwait(false);

                    result[issue].Add(commit);
                }
            }

            var expect = allNotes.Keys.Except(result.Keys).ToList();

            if (expect.Any())
            {
                throw new Exception("Not full commits dictionary!");
            }
            return result;
        }

        private async Task<string> FindProjectByPath(string path)
        {
            var allProjects = await GetAllProjectsAsync().ConfigureAwait(false);
            foreach (var project in allProjects)
            {
                if (project.Path == path)
                    return project.Id.ToString();
            }

            throw new InvalidOperationException("Unknown project!");
        }

        public IReadOnlyList<Label> GetLabels()
        {
            return CachedLabels;
        }

        public async Task<IReadOnlyList<GroupLabel>> FetchGroupLabelsAsync(IReadOnlyList<GroupId>? groups = null)
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
            if (CachedProjects == null)
            {
                var projects = issues
                    .Select(x => x.ProjectId)
                    .Distinct()
                    .Select(x => (ProjectId)x)
                    .ToList();
                CachedProjects = projects;
            }

            return CachedProjects;
        }

        public IReadOnlyList<ProjectId> GetActiveProjects()
        {
            return CachedProjects.Select(x => x).ToList();
        }

        public async Task<IReadOnlyList<Project>> GetAllProjectsAsync()
        {
            var projects = await GitLabClient.Projects.GetAsync(options => {}).ConfigureAwait(false);
            return projects.ToList();
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

        public async Task<IList<User>> FetchAllUsersAsync()
        {
            if (CachedUsers == null)
            {
                var allUsers = await GitLabClient.Users.GetAsync();
                CachedUsers = allUsers
                    .Where(x => x.State != "blocked")
                    .ToList();
            }
            return CachedUsers.ToList();
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
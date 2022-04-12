using System.Diagnostics;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Responses;

namespace GitLabTimeManager.Services
{
    public interface ILabelService
    {
        IReadOnlyList<string> StartIssue(IReadOnlyList<string> labels);

        IReadOnlyList<string> FinishIssue(IReadOnlyList<string> labels);

        IReadOnlyList<string> PauseIssue(IReadOnlyList<string> labels);

        bool InWork(List<string> labels);

        bool IsPaused(List<string> labels);

        bool IsPassed(List<string> labels);

        bool IsReadyForWork(List<string> labels);
        
        TaskStatus GetTaskState(List<string> labels);

        IReadOnlyList<Label> FilterLabels(IReadOnlyList<Label> all, IList<string> source);

        bool ContainsExcludeLabels(IReadOnlyList<Label> labels);

        bool ContainsBoardLabels(IReadOnlyList<Label> labels);

        DateTime? GetStartTime(IReadOnlyList<LabelEvent> labelEvents);

        DateTime? GetPassedTime(IReadOnlyList<LabelEvent> labelEvents);
    }

    public class LabelProcessor : ILabelService, IDisposable
    {
        private IUserProfile UserProfile { get; }
        private IProfileService ProfileService { get; }

        private string? ToDoLabel { get; set; }
        private string? DoingLabel { get; set; }
        private string? DoneLabel { get; set; }

        private IReadOnlyList<string> AllBoardLabels { get; set; }
        private IReadOnlyList<string> ExcludeLabels { get; set; }
        private IReadOnlyList<string> PassedLabels { get; set; }
        private BoardStateLabels BoardStateLabels { get; set; }

        public LabelProcessor(
            IUserProfile userProfile,
            IProfileService profileService)
        {
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            ProfileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

            Update(UserProfile.LabelSettings);

            ProfileService.Serialized += ProfileService_Serialized;
        }

        private void ProfileService_Serialized(object? sender, IUserProfile e)
        {
            Update(UserProfile.LabelSettings);
        }

        private void Update(LabelSettings settings)
        {
            AllBoardLabels = settings.AllBoardLabels;
            PassedLabels = settings.PassedLabels;
            ExcludeLabels = settings.ExcludeLabels;
            BoardStateLabels = settings.BoardStateLabels;

            ToDoLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.ToDoLabel);
            DoingLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.DoingLabel);
            DoneLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.DoneLabel);

            ExcludeLabels = settings.ExcludeLabels;
        }

        public IReadOnlyList<string> StartIssue(IReadOnlyList<string> labels)
        {
            return SwitchBoardLabel(labels, DoingLabel);
        }

        public IReadOnlyList<string> FinishIssue(IReadOnlyList<string> labels)
        {
            return SwitchBoardLabel(labels, DoneLabel);
        }

        public IReadOnlyList<string> PauseIssue(IReadOnlyList<string> labels)
        {
            return SwitchBoardLabel(labels, ToDoLabel);
        }

        private IReadOnlyList<string> SwitchBoardLabel(IReadOnlyList<string> labels, string selectedLabel)
        {
            var newLabels = new List<string>(labels);

            foreach (var label in AllBoardLabels)
            {
                if (label != selectedLabel)
                    newLabels.Remove(label);
            }

            newLabels.Add(selectedLabel);

            return newLabels;
        }

        public TaskStatus GetTaskState(List<string> labels)
        {
            var isPassed = IsPassed(labels);
            var inWork = InWork(labels);
            var waitWork = IsReadyForWork(labels);

            var checkCount = 0;
            if (isPassed)
                checkCount++;

            if (inWork)
                checkCount++;

            if (waitWork)
                checkCount++;

            if (checkCount > 1)
                Debug.Assert(false, "Incorrect state!");

            if (isPassed)
                return TaskFactory.DoneStatus;

            if (inWork)
                return TaskFactory.DoingStatus;

            if (waitWork)
                return TaskFactory.ToDo;

            return null;
        }

        public bool InWork(List<string> labels) => labels.Contains(DoingLabel);
        
        public bool IsPaused(List<string> labels) => !labels.Contains(DoingLabel);

        public bool IsPassed(List<string> labels) => labels.Intersect(PassedLabels).Any();

        public bool IsReadyForWork(List<string> labels) => labels.Contains(ToDoLabel);

        public IReadOnlyList<Label> FilterLabels(IReadOnlyList<Label> all, IList<string> source) => all.Where(x => source.Contains(x.Name)).ToList();

        public bool ContainsExcludeLabels(IReadOnlyList<Label> labels)
        {
            return labels.Any(l => ExcludeLabels.Contains(l.Name));
        }

        public bool ContainsBoardLabels(IReadOnlyList<Label> labels)
        {
            return labels.Any(l => AllBoardLabels.Contains(l.Name));
        }

        public DateTime? GetStartTime(IReadOnlyList<LabelEvent> labelEvents)
        {
            if (labelEvents == null) throw new ArgumentNullException(nameof(labelEvents));

            var date = labelEvents
                .Where(x => x.Label != null)
                .Where(x => x.Label.Name == DoingLabel || x.Label.Name == DoneLabel)
                .Where(x => x.Action == EventAction.Add)
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.CreatedAt)
                .FirstOrDefault();

            if (date == default)
                return null;

            return date;
        }

        public DateTime? GetPassedTime(IReadOnlyList<LabelEvent> labelEvents)
        {
            if (labelEvents == null) throw new ArgumentNullException(nameof(labelEvents));

            var date = labelEvents
                .Where(x => x.Label != null)
                .Where(x => PassedLabels.Contains(x.Label.Name))
                .Where(x => x.Action == EventAction.Add)
                .OrderBy(x => x.CreatedAt)
                .Select(x => x.CreatedAt)
                .FirstOrDefault();

            if (date == default)
                return null;

            return date;
        }

        public void Dispose()
        {
            ProfileService.Serialized -= ProfileService_Serialized;
        }
    }

    public static class LabelStageCalculator
    {
        public static LabelStageMetric GetMetric(this WrappedIssue issue, ILabelService labelService, DateTime start, DateTime end)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            if (labelService == null) throw new ArgumentNullException(nameof(labelService));

            var events = issue.Events
                .Where(ev => ev.Label != null)
                .Where(ev => labelService.InWork(new List<string> {ev.Label.Name}))
                //.Where(ev => IsUserAdded(ev, issue.Issue.Assignee.Username) || ev.Action == EventAction.Remove)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            var count = events.Count;
            var stageTime = TimeSpan.Zero;

            for (int i = 0; i < count; i++)
            {
                var startEvent = events[i];
                var stageStart = startEvent.CreatedAt;
                var nextIndex = i + 1;

                var stageEnd = nextIndex < count 
                    ? events[nextIndex].CreatedAt 
                    : end;

                if (startEvent.Action != EventAction.Add)
                    continue;
                
                if (stageStart < start && stageEnd < start)
                   continue;
                
                if (stageStart > end && stageEnd > end)
                   continue;
                
                if (stageStart < start && stageEnd > start)
                   stageStart = start;
                
                if (stageEnd >= end && stageStart < end)
                   stageEnd = end;
                
                if (stageEnd < stageStart)
                    continue;

                stageTime += StatisticsExtractor.GetAnyDaysSpend(stageStart, stageEnd);
            }

            var iterations = events
                .Where(x => x.CreatedAt >= start && x.CreatedAt <= end)
                .Where(x => x.Action == EventAction.Add)
                .Count(x => labelService.InWork(new List<string> { x.Label.Name }));
            
            return new LabelStageMetric
            {
                Iterations = iterations,
                Duration = stageTime,
            };
        }

        private static bool IsUserAdded(LabelEvent ev, string userName) => ev.Action == EventAction.Add && ev.User.UserName == userName;
    }

    public class LabelStageMetric
    {
        public int Iterations { get; set; }

        public TimeSpan Duration { get; set; }
    }

    public static class LabelHelper
    {
        public static Label ConvertToLabel(this GroupLabel groupLabel) =>
            new()
            {
                Id = groupLabel.Id,
                Name = groupLabel.Name,
                Color = groupLabel.Color,
                Description = groupLabel.Description,
            };
    }
}
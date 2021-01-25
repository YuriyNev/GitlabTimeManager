using System;
using System.Collections.Generic;
using System.Linq;
using GitLabApiClient.Models.Projects.Responses;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public interface ILabelService
    {
        IReadOnlyList<string> StartIssue(IReadOnlyList<string> labels);

        IReadOnlyList<string> FinishIssue(IReadOnlyList<string> labels);

        IReadOnlyList<string> PauseIssue(IReadOnlyList<string> labels);

        bool IsStarted(List<string> labels);

        bool IsPaused(List<string> labels);

        IReadOnlyList<Label> CreateIssueLabels(IReadOnlyList<Label> all, IList<string> source);

        bool ContainsExcludeLabels(IReadOnlyList<Label> labels);

        bool ContainsBoardLabels(IReadOnlyList<Label> labels);
    }

    public class LabelProcessor : ILabelService
    {
        [NotNull] private IUserProfile UserProfile { get; }

        private string ToDoLabel { get; } 
        private string DoingLabel { get; }
        private string DoneLabel { get; }

        private IReadOnlyList<string> AllBoardLabels { get; }
        private IReadOnlyList<string> ExcludeLabels { get; }
        private BoardStateLabels BoardStateLabels { get; }

        public LabelProcessor([NotNull] IUserProfile userProfile)
        {
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));

            var settings = UserProfile.LabelSettings;
            AllBoardLabels = settings.AllBoardLabels;
            ExcludeLabels = settings.ExcludeLabels;
            BoardStateLabels = settings.BoardStateLabels;
            
            ToDoLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.ToDoLabel);
            DoingLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.DoingLabel);
            DoneLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.Done);

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

       
        public bool IsStarted(List<string> labels) => labels.Contains(DoingLabel);

        public bool IsPaused(List<string> labels) => !labels.Contains(DoingLabel);

        public IReadOnlyList<Label> CreateIssueLabels(IReadOnlyList<Label> all, IList<string> source) => all.Where(x => source.Contains(x.Name)).ToList();

        public bool ContainsExcludeLabels(IReadOnlyList<Label> labels)
        {
            return labels.Any(l => ExcludeLabels.Contains(l.Name));
        }

        public bool ContainsBoardLabels(IReadOnlyList<Label> labels)
        {
            return labels.Any(l => AllBoardLabels.Contains(l.Name));
        }
    }
}
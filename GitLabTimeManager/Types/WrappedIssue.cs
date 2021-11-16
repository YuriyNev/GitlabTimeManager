using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Projects.Responses;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Tools;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    [DebuggerDisplay("{Issue.Title} {StartTime} - {EndTime} {Estimate}")]
    public class WrappedIssue : NotifyObject
    {
        private Issue _issue;
        private IReadOnlyList<Label> _labels;
        private IReadOnlyList<LabelEvent> _events;

        public WrappedIssue([NotNull] Issue issue)
        {
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));
        }

        public Issue Issue
        {
            get => _issue;
            set
            {
                if (Equals(value, _issue)) return;
                _issue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Spend));
                OnPropertyChanged(nameof(Estimate));
            }
        }

        public DateTime? StartTime { get; set; }

        public DateTime? PassTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime? DueTime
        {
            get
            {

                if (DateTime.TryParse(Issue.DueDate, out var date))
                {
                    return date;
                }

                if (Issue.DueDate != null) 
                    Debug.Assert(false, "DueTime not null!");

                return null;
            }
        }

        public TaskStatus Status { get; set; }

        public double Spend => TimeHelper.SecondsToHours(Issue.TimeStats.TotalTimeSpent);

        public Dictionary<DateRange, double> Spends { get; set; }

        public double Estimate => TimeHelper.SecondsToHours(Issue.TimeStats.TimeEstimate);

        public IReadOnlyList<Label> Labels
        {
            get => _labels;
            set
            {
                if (Equals(value, _labels)) return;
                _labels = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<LabelEvent> Events
        {
            get => _events;
            set
            {
                if (Equals(value, _events)) return;
                _events = value;
                OnPropertyChanged();
            }
        }

        public int Commits { get; set; }

        public override string ToString() => $"{Issue.Iid}\t{Issue.Title}\t{StartTime}\t{EndTime}\t{Estimate:F1}\t";

        public override bool Equals(object obj)
        {
            if (!(obj is WrappedIssue issue))
                return false;

            var otherIssue = issue.Issue;
            return Issue.Iid == otherIssue.Iid &&
                   Issue.Title == otherIssue.Title &&
                   Issue.ClosedAt == otherIssue.ClosedAt &&
                   Issue.Labels == otherIssue.Labels && 
                   Issue.State == otherIssue.State;
        }
        
        public WrappedIssue Clone()
        {
            return new(Issue)
            {
                StartTime = StartTime,
                EndTime = EndTime,
                Labels = new List<Label>(Labels),
                Spends = new Dictionary<DateRange, double>(Spends),
            };
        }
    }

    //public enum TaskStatus
    //{
    //    None,
    //    ToDo,
    //    Doing,
    //    Ready,
    //}

    public class ReportIssue : NotifyObject
    {
        public int Iid { get; set; }

        public string Title { get; set; }

        public double SpendForPeriod { get; set; }

        public double SpendForPeriodByStage { get; set; }

        public double Estimate { get; set; }

        public double Activity { get; set; }
        
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public DateTime? DueTime { get; set; }

        public int Iterations { get; set; }

        public int Commits { get; set; }

        public string User { get; set; }
        
        public string Epic { get; set; }

        public string WebUri { get; set; }

        public TaskStatus TaskState { get; set; }
    }

    public class UserInfo
    {
        public string Name { get; }

        public UserInfo([NotNull] string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }

    public abstract class TaskStatus : IComparable
    {
        public string Name { get; }

        public Brush Brush { get; }

        public int Index { get; }

        protected TaskStatus([NotNull] string name, [NotNull] Brush brush, int index)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Brush = brush ?? throw new ArgumentNullException(nameof(brush));
            Brush.Freeze();

            Index = index;
        }

        public int CompareTo(object obj)
        {
            if (obj is TaskStatus otherTask)
            {
                if (Index == otherTask.Index) return 0;
                if (Index > otherTask.Index) return 1;
                if (Index < otherTask.Index) return -1;
            }

            return 0;
        }
    }

    public static class TaskFactory
    {
        public static TaskStatus ToDo => new ToDoStatus("Можно выполнять", new SolidColorBrush(new Color { A = 0xFF, R = 0x42, G = 0x8B, B = 0xCA, }), 0);
        public static TaskStatus DoingStatus => new DoingStatus("В работе", new SolidColorBrush(new Color { A = 0xFF, R = 0x00, G = 0x33, B = 0xCC, }), 1);
        public static TaskStatus DoneStatus => new DoneStatus("Сделано",   new SolidColorBrush(new Color { A = 0xFF, R = 0x44, G = 0xAD, B = 0x8E, }), 2);
    }

    public class ToDoStatus : TaskStatus
    {
        public ToDoStatus([NotNull] string name, [NotNull] Brush brush, int index) : base(name, brush, index)
        {
        }
    }

    public class DoingStatus : TaskStatus
    {
        public DoingStatus([NotNull] string name, [NotNull] Brush brush, int index) : base(name, brush, index)
        {
        }
    }

    public class DoneStatus : TaskStatus
    {
        public DoneStatus([NotNull] string name, [NotNull] Brush brush, int index) : base(name, brush, index)
        {
        }
    }
}
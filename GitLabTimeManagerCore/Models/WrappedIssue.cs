using System.Diagnostics;
using System.Windows.Media;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Projects.Responses;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    [DebuggerDisplay("{Issue.Title} {StartTime} - {EndTime} {Estimate}")]
    public class WrappedIssue : NotifyObject
    {
        private Issue _issue;
        private IReadOnlyList<Label> _labels;
        private IReadOnlyList<LabelEvent> _events;

        public WrappedIssue(Issue issue)
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

        public IReadOnlyList<DateTime> Commits { get; set; }

        public override string ToString() => $"{Issue.Iid}\t{Issue.Title}\t{StartTime}\t{EndTime}\t{Estimate:F1}\t";

        public override bool Equals(object? obj)
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
        
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_issue);
            hashCode.Add(_labels);
            hashCode.Add(_events);
            hashCode.Add(StartTime);
            hashCode.Add(PassTime);
            hashCode.Add(EndTime);
            hashCode.Add(Status);
            hashCode.Add(Spends);
            hashCode.Add(Commits);
            return hashCode.ToHashCode();
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

    public class ReportIssue : NotifyObject
    {
        public int Iid { get; init; }

        public string Title { get; init; }

        public double SpendForPeriod { get; init; }

        public double SpendForPeriodByStage { get; init; }

        public double Estimate { get; init; }

        public double Activity { get; init; }
        
        public DateTime? StartTime { get; init; }

        public DateTime? EndTime { get; init; }

        public DateTime? DueTime { get; init; }

        public int Iterations { get; init; }

        public int Commits { get; init; }

        public string User { get; init; }
        
        public string Epic { get; init; }

        public string WebUri { get; init; }

        public TaskStatus TaskState { get; init; }
    }
    
    public abstract class TaskStatus : IComparable
    {
        public string Name { get; }

        public Brush Brush { get; }

        private int Index { get; }

        protected TaskStatus(string name, Brush brush, int index)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Brush = brush ?? throw new ArgumentNullException(nameof(brush));
            Brush.Freeze();

            Index = index;
        }

        public int CompareTo(object? obj)
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
        public static TaskStatus DoneStatus => new DoneStatus("Сделано",  new SolidColorBrush(new Color { A = 0xFF, R = 0x44, G = 0xAD, B = 0x8E, }), 2);
    }

    public class ToDoStatus : TaskStatus
    {
        public ToDoStatus(string name, Brush brush, int index) : base(name, brush, index)
        {
        }
    }

    public class DoingStatus : TaskStatus
    {
        public DoingStatus(string name, Brush brush, int index) : base(name, brush, index)
        {
        }
    }

    public class DoneStatus : TaskStatus
    {
        public DoneStatus(string name, Brush brush, int index) : base(name, brush, index)
        {
        }
    }
}
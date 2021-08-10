using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public WrappedIssue([NotNull] Issue issue, [NotNull] IReadOnlyList<Label> labels)
        {
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));
            Labels = labels ?? throw new ArgumentNullException(nameof(labels));
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

        public DateTime? EndTime { get; set; }

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

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public WrappedIssue Clone()
        {
            return new WrappedIssue(Issue)
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
        public int Iid { get; set; }

        public string Title { get; set; }

        public double SpendForPeriod { get; set; }
        
        public double Estimate { get; set; }
        
        public DateTime? StartTime { get; set; }
    }
}
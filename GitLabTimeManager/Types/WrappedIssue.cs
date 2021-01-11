using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    [DebuggerDisplay("{Issue.Title} {StartTime} - {EndTime} {Estimate}")]
    public class WrappedIssue : NotifyObject
    {
        public Issue Issue { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public double Spend => TimeHelper.SecondsToHours(Issue.TimeStats.TotalTimeSpent);

        public Dictionary<DateRange, double> Spends { get; set; }

        public double Estimate => TimeHelper.SecondsToHours(Issue.TimeStats.TimeEstimate);

        public ObservableCollection<LabelEx> LabelExes { get; set; }

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
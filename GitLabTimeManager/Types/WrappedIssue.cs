using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    [DebuggerDisplay("{Issue.Title} {StartTime} - {EndTime} {StartedIn} {SpendIn} {SpendBefore}")]
    public class WrappedIssue : NotifyObject
    {
        public Issue Issue { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public double SpendIn { get; set; }

        public double SpendBefore { get; set; }

        public bool StartedIn { get; set; }

        public double Spend => TimeHelper.SecondsToHours(Issue.TimeStats.TotalTimeSpent);

        public double Estimate => TimeHelper.SecondsToHours(Issue.TimeStats.TimeEstimate);

        public ObservableCollection<LabelEx> LabelExes { get; set; }

        public override string ToString() => $"{Issue.Iid}\t{Issue.Title}\t{StartTime}\t{EndTime}\t{StartedIn}\t{SpendIn:F1}\t{SpendBefore:F1}";
    }
    
}
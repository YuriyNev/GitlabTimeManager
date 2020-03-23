using System;
using System.Diagnostics;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    [DebuggerDisplay("{Issue.Title} {Started} - {Finished} {StartedIn} {SpendIn} {SpendBefore}")]
    public class WrappedIssue : NotifyObject
    {
        public Issue Issue { get; set; }

        public DateTime? Started { get; set; }

        public DateTime? Finished { get; set; }

        public double SpendIn { get; set; }

        public double SpendBefore { get; set; }

        public bool StartedIn { get; set; }

        public double Spend => TimeHelper.SecondsToHours(Issue.TimeStats.TotalTimeSpent);
        public double Estimate => TimeHelper.SecondsToHours(Issue.TimeStats.TimeEstimate);

        private bool _inProgress;
        public bool InProgress
        {
            get => _inProgress;
            set
            {
                if (value == _inProgress) return;
                _inProgress = value;
                OnPropertyChanged();
            }
        }

        public override string ToString() => $"{Issue.Iid}\t{Started}\t{Finished}\t{StartedIn}\t{SpendIn:F1}\t{SpendBefore:F1}";
    }
}
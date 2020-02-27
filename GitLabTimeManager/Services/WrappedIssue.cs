using System;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    public class WrappedIssue : NotifyObject
    {
        public Issue Issue { get; set; }

        public DateTime? Started { get; set; }

        public DateTime? Finished { get; set; }

        public double SpendIn { get; set; }

        public double SpendBefore { get; set; }
        public bool StartedIn { get; set; }
    }
}
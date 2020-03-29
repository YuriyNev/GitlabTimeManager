using System;

namespace GitLabTimeManager.ViewModel
{
    public class DetailedIssueTime
    {
        public TimeSpan Spent { get; set; }
        public TimeSpan Estimate { get; set; }
    }
}
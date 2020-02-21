using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabApiClient;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Services
{
    public interface ISourceControl
    {
        Task<GitResponse> RequestData();
    }

    public class GitResponse
    {
        public int ClosedSpendInPeriod { get; set; }

        public int OpenSpendInPeriod { get; set; }

        /// <summary> Фактическое время за месяц </summary>
        public int TotalSpendInPeriod { get; set; }

        public int OpenTotalEstimate { get; set; }

        public int OpenTotalSpent { get; set; }

        public int ClosedTotalSpent { get; set; }

        public int ClosedTotalEstimate { get; set; }

        public int TotalEstimate { get; set; }

        public int TotalSpent { get; set; }

        public int ClosedIssuesCount { get; set; }

        public int OpenIssuesCount { get; set; }

        public DateTime SpendPerMonth { get; set; }

        public DateTime EstimatePerMonth { get; set; }
    }
}
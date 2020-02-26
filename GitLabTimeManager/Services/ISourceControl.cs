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
        /// <summary> Most need properties  </summary>
        public double OpenEstimatesStartedInPeriod { get; set; }
        public double ClosedEstimatesStartedInPeriod { get; set; }
        public double ClosedSpendsStartedInPeriod { get; set; }
        public double OpenSpendsStartedInPeriod { get; set; }
        public double OpenEstimatesStartedBefore { get; set; }
        public double ClosedEstimatesStartedBefore { get; set; }
        public double OpenSpendsStartedBefore { get; set; }
        public double ClosedSpendsStartedBefore { get; set; }

        public double ClosedSpendInPeriod { get; set; }
        public double OpenSpendInPeriod { get; set; }
        public double OpenSpendBefore { get; set; }
        public double ClosedSpendBefore { get; set; }

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
using System;
using System.Collections.Generic;
using System.Linq;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Helpers;

namespace GitLabTimeManager.Services
{
    public static class StatisticsExtractor
    {
        public static GitStatistics Process(IReadOnlyList<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endTime)
        {
            var statistics = new GitStatistics();
            // Most need issues
            // started in month
            var issues = wrappedIssues;
            var openIssues = issues.Where(x => IsOpen(x.Issue)).ToList();
            var closedIssues = issues.Where(x => IsClosed(x.Issue)).ToList();

            statistics.OpenEstimatesStartedInPeriod = openIssues
                .Where(issue => StartedIn(issue, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.ClosedEstimatesStartedInPeriod = closedIssues
                .Where(issue => StartedIn(issue, startDate, endTime) &&
                                FinishedInPeriod(issue.Issue, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.AllEstimatesStartedInPeriod = statistics.OpenEstimatesStartedInPeriod + statistics.ClosedEstimatesStartedInPeriod;

            statistics.OpenSpendsStartedInPeriod = openIssues
                .Where(issue => StartedIn(issue, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            statistics.ClosedSpendsStartedInPeriod = closedIssues
                .Where(issue => StartedIn(issue, startDate, endTime) &&
                                FinishedInPeriod(issue.Issue, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            // started before this month
            statistics.OpenEstimatesStartedBefore = openIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.ClosedEstimatesStartedBefore = closedIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.AllEstimatesStartedBefore = statistics.OpenEstimatesStartedBefore + statistics.ClosedEstimatesStartedBefore;

            statistics.AllTodayEstimates = issues
                .Where(issue => StartedIn(issue, DateTime.Today.AddDays(-6), DateTime.Today.AddDays(1)))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));
            
            statistics.OpenSpendsStartedBefore = openIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            statistics.ClosedSpendsStartedBefore = closedIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            // Потраченное время только в этом месяце
            // На задачи начатые в этом месяце
            var withoutExcludes = issues.
                Where(x => !x.LabelExes.IsExcludeLabels()).ToList();

            statistics.OpenSpendBefore = withoutExcludes.
                Where(x => IsOpenAtMoment(x.Issue, startDate, endTime)).
                Where(x => !StartedIn(x, startDate, endTime)).
                Sum(x => SpendsSum(x, startDate, endTime));

            statistics.ClosedSpendBefore = withoutExcludes.
                Where(x => IsCloseAtMoment(x.Issue, startDate, endTime)).
                Where(x => !StartedIn(x, startDate, endTime)).
                Sum(x => SpendsSum(x, startDate, endTime));

            statistics.OpenSpendInPeriod = withoutExcludes.
                Where(x => IsOpenAtMoment(x.Issue, startDate, endTime)).
                Where(x => StartedIn(x, startDate, endTime)).
                Sum(x => SpendsSum(x, startDate, endTime));
            
            statistics.ClosedSpendInPeriod = withoutExcludes.
                Where(x => IsCloseAtMoment(x.Issue, startDate, endTime)).
                Where(x => StartedIn(x, startDate, endTime)).
                Sum(x => SpendsSum(x, startDate, endTime));

            statistics.AllSpendsStartedInPeriod = statistics.OpenSpendInPeriod + statistics.ClosedSpendInPeriod;
            statistics.AllSpendsStartedBefore = statistics.OpenSpendBefore + statistics.ClosedSpendBefore;
            statistics.AllSpendsForPeriod = statistics.AllSpendsStartedInPeriod + statistics.AllSpendsStartedBefore;

            return statistics;
        }

        public static double SpendsSum(WrappedIssue issue, DateTime startDate, DateTime endDate)
        {
            return issue.Spends.Keys.
                Where(x => x.StartDate >= startDate && x.EndDate <= endDate).
                Sum(key => issue.Spends[key]);
        }
        
        private static bool StartedIn(WrappedIssue issue, DateTime startTime, DateTime endTime)
        {
            var spends = issue.Spends;
            // if no spends
            if (!spends.Any())
                return issue.Issue.CreatedAt > startTime && issue.Issue.CreatedAt < endTime &&
                       issue.Issue.TimeStats.TimeEstimate > 0;

            // search min date with not zero time
            var noZeroSpends = spends.Where(x => x.Value > 0);

            var pairs = noZeroSpends.ToList();
            if (!pairs.Any()) return false;
            
            var date = pairs.Select(x => x.Key).Min(x => x.StartDate);
            return date >= startTime && date <= endTime;
        }

        private static bool FinishedInPeriod(Issue issue, DateTime startTime, DateTime endTime)
        {
            return issue.ClosedAt != null && issue.ClosedAt > startTime 
                                          && issue.ClosedAt < endTime;
        }

        /// <summary> Задача открыта и не находится на проверке </summary>
        private static bool IsOpen(Issue issue) => issue.State == IssueState.Opened;
        private static bool IsOpenAtMoment(Issue issue, DateTime startDate, DateTime endDate)
        {
            return issue.State switch
            {
                IssueState.Opened => issue.CreatedAt < endDate,
                IssueState.Closed => issue.ClosedAt != null && (issue.ClosedAt < startDate || issue.ClosedAt > endDate) && issue.CreatedAt < endDate,
                IssueState.All => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static bool IsCloseAtMoment(Issue issue, DateTime startDate, DateTime endDate) =>
            !IsOpenAtMoment(issue, startDate, endDate);

        /// <summary> Задача условно закрыта</summary>
        private static bool IsClosed(Issue issue) => !IsOpen(issue);
    }
    
    public struct GitStatistics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        /// <summary> Most need properties  </summary>
        /// 
        /// <summary> Оценочное время открытых задач, начатых в этом месяце </summary>
        public double OpenEstimatesStartedInPeriod { get; set; }
        
        /// <summary> Оценочное время закрытых задач, начатых в этом месяце </summary>
        public double ClosedEstimatesStartedInPeriod { get; set; }

        /// <summary> Оценочное время всех задач, начатых в этом месяце </summary>
        public double AllEstimatesStartedInPeriod { get; set; }

        /// <summary> Потраченное время закрытых задач, начатых ранее </summary>
        public double ClosedSpendsStartedInPeriod { get; set; }

        /// <summary> Потраченное время открытых задач, начатых в этом месяце </summary>
        public double OpenSpendsStartedInPeriod { get; set; }

        /// <summary> Оценочное время открытых задач, начатых ранее </summary>
        public double OpenEstimatesStartedBefore { get; set; }

        /// <summary> Оценочное время закрытых задач, начатых ранее </summary>
        public double ClosedEstimatesStartedBefore { get; set; }

        /// <summary> Оценочное время всех задач, начатых ранее </summary>
        public double AllEstimatesStartedBefore { get; set; }

        /// <summary> отраченное время открытых задач, начатых ранее </summary>
        public double OpenSpendsStartedBefore { get; set; }

        /// <summary> Потраченное время закрытых задач, начатых ранее </summary>
        public double ClosedSpendsStartedBefore { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в текущем месяце </summary>
        public double ClosedSpendInPeriod { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на открытые задачи в текущем месяце </summary>
        public double OpenSpendInPeriod { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на открытые задачи в этом месяце открытые ранее </summary>
        public double OpenSpendBefore { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на закрытые задачи в этом месяце открытые ранее </summary>
        public double ClosedSpendBefore { get; set; }

        /// <summary> Среднее оценочное время за недавние дни </summary>
        public double AllTodayEstimates { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на все задачи в этом месяце открытые в этом месяце </summary>
        public double AllSpendsStartedInPeriod { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на все задачи в этом месяце открытые ранее </summary>
        public double AllSpendsStartedBefore { get; set; }

        /// <summary> Фактическое время ПОТРАЧЕННОЕ на все задачи в этом месяце </summary>
        public double AllSpendsForPeriod { get; set; }
    }
}
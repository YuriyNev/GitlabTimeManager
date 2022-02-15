using System;
using System.Collections.Generic;
using System.Linq;
using Catel.IoC;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Helpers;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public static class StatisticsExtractor
    {
        public static GitStatistics Process(IReadOnlyList<WrappedIssue> wrappedIssues, DateTime startTime, DateTime endTime)
        {
            var statistics = new GitStatistics();
            // Most need issues
            // started in month
            var issues = wrappedIssues;
            var openIssues = issues.Where(x => IsOpen(x.Issue)).ToList();
            var closedIssues = issues.Where(x => IsClosed(x.Issue)).ToList();

            statistics.OpenEstimatesStartedInPeriod = openIssues
                .Where(issue => StartedIn(issue, startTime, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.ClosedEstimatesStartedInPeriod = closedIssues
                .Where(issue => StartedIn(issue, startTime, endTime) &&
                                FinishedInPeriod(issue.Issue, startTime, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.AllEstimatesStartedInPeriod = statistics.OpenEstimatesStartedInPeriod + statistics.ClosedEstimatesStartedInPeriod;

            statistics.OpenSpendsStartedInPeriod = openIssues
                .Where(issue => StartedIn(issue, startTime, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            statistics.ClosedSpendsStartedInPeriod = closedIssues
                .Where(issue => StartedIn(issue, startTime, endTime) &&
                                FinishedInPeriod(issue.Issue, startTime, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            // started before this month
            statistics.OpenEstimatesStartedBefore = openIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.ClosedEstimatesStartedBefore = closedIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.AllEstimatesStartedBefore = statistics.OpenEstimatesStartedBefore + statistics.ClosedEstimatesStartedBefore;

            statistics.AllTodayEstimates = issues
                .Where(issue => StartedIn(issue, DateTime.Today.AddDays(-6), DateTime.Today.AddDays(1)))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));
            
            statistics.OpenSpendsStartedBefore = openIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            statistics.ClosedSpendsStartedBefore = closedIssues
                .Where(issue => StartedIn(issue, DateTime.MinValue, startTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            var labelProcessor = IoCConfiguration.DefaultDependencyResolver.Resolve<ILabelService>();

            // Потраченное время только в этом месяце
            // На задачи начатые в этом месяце
            var withoutExcludes = issues.
                Where(x => !labelProcessor.ContainsExcludeLabels(x.Labels)).ToList();

            statistics.OpenSpendBefore = withoutExcludes.
                Where(x => IsOpenAtMoment(x.Issue, startTime, endTime)).
                Where(x => !StartedIn(x, startTime, endTime)).
                Sum(x => SpendsSumForPeriod(x, startTime, endTime));

            statistics.ClosedSpendBefore = withoutExcludes.
                Where(x => IsCloseAtMoment(x.Issue, startTime, endTime)).
                Where(x => !StartedIn(x, startTime, endTime)).
                Sum(x => SpendsSumForPeriod(x, startTime, endTime));

            statistics.OpenSpendInPeriod = withoutExcludes.
                Where(x => IsOpenAtMoment(x.Issue, startTime, endTime)).
                Where(x => StartedIn(x, startTime, endTime)).
                Sum(x => SpendsSumForPeriod(x, startTime, endTime));
            
            statistics.ClosedSpendInPeriod = withoutExcludes.
                Where(x => IsCloseAtMoment(x.Issue, startTime, endTime)).
                Where(x => StartedIn(x, startTime, endTime)).
                Sum(x => SpendsSumForPeriod(x, startTime, endTime));

            statistics.AllSpendsByWorkForPeriod = issues
                .Sum(x => x.GetMetric(labelProcessor, startTime, endTime).Duration.TotalHours);
            statistics.AllSpendsByWorkForPeriod = Math.Max(statistics.AllSpendsByWorkForPeriod, 0);
            statistics.Commits = issues.Sum(x => x.Commits.Count);

            statistics.AllSpendsStartedInPeriod = statistics.OpenSpendInPeriod + statistics.ClosedSpendInPeriod;
            statistics.AllSpendsStartedBefore = statistics.OpenSpendBefore + statistics.ClosedSpendBefore;
            statistics.AllSpendsForPeriod = statistics.AllSpendsStartedInPeriod + statistics.AllSpendsStartedBefore;

            statistics.Productivity = statistics.AllEstimatesStartedInPeriod / 100 * 100;

            return statistics;
        }

        public static double SpendsSumForPeriod(WrappedIssue issue, DateTime startDate, DateTime endDate)
        {
            if (issue.EndTime < startDate)
                return 0;

            if (issue.StartTime > endDate)
                return 0;

            var start = startDate > issue.StartTime ? startDate : issue.StartTime;
            var end = endDate < issue.EndTime ? endDate : issue.EndTime;

            var spend = GetAnyDaysSpend(start, end);
            
            return spend.TotalHours;
        }

        public static TimeSpan GetAnyDaysSpend(DateTime? start, DateTime? end)
        {
            if (start == null)
                return TimeSpan.Zero;

            if (end == null)
                return TimeSpan.Zero;
            
            var calendar = IoCConfiguration.DefaultDependencyResolver.Resolve<ICalendar>() ?? throw new ArgumentNullException("IoCConfiguration.DefaultDependencyResolver.Resolve<ICalendar>()");

            var spend = end.Value - start.Value;

            var workTime = calendar.GetWorkingTime(start.Value.Date, end.Value.Date.AddDays(1).AddTicks(-1));

            var days = TimeHelper.HoursToDays(workTime.TotalHours) - 1;

            var freeTimeInDay = TimeSpan.FromDays(1) - TimeSpan.FromHours(TimeHelper.DaysToHours(1));

            var totalFreeTime = TimeSpan.FromHours(freeTimeInDay.TotalHours * days);

            spend -= totalFreeTime;

            if (spend < TimeSpan.Zero)
                spend = TimeSpan.Zero;

            return spend;
        }

        public static double SpendsSum(WrappedIssue issue, DateTime startDate, DateTime endDate)
        {
            return issue.Spends.Keys.
                Where(x => x.StartDate >= startDate && x.EndDate <= endDate).
                Sum(key => issue.Spends[key]);
        }

        private static bool StartedIn(WrappedIssue issue, DateTime startTime, DateTime endTime)
        {
            return SpendsSumForPeriod(issue, startTime, endTime) > 0;
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
    
    public class GitStatistics
    {
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

        /// <summary> Время в работе в этом месяце </summary>
        public double AllSpendsByWorkForPeriod { get; set; }

        /// <summary> Производительность </summary>
        public double Productivity { get; set; }

        /// <summary> Коммитов </summary>
        public int Commits { get; set; }
    }
}
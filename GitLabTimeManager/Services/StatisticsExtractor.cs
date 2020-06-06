using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Responses;
using GitLabTimeManager.Helpers;

namespace GitLabTimeManager.Services
{
    public static class StatisticsExtractor
    {
        public static GitStatistics Process(IEnumerable<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endTime)
        {
            var statistics = new GitStatistics();
            // Most need issues
            // started in month
            var issues = wrappedIssues as WrappedIssue[] ?? wrappedIssues.ToArray();
            var openIssues = issues.Where(x => IsOpen(x.Issue)).ToList();
            var closedIssues = issues.Where(x => IsClosed(x.Issue)).ToList();

            statistics.OpenEstimatesStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.ClosedEstimatesStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, startDate, endTime) &&
                                FinishedInPeriod(issue.Issue, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.OpenSpendsStartedInPeriod = openIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            statistics.ClosedSpendsStartedInPeriod = closedIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, startDate, endTime) &&
                                FinishedInPeriod(issue.Issue, startDate, endTime))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));
            
            // started before this month
            statistics.OpenEstimatesStartedBefore = openIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.ClosedEstimatesStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));

            statistics.AllTodayEstimates = issues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, DateTime.Today.AddDays(-6), DateTime.Today.AddDays(1)))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate));
            
            foreach (var wrappedIssue in closedIssues) Debug.WriteLine($"{wrappedIssue.Issue.Iid} {wrappedIssue.Issue.TimeStats.HumanTimeEstimate}");


            statistics.OpenSpendsStartedBefore = openIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            statistics.ClosedSpendsStartedBefore = closedIssues
                .Where(issue => StartedInPeriod(issue.Issue, issue.Notes, DateTime.MinValue, startDate))
                .Sum(x => TimeHelper.SecondsToHours(x.Issue.TimeStats.TotalTimeSpent));

            // Потраченное время только в этом месяце
            // На задачи начатые в этом месяце
            var withoutExcludes = issues.Where(x => !x.LabelExes.IsExcludeLabels()).ToList();

            statistics.OpenSpendBefore = withoutExcludes.
                Where(x => IsOpen(x.Issue) && !x.StartedIn).Sum(x => x.SpendIn);
            statistics.ClosedSpendBefore = withoutExcludes.
                Where(x => IsClosed(x.Issue) && !x.StartedIn).Sum(x => x.SpendIn);
            statistics.OpenSpendInPeriod = withoutExcludes.
                Where(x => IsOpen(x.Issue) && x.StartedIn).Sum(x => x.SpendIn);
            statistics.ClosedSpendInPeriod = withoutExcludes.
                Where(x => IsClosed(x.Issue) && x.StartedIn).Sum(x => x.SpendIn);

            return statistics;
        }

        private static bool StartedInPeriod(Issue issue, IEnumerable<Note> notes, DateTime startTime, DateTime endTime)
        {
            var enumerable = notes.ToList();
            if (!enumerable.Any()) return issue.CreatedAt > startTime && issue.TimeStats.TimeEstimate > 0;
            
            foreach (var note in enumerable.Where(note => note.Body.ParseEstimate() > 0))
                return note.CreatedAt > startTime;

            return !enumerable.Any(x => x.CreatedAt < startTime);

        }

        private static bool FinishedInPeriod(Issue issue, DateTime startTime, DateTime endTime)
        {
            return issue.ClosedAt != null && issue.ClosedAt > startTime 
                                          && issue.ClosedAt < endTime;
        }

        /// <summary> Задача открыта и не находится на проверке </summary>
        private static bool IsOpen(Issue issue) => issue.State == IssueState.Opened;

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

        /// <summary> Потраченное время закрытых задач, начатых ранее </summary>
        public double ClosedSpendsStartedInPeriod { get; set; }

        /// <summary> Потраченное время открытых задач, начатых в этом месяце </summary>
        public double OpenSpendsStartedInPeriod { get; set; }

        /// <summary> Оценочное время открытых задач, начатых ранее </summary>
        public double OpenEstimatesStartedBefore { get; set; }

        /// <summary> Оценочное время закрытых задач, начатых ранее </summary>
        public double ClosedEstimatesStartedBefore { get; set; }

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
    }
}

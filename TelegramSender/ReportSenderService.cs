using Catel.IoC;
using GitLabTimeManager.Services;
using GitLabTimeManagerCore.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = GitLabApiClient.Models.Users.Responses.User;

namespace TelegramSender
{
    public class ReportSenderService : IReportSenderService
    {
        private ISourceControl SourceControl { get; }
        private IReportProvider ReportProvider { get; }
        private IUserProfile UserProfile { get; }
        private IUserService UserService { get; }
        private ICalendar Calendar { get; }

        public ReportSenderService(
            ISourceControl sourceControl,
            IReportProvider reportProvider,
            IUserProfile userProfile,
            IUserService userService,
            ICalendar calendar)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            ReportProvider = reportProvider ?? throw new ArgumentNullException(nameof(reportProvider));
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            UserService = userService ?? throw new ArgumentNullException(nameof(userService));
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        }

        private bool IsHoliday(DateTime dateTime) => Calendar.GetWorkingTime(dateTime.Date, dateTime) == TimeSpan.Zero;

        public async Task RunAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var scheduler = new Scheduler(IsHoliday);
            try
            {
                //scheduler.AddTask(new ScheduleTime(18, 29, 00), async () => await SendChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Changes Report"); 
                //scheduler.AddTask(new ScheduleTime(18, 29, 00), async () => await SendChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Changes Report"); 
                //await SendSummaryReportAsync(botClient, cancellationToken);
                await SendChangesReportAsync(botClient, new PeriodChecker(), cancellationToken);
                //await SendNoWorkIssuesReportAsync(botClient, cancellationToken);

                scheduler.AddTask(new ScheduleTime(12, 00, 00), async () => await SendChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Отчет по изменениям в коде 1");
                scheduler.AddTask(new ScheduleTime(15, 00, 00), async () => await SendChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Отчет по изменениям в коде 2");
                scheduler.AddTask(new ScheduleTime(18, 00, 00), async () => await SendChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Отчет по изменениям в коде 3");

                scheduler.AddTask(new ScheduleTime(10, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 1");
                scheduler.AddTask(new ScheduleTime(11, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 2");
                scheduler.AddTask(new ScheduleTime(12, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 3");
                scheduler.AddTask(new ScheduleTime(13, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 4");
                scheduler.AddTask(new ScheduleTime(14, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 5");
                scheduler.AddTask(new ScheduleTime(15, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 6");
                scheduler.AddTask(new ScheduleTime(16, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 7");
                scheduler.AddTask(new ScheduleTime(17, 00, 00), async () => await SendNoWorkIssuesReportAsync(botClient, cancellationToken), "Отчет по отсутствию задач 8");

                scheduler.AddTask(new ScheduleTime(17, 00, 00), async () => await SendSummaryReportAsync(botClient, cancellationToken), "Итоговый отчет 1");

                await Task.Delay(-1, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                scheduler.Dispose();
            }
        }

        private async Task SendChangesReportAsync(ITelegramBotClient botClient, PeriodChecker periodChecker, CancellationToken cancellationToken)
        {
            var diffReporters = new List<IReporter>(UserProfile.UserGroups.Keys.Select(x => new ChangesReporter(x)))
                .Where(x => x.Name == "Веб-разработчики")
                .ToList();

            var startTime = DateTime.Now.Date;
            var endTime = DateTime.Now;
            var newPeriod = periodChecker.IsNewDay;

            foreach (var reporter in diffReporters)
            {
                if (newPeriod)
                {
                    //await SendToRecipients(botClient, cancellationToken, "new day").ConfigureAwait(false);
                    reporter.Reset();
                }

                var allUsers = await UserService.FetchUsersAsync(cancellationToken).ConfigureAwait(true);
                var sortedReportCollection = await GetReportDataByGroup(allUsers, reporter.Name, startTime, endTime);
                //sortedReportCollection.Add(new ReportIssue
                //    { User = "Debug User", CommitChanges = new CommitChanges { Additions = DateTime.Now.Second, Deletions = DateTime.Now.Millisecond } });

                if (!reporter.CanShow(sortedReportCollection)) continue;
                var formattedReportHtml = reporter.GenerateHtmlReport(sortedReportCollection);

                await SendToRecipients(botClient, cancellationToken, formattedReportHtml).ConfigureAwait(false);
            }


            periodChecker.RememberTime(endTime);
        }

        private async Task SendSummaryReportAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var diffReporters = new List<IReporter>(UserProfile.UserGroups.Keys.Select(x => new SummaryReporter(x)))
                //.Where(x => x.Name == "Веб-разработчики")
                .ToList();

            var startTime = DateTime.Now.Date;
            var endTime = DateTime.Now;

            foreach (var reporter in diffReporters)
            {
                var allUsers = await UserService.FetchUsersAsync(cancellationToken).ConfigureAwait(true);
                var sortedReportCollection = await GetReportData(allUsers, reporter.Name, startTime, endTime);

                if (!reporter.CanShow(sortedReportCollection)) continue;
                var formattedReportHtml = reporter.GenerateHtmlReport(sortedReportCollection);

                await SendToRecipients(botClient, cancellationToken, formattedReportHtml).ConfigureAwait(false);
            }
        }

        private async Task SendNoWorkIssuesReportAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var diffReporters = new List<IReporter>(UserProfile.UserGroups.Keys.Select(x => new WithoutWorkReporter(x)))
                //.Where(x => x.Name == "Веб-разработчики")
                .ToList();

            foreach (var reporter in diffReporters)
            {
                var allUsers = await UserService.FetchUsersAsync(cancellationToken).ConfigureAwait(true);
                //var sortedReportCollection = await GetReportData(allUsers, reporter.Name, startTime, endTime);
                var sortedReportCollection = await GetIssuesReportData(allUsers, reporter.Name);

                if (!reporter.CanShow(sortedReportCollection)) continue;
                var formattedReportHtml = reporter.GenerateHtmlReport(sortedReportCollection);

                await SendToRecipients(botClient, cancellationToken, formattedReportHtml).ConfigureAwait(false);
            }
        }

        private static async Task SendToRecipients(ITelegramBotClient botClient, CancellationToken cancellationToken, string formattedReportHtml)
        {
            var storageService = IoCConfiguration.DefaultDependencyResolver.Resolve<IStorageService>();
            var storage = storageService.Deserialize();
            if (storage.SubscriptionChats == null)
                return;

            foreach (var chat in storage.SubscriptionChats)
            {
                try
                {
                    await botClient.SendTextMessageAsync(new ChatId(chat), formattedReportHtml, ParseMode.Html, cancellationToken: cancellationToken);
                }
                catch (ApiRequestException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task<ReportCollection> GetReportDataByGroup(IReadOnlyList<User> allUsers, string @group, DateTime startTime, DateTime endTime)
        {
            var groupUser = allUsers.FirstOrDefault(x => x.Name == @group);
            if (groupUser == null)
                throw new Exception($"Cannot find group '{@group}'!");

            var realUsers = UserService.GetRealUsers(allUsers, groupUser);

            var collection = await AppendZeroUserIssues(realUsers, startTime, endTime);

            var sortedReportCollection = collection
                .GroupBy(x => x.User)
                .Select(x => new ReportIssue
                {
                    User = x.Key,
                    Comments = x.Sum(y => y.Comments),
                    Commits = x.Sum(y => y.Commits),
                    CommitChanges = new CommitChanges
                    {
                        Additions = x.Sum(y => y.CommitChanges.Additions),
                        Deletions = x.Sum(y => y.CommitChanges.Deletions),
                    }
                })
                .OrderBy(x => x.CommitChanges.Additions)
                .ThenBy(x => x.CommitChanges.Deletions)
                .ThenBy(x => x.Comments);

            return new ReportCollection(sortedReportCollection);
        }

        private async Task<ReportCollection> GetReportData(IReadOnlyList<User> allUsers, string @group, DateTime startTime, DateTime endTime)
        {
            var groupUser = allUsers.FirstOrDefault(x => x.Name == @group);
            if (groupUser == null)
                throw new Exception($"Cannot find group '{@group}'!");

            var realUsers = UserService.GetRealUsers(allUsers, groupUser);

            var collection = await AppendZeroUserIssues(realUsers, startTime, endTime);

            return new ReportCollection(collection);
        }

        private async Task<ReportCollection> GetIssuesReportData(IReadOnlyList<User> allUsers, string @group)
        {
            var groupUser = allUsers.FirstOrDefault(x => x.Name == @group);
            if (groupUser == null)
                throw new Exception($"Cannot find group '{@group}'!");

            var realUsers = UserService.GetRealUsers(allUsers, groupUser);

            var workLabel = UserProfile?.LabelSettings?.BoardStateLabels?.DoingLabel;
            if (workLabel == null)
                return new ReportCollection();

            var report = await GetWithoutWorkAsync(realUsers, new List<string> { workLabel });

            return new ReportCollection(report);
        }


        private async Task<IEnumerable<ReportIssue>> AppendZeroUserIssues(IReadOnlyCollection<User> realUsers, DateTime startTime, DateTime endTime, IReadOnlyList<string>? labels = null)
        {
            var users = realUsers
                .Select(x => x.Username)
                .ToList();

            var issues = await SourceControl.RequestDataAsync(startTime, endTime, users, labels);
            var reportCollection = ReportProvider.CreateCollection(issues.WrappedIssues, startTime, endTime);

            var fullUserNames = realUsers
                .Select(x => x.Name)
                .ToList();

            var fullZeroCollection = fullUserNames.Select(x => new ReportIssue { User = x, });

            var expected = fullZeroCollection
                .Where(x => reportCollection.All(y => y.User != x.User))
                .ToList();

            var collection = reportCollection.Union(expected);
            return collection;
        }

        private async Task<IEnumerable<ReportIssue>> GetWithoutWorkAsync(IReadOnlyCollection<User> realUsers, IReadOnlyList<string>? labels = null)
        {
            var users = realUsers
                .Select(x => x.Username)
                .ToList();

            var startTime = DateTime.Now.AddMonths(-1);
            var endTime = DateTime.Now;
            var issues = await SourceControl.RequestDataAsync(startTime, endTime, users, labels);
            var reportCollection = ReportProvider.CreateCollection(issues.WrappedIssues, startTime, endTime);

            var fullUserNames = realUsers
                .Select(x => x.Name)
                .ToList();

            var fullZeroCollection = fullUserNames.Select(x => new ReportIssue { User = x, });

            var expected = fullZeroCollection
                .Where(reportIssue => reportCollection
                    .DistinctBy(x => x.User)
                    .All(y => y.User != reportIssue.User))
                .ToList();

            return expected;
        }
    }

    public interface IReportSenderService
    {
        Task RunAsync(ITelegramBotClient botClient, CancellationToken cancellationToken);
    }
}
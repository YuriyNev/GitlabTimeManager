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
            Calendar = calendar;
        }

        private bool IsHoliday(DateTime dateTime) => Calendar.GetWorkingTime(dateTime.Date, dateTime) == TimeSpan.Zero;

        public async Task RunAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var scheduler = new Scheduler(IsHoliday);
            try
            {
                scheduler.AddTask(new ScheduleTime(18, 29, 00), async () => await GetChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Changes Report"); 
                scheduler.AddTask(new ScheduleTime(18, 29, 00), async () => await GetChangesReportAsync(botClient, new PeriodChecker(), cancellationToken), "Changes Report"); 

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

        private async Task GetChangesReportAsync(ITelegramBotClient botClient, PeriodChecker periodChecker, CancellationToken cancellationToken)
        {
            var diffReporters = new List<IReporter>(UserProfile.UserGroups.Keys.Select(x => new DiffsReporter(x)))
                .Where(x => x.Name == "Веб-разработчики")
                .ToList();

            var startTime = DateTime.Now.Date;
            var endTime = DateTime.Now;
            var newPeriod = periodChecker.IsNewDay;

            foreach (var reporter in diffReporters)
            {
                if (newPeriod)
                {
                    await SendToRecipients(botClient, cancellationToken, "new day").ConfigureAwait(false);
                    reporter.Reset();
                }

                var allUsers = await UserService.FetchUsersAsync(cancellationToken).ConfigureAwait(true);
                var sortedReportCollection = await GetReportByGroup(allUsers, reporter.Name, startTime, endTime);
                //sortedReportCollection.Add(new ReportIssue
                //    { User = "Debug User", CommitChanges = new CommitChanges { Additions = DateTime.Now.Second, Deletions = DateTime.Now.Millisecond } });

                if (!reporter.CanShow(sortedReportCollection)) continue;
                var formattedReportHtml = reporter.GenerateHtmlReport(sortedReportCollection);

                await SendToRecipients(botClient, cancellationToken, formattedReportHtml).ConfigureAwait(false);
            }

            
            periodChecker.RememberTime(endTime);
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

        private async Task<ReportCollection> GetReportByGroup(IReadOnlyList<User> allUsers, string @group, DateTime startTime, DateTime endTime)
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

        private async Task<IEnumerable<ReportIssue>> AppendZeroUserIssues(IReadOnlyCollection<User> realUsers, DateTime startTime, DateTime endTime)
        {
            var users = realUsers
                .Select(x => x.Username)
                .ToList();

            var issues = await SourceControl.RequestDataAsync(startTime, endTime, users, null);
            var reportCollection = ReportProvider.CreateCollection(issues.WrappedIssues, startTime, endTime);

            var fullUserNames = realUsers
                .Select(x => x.Name)
                .ToList();

            var fullZeroCollection = fullUserNames.Select(x => new ReportIssue { User = x, });

            var expected = fullZeroCollection
                .Except(reportCollection)
                .ToList();

            var collection = reportCollection.Union(expected);
            return collection;
        }
    }
    
    public interface IReportSenderService
    {
        Task RunAsync(ITelegramBotClient botClient, CancellationToken cancellationToken);
    }
}
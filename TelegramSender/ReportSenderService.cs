using System.Text;
using System.Windows.Documents;
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

        public ReportSenderService(
            ISourceControl sourceControl, 
            IReportProvider reportProvider, 
            IUserProfile userProfile, 
            IUserService userService)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            ReportProvider = reportProvider ?? throw new ArgumentNullException(nameof(reportProvider));
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            UserService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        private Dictionary<string, ReportCollection> _oldReports;

        public async Task RunAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            try
            {
                DateTime startTime = DateTime.Now.Date;
                DateTime endTime = DateTime.Now;

                _oldReports = UserProfile.UserGroups.Keys.ToDictionary(x => x, _ => new ReportCollection());

                while (true)
                {
                    foreach (var @group in UserProfile.UserGroups.Keys)
                    {
                        var allUsers = await UserService.FetchUsersAsync(cancellationToken).ConfigureAwait(true);
                        var sortedReportCollection = await GetReportByGroup(allUsers, @group, startTime, endTime);

                        if (sortedReportCollection.IsEmpty())
                            continue;

                        if (sortedReportCollection.SequenceEqual(_oldReports[@group], new ReportCollection()))
                            continue;

                        var formattedReportHtml = CreateFormattedReport(sortedReportCollection);

                        await SendToRecipients(botClient, cancellationToken, formattedReportHtml).ConfigureAwait(false);

                        _oldReports[@group] = sortedReportCollection.Clone();
                    }

                    await Task.Delay(1 * 30_000, cancellationToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task SendToRecipients(ITelegramBotClient botClient, CancellationToken cancellationToken, string formattedReportHtml)
        {
            var storage = StorageProvider.Instance.Deserialize();
            if (storage.SubscriptionChats == null)
                return;

            foreach (var chat in storage.SubscriptionChats)
            {
                try
                {
                    await botClient.SendTextMessageAsync(new ChatId(chat), formattedReportHtml, ParseMode.Html,
                        cancellationToken: cancellationToken);
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

        private static string CreateFormattedReport(IReadOnlyList<ReportIssue> sortedReportCollection)
        {
            var stringBuilder = new StringBuilder();
            try
            {
                stringBuilder.Append("<pre>");
                var maxIssueUser = sortedReportCollection.MaxBy(x => x.User.Length);
                if (maxIssueUser == null)
                    return "<pre>empty list</pre>";

                var maxNameSize = maxIssueUser.User.Length;
                var tabUserSize = maxNameSize + 2;

                //stringBuilder.AppendLine($"{WithDynamicTab("user", tabSize)}{WithDynamicTab("commits", 7)}");

                foreach (var reportIssue in sortedReportCollection)
                {
                    stringBuilder.Append($"{Tab(reportIssue.User, tabUserSize)}");
                    stringBuilder.Append($"{Tab($"+{reportIssue.CommitChanges.Additions}/-{reportIssue.CommitChanges.Deletions}", 10)}");
                    stringBuilder.Append($"{Tab($"{reportIssue.Comments}", 3)}");
                    stringBuilder.AppendLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            finally
            {
                stringBuilder.Append("</pre>");
            }

            return stringBuilder.ToString();
        }

        private static string Tab(string value, int maxSize) => $"{value}{new string(' ', maxSize - value.Length)}";
    }
    
    public interface IReportSenderService
    {
        Task RunAsync(ITelegramBotClient botClient, CancellationToken cancellationToken);
    }
}

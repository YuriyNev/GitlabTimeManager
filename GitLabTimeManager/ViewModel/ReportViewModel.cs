using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Catel.Data;
using Catel.MVVM;
using Catel.Threading;
using GitLabApiClient.Models.Users.Responses;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Models;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class ReportViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData DataProperty = RegisterProperty<ReportViewModel, GitResponse>(x => x.Data);
        [UsedImplicitly] public static readonly PropertyData ValuesForPeriodProperty = RegisterProperty<ReportViewModel, ObservableCollection<TimeStatsProperty>>(x => x.ValuesForPeriod);
        [UsedImplicitly] public static readonly PropertyData IsProgressProperty = RegisterProperty<ReportViewModel, bool>(x => x.IsProgress);
        [UsedImplicitly] public static readonly PropertyData AllUsersProperty = RegisterProperty<ReportViewModel, IReadOnlyList<User>>(x => x.AllUsers);
        [UsedImplicitly] public static readonly PropertyData CurrentUserProperty = RegisterProperty<ReportViewModel, User>(x => x.CurrentUser);
        [UsedImplicitly] public static readonly PropertyData StartTimeProperty = RegisterProperty<ReportViewModel, DateTime>(x => x.StartTime);
        [UsedImplicitly] public static readonly PropertyData EndTimeProperty = RegisterProperty<ReportViewModel, DateTime>(x => x.EndTime);
        [UsedImplicitly] public static readonly PropertyData IsSingleUserProperty = RegisterProperty<ReportViewModel, bool>(x => x.IsSingleUser);
        [UsedImplicitly] public static readonly PropertyData CollectionViewProperty = RegisterProperty<ReportViewModel, CollectionView>(x => x.IssuesCollection);
        [UsedImplicitly] public static readonly PropertyData EpicShowingProperty = RegisterProperty<ReportViewModel, bool>(x => x.EpicShowing);

        public bool EpicShowing
        {
            get => GetValue<bool>(EpicShowingProperty);
            set => SetValue(EpicShowingProperty, value);
        }

        public CollectionView IssuesCollection
        {
            get => GetValue<CollectionView>(CollectionViewProperty);
            private set => SetValue(CollectionViewProperty, value);
        }

        public bool IsSingleUser
        {
            get => GetValue<bool>(IsSingleUserProperty);
            private set => SetValue(IsSingleUserProperty, value);
        }

        public DateTime EndTime
        {
            get => GetValue<DateTime>(EndTimeProperty);
            set => SetValue(EndTimeProperty, value);
        }

        public DateTime StartTime
        {
            get => GetValue<DateTime>(StartTimeProperty);
            set => SetValue(StartTimeProperty, value);
        }

        public User CurrentUser
        {
            get => GetValue<User>(CurrentUserProperty);
            set => SetValue(CurrentUserProperty, value);
        }

        public IReadOnlyList<User> AllUsers
        {
            get => GetValue<IReadOnlyList<User>>(AllUsersProperty);
            private set => SetValue(AllUsersProperty, value);
        }

        public bool IsProgress
        {
            get => GetValue<bool>(IsProgressProperty);
            private set => SetValue(IsProgressProperty, value);
        }

        public GitResponse Data
        {
            get => GetValue<GitResponse>(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public ObservableCollection<TimeStatsProperty> ValuesForPeriod
        {
            get => GetValue<ObservableCollection<TimeStatsProperty>>(ValuesForPeriodProperty);
            private set => SetValue(ValuesForPeriodProperty, value);
        }

        [NotNull] private IDataRequestService DataRequestService { get; }
        [NotNull] private ICalendar Calendar { get; }
        [NotNull] private IUserProfile UserProfile { get; }
        [NotNull] private INotificationMessageService MessageService { get; }
        [NotNull] private ILabelService LabelService { get; }
        [NotNull] private ISourceControl SourceControl { get; }
        [NotNull] private IDataSubscription DataSubscription { get; }

        private GitStatistics Statistics { get; set; }
        private TimeSpan WorkingTime { get; set; }
        private DateTime FullEndTime => EndTime.AddDays(1).AddTicks(-1);
        private ObservableCollection<ReportIssue> ReportIssues { get; set; }

        public Command ExportCsvCommand { get; }

        private bool _canSave = true;

        private bool CanSave() => _canSave;
        private ChangeNotificationWrapper _changeNotificationWrapper;

        public ReportViewModel(
            [NotNull] IDataRequestService dataRequestService,
            [NotNull] ICalendar calendar,
            [NotNull] IUserProfile userProfile,
            [NotNull] INotificationMessageService messageService,
            [NotNull] ILabelService labelService,
            [NotNull] ISourceControl sourceControl)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));

            _changeNotificationWrapper = new ChangeNotificationWrapper(UserProfile);
            _changeNotificationWrapper.PropertyChanged += UserProfile_PropertyChanged;

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;

            ExportCsvCommand = new Command(() => ExportCsv().WaitAndUnwrapException(), CanSave);

            StartTime = DateTime.Today.AddDays(-7);
            EndTime = DateTime.Today;

            _ = Task.Run(async () => { await RequestUsersAsync(); });
        }

        private async Task RequestUsersAsync()
        {
            var users = await SourceControl.FetchAllUsersAsync().ConfigureAwait(true);
            users = users
                .OrderBy(x => x.Name)
                .ToList();

            int position = 0;
            foreach (var group in UserProfile.UserGroups)
            {
                users.Insert(position, UserGroupEx.CreateUserAsGroup(group.Key));
                position++;
            }

            AllUsers = users.ToList();
        }

        private void UserProfile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IUserProfile.UserGroups))
            {
                RequestUsersAsync().WaitAndUnwrapException();
            }
        }

        private async Task ExportCsv()
        {
            _canSave = false;
            try
            {
                var export = new ExcelExporter();

                var defaultName = $"{StartTime.ToString("yyyy MMMM", CultureInfo.CurrentCulture)}-{EndTime.ToString("yyyy MMMM", CultureInfo.CurrentCulture)}";
                var extension = "xlsx";
                var path = await FileHelper.SaveDialog(defaultName, extension).ConfigureAwait(false);
                if (path == null)
                    throw new ArgumentNullException();

                var data = new ExportData {Issues = ReportIssues, Statistics = Statistics, WorkingTime = WorkingTime};

                IsProgress = true;
                
                var result = export.SaveAsync(path, data).ConfigureAwait(false);
                var awaiter = result.GetAwaiter();
                awaiter.OnCompleted(OnSavingFinished);
            }
            catch
            {
                OnSavingFailed();
            }
        }

        private void OnSavingFinished()
        {
            _canSave = true;
            ExportCsvCommand?.RaiseCanExecuteChanged();
            IsProgress = false;

            MessageService.OnMessage(this, "Документ сохранен");
        }

        private void OnSavingFailed()
        {
            _canSave = true;
            ExportCsvCommand?.RaiseCanExecuteChanged();
            IsProgress = false;

            MessageService.OnMessage(this, "Не удалось сохранить документ");
        }
        
        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            Data = e;

            FillReport(e);
        }

        private void FillReport(GitResponse response)
        {
            var startTime = StartTime;
            var endTime = FullEndTime;

            WorkingTime = Calendar.GetWorkingTime(startTime, endTime);

            ReportIssues = CreateCollection(response.WrappedIssues, startTime, endTime);
            IssuesCollection = (CollectionView)CollectionViewSource.GetDefaultView(ReportIssues);
            IssuesCollection.SortDescriptions.Add(new SortDescription(nameof(ReportIssue.User), ListSortDirection.Ascending));
            IssuesCollection.SortDescriptions.Add(new SortDescription(nameof(ReportIssue.TaskState), ListSortDirection.Ascending));
            if (SourceControl.CurrentUsers.Count > 1) 
                IssuesCollection.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(ReportIssue.User)));

            EpicShowing = ReportIssues.Any(x => !string.IsNullOrEmpty(x.Epic));
            Statistics = StatisticsExtractor.Process(response.WrappedIssues, startTime, endTime);

            ValuesForPeriod = ExtractSums(Statistics, WorkingTime);
        }

        private static ObservableCollection<TimeStatsProperty> ExtractSums(GitStatistics statistics, TimeSpan workingHours) 
            => new()
            {
                new("Выполнено задач", statistics.ClosedEstimatesStartedInPeriod, "ч"),
                //new TimeStatsProperty("Оценка по открытым задачам", statistics.OpenEstimatesStartedInPeriod, "ч"),
                new("из", statistics.AllEstimatesStartedInPeriod, "ч"),

                //new("Временные затраты на текущие задачи", statistics.AllSpendsStartedInPeriod, "ч"),
                //new("из", statistics.AllSpendsForPeriod, "ч"),

                new("В этом месяце рабочих часов", workingHours.TotalHours, "ч"),
                new("не заполнено", Math.Max(workingHours.TotalHours - statistics.AllSpendsByWorkForPeriod, 0), "ч"),
                new("Производительность", statistics.Productivity, "%"),
            };

        private ObservableCollection<ReportIssue> CreateCollection(IEnumerable<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endDate) =>
            new(
                wrappedIssues
                    .Where(x => x.Issue.Assignee != null)
                    .Select(x =>
                    {
                        var metrics = x.GetMetric(LabelService);
                        return new ReportIssue
                        {
                            Iid = x.Issue.Iid,
                            Title = x.Issue.Title,
                            Estimate = TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate),
                            SpendForPeriodByStage = metrics.Duration.TotalHours,
                            Iterations = metrics.Iterations,
                            SpendForPeriod = StatisticsExtractor.SpendsSum(x, startDate, endDate),
                            Activity = StatisticsExtractor.SpendsSumForPeriod(x, startDate, endDate),
                            StartTime = x.StartTime == DateTime.MaxValue ? null : x.StartTime,
                            EndTime = x.EndTime == DateTime.MinValue ? null : x.EndTime,
                            DueTime = x.DueTime,
                            Commits = x.Commits,
                            User = x.Issue.Assignee.Name,
                            Epic = x.Issue.Epic?.Title,
                            WebUri = x.Issue.WebUrl,
                            TaskState = x.Status,
                        };
                    }));

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(StartTime))
            {
                RequestNewData();
            }
            else if (e.PropertyName == nameof(EndTime))
            {
                RequestNewData();
            }
            else if (e.PropertyName == nameof(CurrentUser))
            {
                RequestNewData();
            }
            else if (e.PropertyName == nameof(Data))
            {
                UpdatePropertiesAsync();
            }
        }

        private void RequestNewData()
        {
            if (CurrentUser == null)
                return;
            
            var users = CurrentUser.IsGroup()
                ? ExtractUsersFromGroup(UserProfile.UserGroups, CurrentUser.Name) 
                : new List<string> { CurrentUser.Username };

            IsSingleUser = !CurrentUser.IsGroup();

            DataRequestService.Restart(StartTime, FullEndTime, users);
        }

        private List<string> ExtractUsersFromGroup([NotNull] Dictionary<string, IList<string>> userProfileUserGroups, [NotNull] string group)
        {
            if (userProfileUserGroups == null) throw new ArgumentNullException(nameof(userProfileUserGroups));
            if (group == null) throw new ArgumentNullException(nameof(group));

            List<string> users;
            if (userProfileUserGroups.TryGetValue(group, out var u))
            {
                users = AllUsers
                    .Where(x => u.Contains(x.Name))
                    .Select(x => x.Username)
                    .ToList();
            }
            else
            {
                users = Array.Empty<string>().ToList();
            }

            return users;
        }

        private void UpdatePropertiesAsync()
        {
            if (Data == null)
                return;
            
            FillReport(Data);
        }
        
        protected override Task CloseAsync()
        {
            _changeNotificationWrapper.UnsubscribeFromAllEvents();
            _changeNotificationWrapper = null;

            DataSubscription.NewData -= DataSubscriptionOnNewData;
            DataSubscription.Dispose();

            return base.CloseAsync();
        }
    }

    public static class UserGroupEx
    {
        private static string GroupMarker => "group";

        public static bool IsGroup(this User user) => user.Organization == GroupMarker;

        public static User CreateUserAsGroup(string name) => new() { Name = name, Organization = GroupMarker };
    }
}
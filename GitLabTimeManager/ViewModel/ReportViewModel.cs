using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;
using Catel.Threading;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Models;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class ReportViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData ReportIssuesProperty = RegisterProperty<ReportViewModel, ObservableCollection<ReportIssue>>(x => x.ReportIssues);
        [UsedImplicitly] public static readonly PropertyData LastMonthsProperty = RegisterProperty<ReportViewModel, ObservableCollection<DateTime>>(x => x.LastMonths, new ObservableCollection<DateTime>());
        [UsedImplicitly] public static readonly PropertyData SelectedMonthProperty = RegisterProperty<ReportViewModel, DateTime>(x => x.SelectedMonth);
        [UsedImplicitly] public static readonly PropertyData DataProperty = RegisterProperty<ReportViewModel, GitResponse>(x => x.Data);
        [UsedImplicitly] public static readonly PropertyData ValuesForPeriodProperty = RegisterProperty<ReportViewModel, ObservableCollection<TimeStatsProperty>>(x => x.ValuesForPeriod);
        [UsedImplicitly] public static readonly PropertyData IsProgressProperty = RegisterProperty<ReportViewModel, bool>(x => x.IsProgress);

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

        /// <summary> First Day </summary>
        public DateTime SelectedMonth
        {
            get => GetValue<DateTime>(SelectedMonthProperty);
            set => SetValue(SelectedMonthProperty, value);
        }

        public ObservableCollection<DateTime> LastMonths
        {
            get => GetValue<ObservableCollection<DateTime>>(LastMonthsProperty);
            private set => SetValue(LastMonthsProperty, value);
        }
        
        public ObservableCollection<ReportIssue> ReportIssues
        {
            get => GetValue<ObservableCollection<ReportIssue>>(ReportIssuesProperty);
            private set => SetValue(ReportIssuesProperty, value);
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
        [NotNull] private IDataSubscription DataSubscription { get; }

        private GitStatistics Statistics { get; set; }
        private TimeSpan WorkingTime { get; set; }

        public Command ExportCsvCommand { get; }

        private bool _canSave = true;

        private bool CanSave() => _canSave;

        public ReportViewModel(
            [NotNull] IDataRequestService dataRequestService, 
            [NotNull] ICalendar calendar,
            [NotNull] IUserProfile userProfile,
            [NotNull] INotificationMessageService messageService)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;

            ExportCsvCommand = new Command(() => ExportCsv().WaitAndUnwrapException(), CanSave);

            LastMonths = AddLastMonths();
            SelectedMonth = LastMonths.Max();
        }

        private async Task ExportCsv()
        {
            _canSave = false;
            try
            {
                var export = new ExcelExporter();

                var defaultName = $"{SelectedMonth.ToString("yyyy MMMM", CultureInfo.CurrentCulture)}";
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
                MessageService.OnSendMessage(this, "Не удалось сохранить документ!");
                OnSavingFinished();
            }
        }

        private void OnSavingFinished()
        {
            _canSave = true;
            ExportCsvCommand?.RaiseCanExecuteChanged();
            IsProgress = false;

            MessageService.OnSendMessage(this, "Документ сохранен");
        }

        private ObservableCollection<DateTime> AddLastMonths()
        {
            var collection = new ObservableCollection<DateTime>();
            var currentMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            collection.Add(currentMonth);
            
            int monthCount = UserProfile.RequestMonths;
            for (var i = 0; i < monthCount - 1; i++) collection.Add(currentMonth.AddMonths(-(i + 1)));
           
            return collection;
        }
        
        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            Data = e;

            FillReport(e);
        }

        private void FillReport(GitResponse response)
        {
            var startTime = SelectedMonth;
            var endTime = SelectedMonth.AddMonths(1).AddTicks(-1);

            WorkingTime = Calendar.GetWorkingTime(startTime, endTime);

            ReportIssues = CreateCollection(response.WrappedIssues, startTime, endTime);

            Statistics = StatisticsExtractor.Process(response.WrappedIssues, startTime, endTime);

            ValuesForPeriod = ExtractSums(Statistics, WorkingTime);
        }

        private static ObservableCollection<TimeStatsProperty> ExtractSums(GitStatistics statistics, TimeSpan workingHours) 
            => new ObservableCollection<TimeStatsProperty>
            {
                new TimeStatsProperty("Выполнено задач", statistics.ClosedEstimatesStartedInPeriod, "ч"),
                //new TimeStatsProperty("Оценка по открытым задачам", statistics.OpenEstimatesStartedInPeriod, "ч"),
                new TimeStatsProperty("из", statistics.AllEstimatesStartedInPeriod, "ч"),

                new TimeStatsProperty("Временные затраты на текущие задачи", statistics.AllSpendsStartedInPeriod, "ч"),
                new TimeStatsProperty("из", statistics.AllSpendsForPeriod, "ч"),

                new TimeStatsProperty("В этом месяце рабочих часов", workingHours.TotalHours, "ч"),
                new TimeStatsProperty("не заполнено", workingHours.TotalHours - statistics.AllSpendsForPeriod, "ч"),
            };

        private static ObservableCollection<ReportIssue> CreateCollection(IEnumerable<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endDate) =>
            new ObservableCollection<ReportIssue>(
                wrappedIssues.Select(x => new ReportIssue
                {
                    Iid = x.Issue.Iid,
                    Title = x.Issue.Title,
                    Estimate = TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate),
                    SpendForPeriod = StatisticsExtractor.SpendsSum(x, startDate, endDate),
                    StartTime = x.StartTime,
                }).Where(x => x.SpendForPeriod > 0));


        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(SelectedMonth))
            {
                UpdatePropertiesAsync();
            }
            else if (e.PropertyName == nameof(Data))
            {
                UpdatePropertiesAsync();
            }
        }
        
        private void UpdatePropertiesAsync()
        {
            if (Data == null)
                return;
            
            FillReport(Data);
        }
        
        protected override Task CloseAsync()
        {
            DataSubscription.NewData -= DataSubscriptionOnNewData;
            DataSubscription.Dispose();

            return base.CloseAsync();
        }
    }
}
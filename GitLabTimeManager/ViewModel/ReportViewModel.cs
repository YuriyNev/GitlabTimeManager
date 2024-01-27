using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace GitLabTimeManager.ViewModel;

[UsedImplicitly]
public class ReportViewModel : ViewModelBase
{
    [UsedImplicitly] public static readonly IPropertyData ReportIssuesProperty = RegisterProperty<ReportViewModel, ObservableCollection<ReportIssue>>(x => x.ReportIssues);
    [UsedImplicitly] public static readonly IPropertyData LastMonthsProperty = RegisterProperty<ReportViewModel, ObservableCollection<DateTime>>(x => x.LastMonths, new ObservableCollection<DateTime>());
    [UsedImplicitly] public static readonly IPropertyData SelectedMonthProperty = RegisterProperty<ReportViewModel, DateTime>(x => x.SelectedMonth);
    [UsedImplicitly] public static readonly IPropertyData DataProperty = RegisterProperty<ReportViewModel, GitResponse>(x => x.Data);
    [UsedImplicitly] public static readonly IPropertyData ValuesForPeriodProperty = RegisterProperty<ReportViewModel, ObservableCollection<TimeStatsProperty>>(x => x.ValuesForPeriod);
    [UsedImplicitly] public static readonly IPropertyData IsProgressProperty = RegisterProperty<ReportViewModel, bool>(x => x.IsProgress);

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
        private init => SetValue(LastMonthsProperty, value);
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

            var data = new ExportData { Issues = ReportIssues, Statistics = Statistics, WorkingTime = WorkingTime };

            IsProgress = true;

            var result = export.SaveAsync(path, data).ConfigureAwait(false);
            var awaiter = result.GetAwaiter();
            awaiter.OnCompleted(OnSavingFinished);
        }
        catch
        {
            MessageService.OnMessage(this, "Не удалось сохранить документ!");
            OnSavingFinished();
        }
        finally
        {
            _canSave = true;
            ExportCsvCommand?.RaiseCanExecuteChanged();
            IsProgress = false;
        }
    }

    private void OnSavingFinished() => MessageService.OnMessage(this, "The document is saved");

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
        => new()
        {
            new("Completed issues", statistics.ClosedEstimatesStartedInPeriod, "h"),
            new("from", statistics.AllEstimatesStartedInPeriod, "h"),

            new("Time spent on current issues", statistics.AllSpendsStartedInPeriod, "h"),
            new("from", statistics.AllSpendsForPeriod, "h"),

            new("Working hours this month", workingHours.TotalHours, "h"),
            new("not filled in", workingHours.TotalHours - statistics.AllSpendsForPeriod, "h"),
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


    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
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
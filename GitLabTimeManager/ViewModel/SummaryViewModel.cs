using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Models;
using GitLabTimeManager.Services;
using JetBrains.Annotations;
using LiveCharts;
using LiveCharts.Wpf;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class SummaryViewModel : ViewModelBase
    {
        private ICalendar Calendar { get; }
        private IDataRequestService DataRequestService { get; }
        private IMoneyCalculator MoneyCalculator { get; }
        private IDataSubscription DataSubscription { get; }

        #region Properties
        public static readonly PropertyData TotalSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedInPeriod);
        public static readonly PropertyData TotalEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedInPeriod);
        public static readonly PropertyData TotalSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedBefore);
        public static readonly PropertyData TotalEstimatesStaredBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedBefore);

        public static readonly PropertyData OpenSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendInPeriod);
        public static readonly PropertyData ClosedSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendInPeriod);
        public static readonly PropertyData SpendInPeriodSeriesProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.SpendSeries, new SeriesCollection());
        public static readonly PropertyData ClosedSpendBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendBefore);
        public static readonly PropertyData OpenSpendBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendBefore);
        public static readonly PropertyData OpenEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenEstimatesStartedInPeriod);
        public static readonly PropertyData ClosedEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedEstimatesStartedInPeriod);
        public static readonly PropertyData ClosedSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendsStartedInPeriod);
        public static readonly PropertyData OpenSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendsStartedInPeriod);
        public static readonly PropertyData OpenEstimatesStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenEstimatesStartedBefore);
        public static readonly PropertyData ClosedEstimatesStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedEstimatesStartedBefore);
        public static readonly PropertyData OpenSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendsStartedBefore);
        public static readonly PropertyData ClosedSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendsStartedBefore);
        public static readonly PropertyData EstimatesInPeriodProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.EstimatesSeries, new SeriesCollection());
        public static readonly PropertyData ShowingEarningProperty = RegisterProperty<SummaryViewModel, bool>(x => x.ShowingEarning);
        public static readonly PropertyData EarningProperty = RegisterProperty<SummaryViewModel, double>(x => x.Earning);
        public static readonly PropertyData AllClosedEstimatesProperty = RegisterProperty<SummaryViewModel, double>(x => x.AllClosedEstimates);
        public static readonly PropertyData ActualDesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.ActualDesiredEstimate);
        public static readonly PropertyData DesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.DesiredEstimate);
        public static readonly PropertyData AverageKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.AverageKPI);
        public static readonly PropertyData TodayKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.TodayKPI);
        public static readonly PropertyData SelectedDateProperty = RegisterProperty<SummaryViewModel, DateTime?>(x => x.SelectedDate, () => DateTime.Now);
        public static readonly PropertyData LastMonthsProperty = RegisterProperty<SummaryViewModel, ObservableCollection<DateTime>>(x => x.LastMonths, new ObservableCollection<DateTime>());
        public static readonly PropertyData SelectedMonthProperty = RegisterProperty<SummaryViewModel, DateTime>(x => x.SelectedMonth);

        public DateTime SelectedMonth
        {
            get => GetValue<DateTime>(SelectedMonthProperty);
            set => SetValue(SelectedMonthProperty, value);
        }

        public ObservableCollection<DateTime> LastMonths
        {
            get => GetValue<ObservableCollection<DateTime>>(LastMonthsProperty);
            set => SetValue(LastMonthsProperty, value);
        }

        public DateTime? SelectedDate
        {
            get => GetValue<DateTime?>(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public double TodayKPI => GetValue<double>(TodayKPIProperty);

        public double AverageKPI
        {
            get => GetValue<double>(AverageKPIProperty);
            private set => SetValue(AverageKPIProperty, value);
        }

        public double DesiredEstimate
        {
            get => GetValue<double>(DesiredEstimateProperty);
            private set => SetValue(DesiredEstimateProperty, value);
        }
        
        public double ActualDesiredEstimate
        {
            get => GetValue<double>(ActualDesiredEstimateProperty);
            private set => SetValue(ActualDesiredEstimateProperty, value);
        }

        public double AllClosedEstimates
        {
            get => GetValue<double>(AllClosedEstimatesProperty);
            private set => SetValue(AllClosedEstimatesProperty, value);
        }

        public double Earning
        {
            get => GetValue<double>(EarningProperty);
            private set => SetValue(EarningProperty, value);
        }

        public bool ShowingEarning
        {
            get => GetValue<bool>(ShowingEarningProperty);
            private set => SetValue(ShowingEarningProperty, value);
        }

        public SeriesCollection EstimatesSeries
        {
            get => GetValue<SeriesCollection>(EstimatesInPeriodProperty);
            private set => SetValue(EstimatesInPeriodProperty, value);
        }

        public double TotalEstimatesStartedBefore
        {
            get => GetValue<double>(TotalEstimatesStaredBeforeProperty);
            private set => SetValue(TotalEstimatesStaredBeforeProperty, value);
        }

        public double TotalSpendsStartedBefore
        {
            get => GetValue<double>(TotalSpendsStartedBeforeProperty);
            private set => SetValue(TotalSpendsStartedBeforeProperty, value);
        }

        public double ClosedSpendsStartedBefore
        {
            get => GetValue<double>(ClosedSpendsStartedBeforeProperty);
            private set => SetValue(ClosedSpendsStartedBeforeProperty, value);
        }

        public double OpenSpendsStartedBefore
        {
            get => GetValue<double>(OpenSpendsStartedBeforeProperty);
            private set => SetValue(OpenSpendsStartedBeforeProperty, value);
        }

        public double ClosedEstimatesStartedBefore
        {
            get => GetValue<double>(ClosedEstimatesStartedBeforeProperty);
            private set => SetValue(ClosedEstimatesStartedBeforeProperty, value);
        }

        public double OpenEstimatesStartedBefore
        {
            get => GetValue<double>(OpenEstimatesStartedBeforeProperty);
            private set => SetValue(OpenEstimatesStartedBeforeProperty, value);
        }

        public double OpenSpendBefore
        {
            get => GetValue<double>(OpenSpendBeforeProperty);
            private set => SetValue(OpenSpendBeforeProperty, value);
        }

        public double ClosedSpendBefore
        {
            get => GetValue<double>(ClosedSpendBeforeProperty);
            private set => SetValue(ClosedSpendBeforeProperty, value);
        }

        public double TotalEstimatesStartedInPeriod
        {
            get => GetValue<double>(TotalEstimatesStartedInPeriodProperty);
            private set => SetValue(TotalEstimatesStartedInPeriodProperty, value);
        }

        public double TotalSpendsStartedInPeriod
        {
            get => GetValue<double>(TotalSpendsStartedInPeriodProperty);
            private set => SetValue(TotalSpendsStartedInPeriodProperty, value);
        }

        public double ClosedSpendsStartedInPeriod
        {
            get => GetValue<double>(ClosedSpendsStartedInPeriodProperty);
            private set => SetValue(ClosedSpendsStartedInPeriodProperty, value);
        }

        public double OpenSpendsStartedInPeriod
        {
            get => GetValue<double>(OpenSpendsStartedInPeriodProperty);
            private set => SetValue(OpenSpendsStartedInPeriodProperty, value);
        }

        public double ClosedEstimatesStartedInPeriod
        {
            get => GetValue<double>(ClosedEstimatesStartedInPeriodProperty);
            private set => SetValue(ClosedEstimatesStartedInPeriodProperty, value);
        }

        public double OpenEstimatesStartedInPeriod
        {
            get => GetValue<double>(OpenEstimatesStartedInPeriodProperty);
            private set => SetValue(OpenEstimatesStartedInPeriodProperty, value);
        }

        public SeriesCollection SpendSeries
        {
            get => GetValue<SeriesCollection>(SpendInPeriodSeriesProperty);
            private set => SetValue(SpendInPeriodSeriesProperty, value);
        }

        public double ClosedSpendInPeriod
        {
            get => GetValue<double>(ClosedSpendInPeriodProperty);
            private set => SetValue(ClosedSpendInPeriodProperty, value);
        }

        public double OpenSpendInPeriod
        {
            get => GetValue<double>(OpenSpendInPeriodProperty);
            private set => SetValue(OpenSpendInPeriodProperty, value);
        }
        #endregion

        public ObservableCollection<StatsBlock> OnlyMonthStatsBlocks { get; } = new ObservableCollection<StatsBlock>();
        public ObservableCollection<StatsBlock> EarlyStatsBlocks { get; } = new ObservableCollection<StatsBlock>();

        public static Func<double, string> Formatter => x => x.ToString("F1");
        public static Func<double, string> CeilFormatter => x => x.ToString("F0");

        private DateTime StartDate { get; set; }
        private DateTime EndDate { get; set; }

        public Command ShowEarningsCommand { get; }

        public SummaryViewModel([NotNull] ICalendar calendar,
            [NotNull] IDataRequestService dataRequestService,
            [NotNull] IMoneyCalculator moneyCalculator)
        {
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            MoneyCalculator = moneyCalculator ?? throw new ArgumentNullException(nameof(moneyCalculator));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;
            
            ShowEarningsCommand = new Command(() => ShowingEarning = !ShowingEarning, () => true);

            LastMonths = AddLastMonths();
            SelectedMonth = LastMonths.Max();

            BadRedraw();
        }

        private static ObservableCollection<DateTime> AddLastMonths()
        {
            var collection = new ObservableCollection<DateTime>();
            var currentMonth = DateTime.Today.AddDays(- DateTime.Today.Day + 1);
            collection.Add(currentMonth);

            for (var i = 0; i < 3; i++) collection.Add(currentMonth.AddMonths(-(i+1)));

            return collection;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            UpdateData(e);
        }

        private async void UpdateData(GitResponse data)
        {
            StartDate = data.StartDate;
            EndDate = data.EndDate;

            var stats = StatisticsExtractor.Process(data.WrappedIssues, data.StartDate, data.EndDate);
            // Время по задачам 
            OpenEstimatesStartedInPeriod = stats.OpenEstimatesStartedInPeriod;
            ClosedEstimatesStartedInPeriod = stats.ClosedEstimatesStartedInPeriod;
            OpenSpendsStartedInPeriod = stats.OpenSpendsStartedInPeriod;
            ClosedSpendsStartedInPeriod = stats.ClosedSpendsStartedInPeriod;

            OpenEstimatesStartedBefore = stats.OpenEstimatesStartedBefore;
            ClosedEstimatesStartedBefore = stats.ClosedEstimatesStartedBefore;
            OpenSpendsStartedBefore = stats.OpenSpendsStartedBefore;
            ClosedSpendsStartedBefore = stats.ClosedSpendsStartedBefore;

            // Время по задачам фактически за период
            OpenSpendInPeriod = stats.OpenSpendInPeriod;
            ClosedSpendInPeriod = stats.ClosedSpendInPeriod;
            OpenSpendBefore = stats.OpenSpendBefore;
            ClosedSpendBefore = stats.ClosedSpendBefore;

            TotalSpendsStartedInPeriod = OpenSpendsStartedInPeriod + ClosedSpendsStartedInPeriod;
            TotalEstimatesStartedInPeriod = OpenEstimatesStartedInPeriod + ClosedEstimatesStartedInPeriod;
            
            TotalSpendsStartedBefore = OpenSpendsStartedBefore + ClosedSpendsStartedBefore;
            TotalEstimatesStartedBefore = OpenEstimatesStartedBefore + ClosedEstimatesStartedBefore;

            var workingCurrentHours = (await Calendar.GetWorkingTimeAsync(StartDate, DateTime.Now).ConfigureAwait(true)).TotalHours;
            var workingTotalHours = (await Calendar.GetWorkingTimeAsync(StartDate, EndDate).ConfigureAwait(true)).TotalHours;
            ActualDesiredEstimate = workingCurrentHours / workingTotalHours * MoneyCalculator.DesiredEstimate;
            DesiredEstimate = MoneyCalculator.DesiredEstimate;

            AllClosedEstimates = Math.Round(ClosedEstimatesStartedInPeriod, 1);

            Earning = MoneyCalculator.Calculate(TimeSpan.FromHours(AllClosedEstimates));

            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Открытые задачи", OpenSpendsStartedInPeriod, OpenEstimatesStartedInPeriod);
            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Закрытые задачи", ClosedSpendsStartedInPeriod, ClosedEstimatesStartedInPeriod);
            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Всего", TotalSpendsStartedInPeriod, TotalEstimatesStartedInPeriod);

            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Открытые задачи", OpenSpendsStartedBefore, OpenEstimatesStartedBefore);
            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Закрытые задачи", ClosedSpendsStartedBefore, ClosedEstimatesStartedBefore);
            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Всего", TotalSpendsStartedBefore, TotalEstimatesStartedBefore);

            FillCharts();
            
            AverageKPI = AllClosedEstimates / ActualDesiredEstimate * 100;

            BadRedraw();
        }

        private async void BadRedraw()
        {
            AllClosedEstimates += 0.01;
            await Task.Delay(500);
            AllClosedEstimates -= 0.01;
            await Task.Delay(500);
            AllClosedEstimates += 0.01;
            await Task.Delay(500);
            AllClosedEstimates -= 0.01;
        }

        private static void UpdateOrAddStatsBlock(ICollection<StatsBlock> collection, string title, double value, double total)
        {
            if (collection == null) return;
            var first = collection.FirstOrDefault(x => x.Title == title);
            if (first == null)
            {
                var block = new StatsBlock(title, value, total);
                collection.Add(block);
            }
            else
            {
                first.Update(value, total);
            }
        }

        private async void FillCharts()
        {
            var workTime = await GetWorkingTime(StartDate, DateTime.Today).ConfigureAwait(true);

            var remained =
                Math.Max(workTime - (ClosedSpendInPeriod + OpenSpendInPeriod + ClosedSpendBefore + OpenSpendBefore), 0);

            SpendSeries = new SeriesCollection
            {
                CreatePieSeries(OpenSpendInPeriod, "Открытые"),
                CreatePieSeries(ClosedSpendInPeriod, "Закрытые"),
                CreatePieSeries(ClosedSpendBefore, "Закрытые (старые)"),
                CreatePieSeries(OpenSpendBefore, "Открытые (старые)"),
                CreatePieSeries(remained, "Пропущенные часы", new SolidColorBrush(Colors.DarkGray)),
            };
        }

        private async Task<double> GetWorkingTime(DateTime startDate, DateTime endDate)
        {
            var workTime = TimeHelper.GetWeekdaysTime(startDate, endDate).TotalHours;

            var holidays = await Calendar.GetHolidaysAsync(startDate, endDate).ConfigureAwait(true);
            var holidayTime = holidays?.Where(x => x.Key > startDate && x.Key <= endDate).Sum(x => x.Value.TotalHours) ?? 0;

            workTime = Math.Max(workTime - holidayTime, 0);
            return workTime;
        }

        private static PieSeries CreatePieSeries(double value, string title, [CanBeNull] Brush brush = null)
        {
            if (brush == null)
                return new PieSeries
                {
                    Values = new ChartValues<double> {Math.Round(value, 1)},
                    Title = title,
                    DataLabels = IsShowLabel(value),
                };

            return new PieSeries
            {
                Values = new ChartValues<double> {Math.Round(value, 1)},
                Title = title,
                DataLabels = IsShowLabel(value),
                Fill = brush
            };

        }

        private static bool IsShowLabel(double value) => value > 4;
    }
}

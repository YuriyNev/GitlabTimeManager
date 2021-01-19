using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        #region Properties
        [UsedImplicitly] public static readonly PropertyData TotalSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData TotalEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData TotalSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedBefore);
        [UsedImplicitly] public static readonly PropertyData TotalEstimatesStaredBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedBefore);
                         
        [UsedImplicitly] public static readonly PropertyData OpenSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendInPeriod);
        [UsedImplicitly] public static readonly PropertyData ClosedSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendInPeriod);
        [UsedImplicitly] public static readonly PropertyData SpendInPeriodSeriesProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.SpendSeries, new SeriesCollection());
        [UsedImplicitly] public static readonly PropertyData ClosedSpendBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendBefore);
        [UsedImplicitly] public static readonly PropertyData OpenSpendBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendBefore);
        [UsedImplicitly] public static readonly PropertyData OpenEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenEstimatesStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData ClosedEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedEstimatesStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData ClosedSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendsStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData OpenSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendsStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData OpenEstimatesStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenEstimatesStartedBefore);
        [UsedImplicitly] public static readonly PropertyData ClosedEstimatesStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedEstimatesStartedBefore);
        [UsedImplicitly] public static readonly PropertyData OpenSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendsStartedBefore);
        [UsedImplicitly] public static readonly PropertyData ClosedSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendsStartedBefore);
        [UsedImplicitly] public static readonly PropertyData EstimatesInPeriodProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.EstimatesSeries, new SeriesCollection());
        [UsedImplicitly] public static readonly PropertyData ShowingEarningProperty = RegisterProperty<SummaryViewModel, bool>(x => x.ShowingEarning);
        [UsedImplicitly] public static readonly PropertyData EarningProperty = RegisterProperty<SummaryViewModel, double>(x => x.Earning);
        [UsedImplicitly] public static readonly PropertyData AllClosedEstimatesProperty = RegisterProperty<SummaryViewModel, double>(x => x.AllClosedEstimates);
        [UsedImplicitly] public static readonly PropertyData ActualDesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.ActualDesiredEstimate);
        [UsedImplicitly] public static readonly PropertyData DesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.DesiredEstimate);
        [UsedImplicitly] public static readonly PropertyData AverageKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.AverageKPI);
        [UsedImplicitly] public static readonly PropertyData TodayKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.TodayKPI);
        [UsedImplicitly] public static readonly PropertyData SelectedDateProperty = RegisterProperty<SummaryViewModel, DateTime?>(x => x.SelectedDate, () => DateTime.Now);
        [UsedImplicitly] public static readonly PropertyData DataProperty = RegisterProperty<SummaryViewModel, GitResponse>(x => x.Data);
        [UsedImplicitly] public static readonly PropertyData AllSpendsForPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.AllSpendsForPeriod);

        public double AllSpendsForPeriod
        {
            get => GetValue<double>(AllSpendsForPeriodProperty);
            set => SetValue(AllSpendsForPeriodProperty, value);
        }

        public GitResponse Data
        {
            get => GetValue<GitResponse>(DataProperty);
            set => SetValue(DataProperty, value);
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

        [NotNull] private ICalendar Calendar { get; }
        [NotNull] private IDataRequestService DataRequestService { get; }
        [NotNull] private IMoneyCalculator MoneyCalculator { get; }
        [NotNull] private IDataSubscription DataSubscription { get; }

        public ObservableCollection<StatsBlock> OnlyMonthStatsBlocks { get; } = new ObservableCollection<StatsBlock>();
        public ObservableCollection<StatsBlock> EarlyStatsBlocks { get; } = new ObservableCollection<StatsBlock>();

        public static Func<double, string> Formatter => x => x.ToString("F1");
        public static Func<double, string> CeilFormatter => x => x.ToString("F0");

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
        }
        
        private void DataSubscriptionOnNewData(object sender, GitResponse e) => Data = e;

        private void UpdatePropertiesInternal(GitResponse data, DateTime startDate, DateTime endDate)
        {
            if (data == null) return;
            var stats = StatisticsExtractor.Process(data.WrappedIssues, startDate, endDate);

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
            AllSpendsForPeriod = stats.AllSpendsForPeriod;

            TotalSpendsStartedInPeriod = OpenSpendsStartedInPeriod + ClosedSpendsStartedInPeriod;
            TotalEstimatesStartedInPeriod = OpenEstimatesStartedInPeriod + ClosedEstimatesStartedInPeriod;

            TotalSpendsStartedBefore = OpenSpendsStartedBefore + ClosedSpendsStartedBefore;
            TotalEstimatesStartedBefore = OpenEstimatesStartedBefore + ClosedEstimatesStartedBefore;

            var workingCurrentHours = Calendar.GetWorkingTime(startDate, DateTime.Now).TotalHours;
            var workingTotalHours = Calendar.GetWorkingTime(startDate, endDate).TotalHours;
            ActualDesiredEstimate = workingCurrentHours / workingTotalHours * MoneyCalculator.DesiredEstimate;
            DesiredEstimate = MoneyCalculator.DesiredEstimate;

            AllClosedEstimates = Math.Round(ClosedEstimatesStartedInPeriod, 1);

            Earning = MoneyCalculator.Calculate(TimeSpan.FromHours(AllClosedEstimates));

            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Открытые", OpenSpendsStartedInPeriod, OpenEstimatesStartedInPeriod);
            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Закрытые", ClosedSpendsStartedInPeriod, ClosedEstimatesStartedInPeriod);
            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Всего", TotalSpendsStartedInPeriod, TotalEstimatesStartedInPeriod);

            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Открытые", OpenSpendsStartedBefore, OpenEstimatesStartedBefore);
            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Закрытые", ClosedSpendsStartedBefore, ClosedEstimatesStartedBefore);
            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Всего", TotalSpendsStartedBefore, TotalEstimatesStartedBefore);

            FillCharts();

            AverageKPI = AllClosedEstimates / ActualDesiredEstimate * 100;
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

        private void FillCharts()
        {
            var workTime = GetWorkingTime(TimeHelper.StartDate, DateTime.Today);

            var remained =
                Math.Max(workTime - AllSpendsForPeriod, 0);

            SpendSeries = new SeriesCollection
            {
                CreatePieSeries(OpenSpendInPeriod, "Открытые"),
                CreatePieSeries(ClosedSpendInPeriod, "Закрытые"),
                CreatePieSeries(ClosedSpendBefore, "Закрытые (старые)"),
                CreatePieSeries(OpenSpendBefore, "Открытые (старые)"),
                CreatePieSeries(remained, "Пропущенные часы", new SolidColorBrush(Colors.DarkGray)),
            };
        }

        private double GetWorkingTime(DateTime startDate, DateTime endDate)
        {
            var workTime = TimeHelper.GetWeekdaysTime(startDate, endDate).TotalHours;

            var holidays = Calendar.GetHolidays();
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

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Data))
            {
                UpdatePropertiesAsync();
            }
        }

        private void UpdatePropertiesAsync()
        { 
            var startDate = TimeHelper.StartDate;
            var endDate = TimeHelper.EndDate;
            UpdatePropertiesInternal(Data, startDate, endDate);
        }
    }
}
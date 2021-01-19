using System;
using System.Collections.ObjectModel;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using JetBrains.Annotations;
using LiveCharts;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class TodayViewModel : ViewModelBase
    {
        private IDataRequestService DataRequestService { get; }
        private IDataSubscription DataSubscription { get; }
        private ICalendar Calendar { get; }

        #region Properties
        [UsedImplicitly] public static readonly PropertyData TotalSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData TotalEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedInPeriod);
        [UsedImplicitly] public static readonly PropertyData TotalSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedBefore);
        [UsedImplicitly] public static readonly PropertyData TotalEstimatesStaredBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedBefore);

        [UsedImplicitly] public static readonly PropertyData OpenSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendInPeriod);
        [UsedImplicitly] public static readonly PropertyData ClosedSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendInPeriod);
        [UsedImplicitly] public static readonly PropertyData SpendInPeriodSeriesProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.SpendSeries);
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
        [UsedImplicitly] public static readonly PropertyData EstimatesInPeriodProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.EstimatesSeries);
        [UsedImplicitly] public static readonly PropertyData ShowingEarningProperty = RegisterProperty<SummaryViewModel, bool>(x => x.ShowingEarning);
        [UsedImplicitly] public static readonly PropertyData EarningProperty = RegisterProperty<SummaryViewModel, double>(x => x.Earning);
        [UsedImplicitly] public static readonly PropertyData AllClosedEstimatesProperty = RegisterProperty<SummaryViewModel, double>(x => x.AllClosedEstimates);
        [UsedImplicitly] public static readonly PropertyData ActualDesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.ActualDesiredEstimate);
        [UsedImplicitly] public static readonly PropertyData DesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.DesiredEstimate);
        [UsedImplicitly] public static readonly PropertyData AverageKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.AverageKPI);
        [UsedImplicitly] public static readonly PropertyData TodayKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.TodayKPI);
        [UsedImplicitly] public static readonly PropertyData NecessaryDailyEstimateProperty = RegisterProperty<TodayViewModel, double>(x => x.NecessaryDailyEstimate);
        [UsedImplicitly] public static readonly PropertyData AllTodayEstimatesProperty = RegisterProperty<TodayViewModel, double>(x => x.AllTodayEstimates);

        public double AllTodayEstimates
        {
            get => GetValue<double>(AllTodayEstimatesProperty);
            private set => SetValue(AllTodayEstimatesProperty, value);
        }

        public double NecessaryDailyEstimate
        {
            get => GetValue<double>(NecessaryDailyEstimateProperty);
            private set => SetValue(NecessaryDailyEstimateProperty, value);
        }

        public double TodayKPI
        {
            get => GetValue<double>(TodayKPIProperty);
            set => SetValue(TodayKPIProperty, value);
        }

        public double AverageKPI
        {
            get => GetValue<double>(AverageKPIProperty);
            private set => SetValue(AverageKPIProperty, value);
        }

        public double DesiredEstimate
        {
            get => GetValue<double>(DesiredEstimateProperty);
            set => SetValue(DesiredEstimateProperty, value);
        }

        public double ActualDesiredEstimate
        {
            get => GetValue<double>(ActualDesiredEstimateProperty);
            set => SetValue(ActualDesiredEstimateProperty, value);
        }

        public double AllClosedEstimates
        {
            get => GetValue<double>(AllClosedEstimatesProperty);
            set => SetValue(AllClosedEstimatesProperty, value);
        }

        public double Earning
        {
            get => GetValue<double>(EarningProperty);
            set => SetValue(EarningProperty, value);
        }

        public bool ShowingEarning
        {
            get => GetValue<bool>(ShowingEarningProperty);
            set => SetValue(ShowingEarningProperty, value);
        }

        public SeriesCollection EstimatesSeries
        {
            get => GetValue<SeriesCollection>(EstimatesInPeriodProperty);
            set => SetValue(EstimatesInPeriodProperty, value);
        }

        public double TotalEstimatesStartedBefore
        {
            get => GetValue<double>(TotalEstimatesStaredBeforeProperty);
            set => SetValue(TotalEstimatesStaredBeforeProperty, value);
        }

        public double TotalSpendsStartedBefore
        {
            get => GetValue<double>(TotalSpendsStartedBeforeProperty);
            set => SetValue(TotalSpendsStartedBeforeProperty, value);
        }

        public double ClosedSpendsStartedBefore
        {
            get => GetValue<double>(ClosedSpendsStartedBeforeProperty);
            set => SetValue(ClosedSpendsStartedBeforeProperty, value);
        }

        public double OpenSpendsStartedBefore
        {
            get => GetValue<double>(OpenSpendsStartedBeforeProperty);
            set => SetValue(OpenSpendsStartedBeforeProperty, value);
        }

        public double ClosedEstimatesStartedBefore
        {
            get => GetValue<double>(ClosedEstimatesStartedBeforeProperty);
            set => SetValue(ClosedEstimatesStartedBeforeProperty, value);
        }

        public double OpenEstimatesStartedBefore
        {
            get => GetValue<double>(OpenEstimatesStartedBeforeProperty);
            set => SetValue(OpenEstimatesStartedBeforeProperty, value);
        }

        public double OpenSpendBefore
        {
            get => GetValue<double>(OpenSpendBeforeProperty);
            set => SetValue(OpenSpendBeforeProperty, value);
        }

        public double ClosedSpendBefore
        {
            get => GetValue<double>(ClosedSpendBeforeProperty);
            set => SetValue(ClosedSpendBeforeProperty, value);
        }

        public double TotalEstimatesStartedInPeriod
        {
            get => GetValue<double>(TotalEstimatesStartedInPeriodProperty);
            set => SetValue(TotalEstimatesStartedInPeriodProperty, value);
        }

        public double TotalSpendsStartedInPeriod
        {
            get => GetValue<double>(TotalSpendsStartedInPeriodProperty);
            set => SetValue(TotalSpendsStartedInPeriodProperty, value);
        }

        public double ClosedSpendsStartedInPeriod
        {
            get => GetValue<double>(ClosedSpendsStartedInPeriodProperty);
            set => SetValue(ClosedSpendsStartedInPeriodProperty, value);
        }

        public double OpenSpendsStartedInPeriod
        {
            get => GetValue<double>(OpenSpendsStartedInPeriodProperty);
            set => SetValue(OpenSpendsStartedInPeriodProperty, value);
        }

        public double ClosedEstimatesStartedInPeriod
        {
            get => GetValue<double>(ClosedEstimatesStartedInPeriodProperty);
            set => SetValue(ClosedEstimatesStartedInPeriodProperty, value);
        }

        public double OpenEstimatesStartedInPeriod
        {
            get => GetValue<double>(OpenEstimatesStartedInPeriodProperty);
            set => SetValue(OpenEstimatesStartedInPeriodProperty, value);
        }

        public SeriesCollection SpendSeries
        {
            get => GetValue<SeriesCollection>(SpendInPeriodSeriesProperty);
            set => SetValue(SpendInPeriodSeriesProperty, value);
        }

        public double ClosedSpendInPeriod
        {
            get => GetValue<double>(ClosedSpendInPeriodProperty);
            set => SetValue(ClosedSpendInPeriodProperty, value);
        }

        public double OpenSpendInPeriod
        {
            get => GetValue<double>(OpenSpendInPeriodProperty);
            set => SetValue(OpenSpendInPeriodProperty, value);
        }

        public ObservableCollection<WrappedIssue> WrappedIssues { get; set; }

        #endregion

        public TodayViewModel([NotNull] IDataRequestService dataRequestService,
            [NotNull] ICalendar calendar)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            UpdateData(e);
        }

        private void UpdateData(GitResponse data)
        {
            WrappedIssues = data.WrappedIssues;

            var stats = StatisticsExtractor.Process(data.WrappedIssues, TimeHelper.StartDate, TimeHelper.EndDate);

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

            AllTodayEstimates = stats.AllTodayEstimates;

            TotalSpendsStartedInPeriod = OpenSpendsStartedInPeriod + ClosedSpendsStartedInPeriod;
            TotalEstimatesStartedInPeriod = OpenEstimatesStartedInPeriod + ClosedEstimatesStartedInPeriod;

            TotalSpendsStartedBefore = OpenSpendsStartedBefore + ClosedSpendsStartedBefore;
            TotalEstimatesStartedBefore = OpenEstimatesStartedBefore + ClosedEstimatesStartedBefore;

            var moneyCalculator = new MoneyCalculator();

            var workingCurrentHours = Calendar.GetWorkingTime(TimeHelper.StartDate, DateTime.Now).TotalHours;
            var workingTotalHours = Calendar.GetWorkingTime(TimeHelper.StartDate, TimeHelper.EndDate).TotalHours;
            ActualDesiredEstimate = workingCurrentHours / workingTotalHours * moneyCalculator.DesiredEstimate;
            DesiredEstimate = moneyCalculator.DesiredEstimate;

            AllClosedEstimates = Math.Round(ClosedEstimatesStartedInPeriod, 1);

            Earning = moneyCalculator.Calculate(TimeSpan.FromHours(AllClosedEstimates));

            if (Math.Abs(ActualDesiredEstimate) > double.Epsilon)
                AverageKPI = AllClosedEstimates / ActualDesiredEstimate * 100;

            NecessaryDailyEstimate = TimeHelper.DaysToHours(1) * (DesiredEstimate - AllClosedEstimates) / (workingTotalHours - workingCurrentHours);
        }
    }
}
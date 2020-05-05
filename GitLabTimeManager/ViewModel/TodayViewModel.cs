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
        public ISourceControl SourceControl { get; }
        public IDataRequestService DataRequestService { get; }
        public IDataSubscription DataSubscription { get; }
        public ICalendar Calendar { get; }

        #region Properties
        public static readonly PropertyData TotalSpendsStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedInPeriod);
        public static readonly PropertyData TotalEstimatesStartedInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedInPeriod);
        public static readonly PropertyData TotalSpendsStartedBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalSpendsStartedBefore);
        public static readonly PropertyData TotalEstimatesStaredBeforeProperty = RegisterProperty<SummaryViewModel, double>(x => x.TotalEstimatesStartedBefore);

        public static readonly PropertyData OpenSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.OpenSpendInPeriod);
        public static readonly PropertyData ClosedSpendInPeriodProperty = RegisterProperty<SummaryViewModel, double>(x => x.ClosedSpendInPeriod);
        public static readonly PropertyData SpendInPeriodSeriesProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.SpendSeries);
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
        public static readonly PropertyData EstimatesInPeriodProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.EstimatesSeries);
        public static readonly PropertyData ShowingEarningProperty = RegisterProperty<SummaryViewModel, bool>(x => x.ShowingEarning);
        public static readonly PropertyData EarningProperty = RegisterProperty<SummaryViewModel, double>(x => x.Earning);
        public static readonly PropertyData AllClosedEstimatesProperty = RegisterProperty<SummaryViewModel, double>(x => x.AllClosedEstimates);
        public static readonly PropertyData ActualDesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.ActualDesiredEstimate);
        public static readonly PropertyData DesiredEstimateProperty = RegisterProperty<SummaryViewModel, double>(x => x.DesiredEstimate);
        public static readonly PropertyData AverageKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.AverageKPI);
        public static readonly PropertyData TodayKPIProperty = RegisterProperty<SummaryViewModel, double>(x => x.TodayKPI);
        public static readonly PropertyData NecessaryDailyEstimateProperty = RegisterProperty<TodayViewModel, double>(x => x.NecessaryDailyEstimate);
        public static readonly PropertyData AllTodayEstimatesProperty = RegisterProperty<TodayViewModel, double>(x => x.AllTodayEstimates);

        public double AllTodayEstimates
        {
            get => GetValue<double>(AllTodayEstimatesProperty);
            set => SetValue(AllTodayEstimatesProperty, value);
        }

        public double NecessaryDailyEstimate
        {
            get => GetValue<double>(NecessaryDailyEstimateProperty);
            set => SetValue(NecessaryDailyEstimateProperty, value);
        }

        public double TodayKPI
        {
            get => GetValue<double>(TodayKPIProperty);
            set => SetValue(TodayKPIProperty, value);
        }

        public double AverageKPI
        {
            get => GetValue<double>(AverageKPIProperty);
            set => SetValue(AverageKPIProperty, value);
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
            get => (bool)GetValue(ShowingEarningProperty);
            set => SetValue(ShowingEarningProperty, value);
        }

        public SeriesCollection EstimatesSeries
        {
            get => (SeriesCollection)GetValue(EstimatesInPeriodProperty);
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
            get => (SeriesCollection)GetValue(SpendInPeriodSeriesProperty);
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

        private DateTime StartDate { get; set; }
        private DateTime EndDate { get; set; }

        public TodayViewModel([NotNull] ISourceControl sourceControl, [NotNull] IDataRequestService dataRequestService,
            [NotNull] ICalendar calendar)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            UpdateData(e);
        }

        private async void UpdateData(GitResponse data)
        {
            StartDate = data.StartDate;
            EndDate = data.EndDate;

            WrappedIssues = data.WrappedIssues;

            // Время по задачам 
            OpenEstimatesStartedInPeriod = data.OpenEstimatesStartedInPeriod;
            ClosedEstimatesStartedInPeriod = data.ClosedEstimatesStartedInPeriod;
            OpenSpendsStartedInPeriod = data.OpenSpendsStartedInPeriod;
            ClosedSpendsStartedInPeriod = data.ClosedSpendsStartedInPeriod;

            OpenEstimatesStartedBefore = data.OpenEstimatesStartedBefore;
            ClosedEstimatesStartedBefore = data.ClosedEstimatesStartedBefore;
            OpenSpendsStartedBefore = data.OpenSpendsStartedBefore;
            ClosedSpendsStartedBefore = data.ClosedSpendsStartedBefore;

            // Время по задачам фактически за период
            OpenSpendInPeriod = data.OpenSpendInPeriod;
            ClosedSpendInPeriod = data.ClosedSpendInPeriod;
            OpenSpendBefore = data.OpenSpendBefore;
            ClosedSpendBefore = data.ClosedSpendBefore;

            AllTodayEstimates = data.AllTodayEstimates;

            TotalSpendsStartedInPeriod = OpenSpendsStartedInPeriod + ClosedSpendsStartedInPeriod;
            TotalEstimatesStartedInPeriod = OpenEstimatesStartedInPeriod + ClosedEstimatesStartedInPeriod;

            TotalSpendsStartedBefore = OpenSpendsStartedBefore + ClosedSpendsStartedBefore;
            TotalEstimatesStartedBefore = OpenEstimatesStartedBefore + ClosedEstimatesStartedBefore;

            var moneyCalculator = new MoneyCalculator();

            var workingCurrentHours = (await Calendar.GetWorkingTimeAsync(StartDate, DateTime.Now).ConfigureAwait(true)).TotalHours;
            var workingTotalHours = (await Calendar.GetWorkingTimeAsync(StartDate, EndDate).ConfigureAwait(true)).TotalHours;
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

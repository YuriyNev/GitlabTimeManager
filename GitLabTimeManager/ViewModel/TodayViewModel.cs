using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            get => (double)GetValue(AllClosedEstimatesProperty);
            set => SetValue(AllClosedEstimatesProperty, value);
        }

        public double Earning
        {
            get => (double)GetValue(EarningProperty);
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
            get => (double)GetValue(TotalEstimatesStaredBeforeProperty);
            set => SetValue(TotalEstimatesStaredBeforeProperty, value);
        }

        public double TotalSpendsStartedBefore
        {
            get => (double)GetValue(TotalSpendsStartedBeforeProperty);
            set => SetValue(TotalSpendsStartedBeforeProperty, value);
        }

        public double ClosedSpendsStartedBefore
        {
            get => (double)GetValue(ClosedSpendsStartedBeforeProperty);
            set => SetValue(ClosedSpendsStartedBeforeProperty, value);
        }

        public double OpenSpendsStartedBefore
        {
            get => (double)GetValue(OpenSpendsStartedBeforeProperty);
            set => SetValue(OpenSpendsStartedBeforeProperty, value);
        }

        public double ClosedEstimatesStartedBefore
        {
            get => (double)GetValue(ClosedEstimatesStartedBeforeProperty);
            set => SetValue(ClosedEstimatesStartedBeforeProperty, value);
        }

        public double OpenEstimatesStartedBefore
        {
            get => (double)GetValue(OpenEstimatesStartedBeforeProperty);
            set => SetValue(OpenEstimatesStartedBeforeProperty, value);
        }

        public double OpenSpendBefore
        {
            get => (double)GetValue(OpenSpendBeforeProperty);
            set => SetValue(OpenSpendBeforeProperty, value);
        }

        public double ClosedSpendBefore
        {
            get => (double)GetValue(ClosedSpendBeforeProperty);
            set => SetValue(ClosedSpendBeforeProperty, value);
        }

        public double TotalEstimatesStartedInPeriod
        {
            get => (double)GetValue(TotalEstimatesStartedInPeriodProperty);
            set => SetValue(TotalEstimatesStartedInPeriodProperty, value);
        }

        public double TotalSpendsStartedInPeriod
        {
            get => (double)GetValue(TotalSpendsStartedInPeriodProperty);
            set => SetValue(TotalSpendsStartedInPeriodProperty, value);
        }

        public double ClosedSpendsStartedInPeriod
        {
            get => (double)GetValue(ClosedSpendsStartedInPeriodProperty);
            set => SetValue(ClosedSpendsStartedInPeriodProperty, value);
        }

        public double OpenSpendsStartedInPeriod
        {
            get => (double)GetValue(OpenSpendsStartedInPeriodProperty);
            set => SetValue(OpenSpendsStartedInPeriodProperty, value);
        }

        public double ClosedEstimatesStartedInPeriod
        {
            get => (double)GetValue(ClosedEstimatesStartedInPeriodProperty);
            set => SetValue(ClosedEstimatesStartedInPeriodProperty, value);
        }

        public double OpenEstimatesStartedInPeriod
        {
            get => (double)GetValue(OpenEstimatesStartedInPeriodProperty);
            set => SetValue(OpenEstimatesStartedInPeriodProperty, value);
        }

        public SeriesCollection SpendSeries
        {
            get => (SeriesCollection)GetValue(SpendInPeriodSeriesProperty);
            set => SetValue(SpendInPeriodSeriesProperty, value);
        }

        public double ClosedSpendInPeriod
        {
            get => (double)GetValue(ClosedSpendInPeriodProperty);
            set => SetValue(ClosedSpendInPeriodProperty, value);
        }

        public double OpenSpendInPeriod
        {
            get => (double)GetValue(OpenSpendInPeriodProperty);
            set => SetValue(OpenSpendInPeriodProperty, value);
        }

        public ObservableCollection<WrappedIssue> WrappedIssues { get; set; }

        #endregion

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public TodayViewModel([NotNull] SuperParameter superParameter)
        {
            if (superParameter == null) throw new ArgumentNullException(nameof(superParameter));
            SourceControl = superParameter.SourceControl ?? throw new ArgumentNullException(nameof(superParameter.SourceControl));
            Calendar = superParameter.Calendar ?? throw new ArgumentNullException(nameof(superParameter.Calendar));
        }

        public async void UpdateData(GitResponse data)
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
            
            AverageKPI = AllClosedEstimates / ActualDesiredEstimate * 100;

            NecessaryDailyEstimate = TimeHelper.DaysToHours(1) * (DesiredEstimate - AllClosedEstimates) / (workingTotalHours - workingCurrentHours);
        }

        
    }
}

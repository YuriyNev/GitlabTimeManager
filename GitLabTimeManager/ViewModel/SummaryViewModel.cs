using System;
using System.Collections.ObjectModel;
using System.Linq;
using Catel.Data;
using Catel.MVVM;
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
        public ISourceControl SourceControl { get; }

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

        public double Earning
        {
            get { return (double) GetValue(EarningProperty); }
            set { SetValue(EarningProperty, value); }
        }

        public bool ShowingEarning
        {
            get { return (bool) GetValue(ShowingEarningProperty); }
            set { SetValue(ShowingEarningProperty, value); }
        }

        public SeriesCollection EstimatesSeries
        {
            get { return (SeriesCollection) GetValue(EstimatesInPeriodProperty); }
            set { SetValue(EstimatesInPeriodProperty, value); }
        }

        public double TotalEstimatesStartedBefore
        {
            get { return (double)GetValue(TotalEstimatesStaredBeforeProperty); }
            set { SetValue(TotalEstimatesStaredBeforeProperty, value); }
        }

        public double TotalSpendsStartedBefore
        {
            get { return (double)GetValue(TotalSpendsStartedBeforeProperty); }
            set { SetValue(TotalSpendsStartedBeforeProperty, value); }
        }

        public double ClosedSpendsStartedBefore
        {
            get { return (double) GetValue(ClosedSpendsStartedBeforeProperty); }
            set { SetValue(ClosedSpendsStartedBeforeProperty, value); }
        }

        public double OpenSpendsStartedBefore
        {
            get { return (double) GetValue(OpenSpendsStartedBeforeProperty); }
            set { SetValue(OpenSpendsStartedBeforeProperty, value); }
        }

        public double ClosedEstimatesStartedBefore
        {
            get { return (double) GetValue(ClosedEstimatesStartedBeforeProperty); }
            set { SetValue(ClosedEstimatesStartedBeforeProperty, value); }
        }

        public double OpenEstimatesStartedBefore
        {
            get { return (double) GetValue(OpenEstimatesStartedBeforeProperty); }
            set { SetValue(OpenEstimatesStartedBeforeProperty, value); }
        }

        public double OpenSpendBefore
        {
            get { return (double)GetValue(OpenSpendBeforeProperty); }
            set { SetValue(OpenSpendBeforeProperty, value); }
        }

        public double ClosedSpendBefore
        {
            get { return (double)GetValue(ClosedSpendBeforeProperty); }
            set { SetValue(ClosedSpendBeforeProperty, value); }
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
            get { return (SeriesCollection) GetValue(SpendInPeriodSeriesProperty); }
            set { SetValue(SpendInPeriodSeriesProperty, value); }
        }

        public double ClosedSpendInPeriod
        {
            get { return (double) GetValue(ClosedSpendInPeriodProperty); }
            set { SetValue(ClosedSpendInPeriodProperty, value); }
        }

        public double OpenSpendInPeriod
        {
            get { return (double) GetValue(OpenSpendInPeriodProperty); }
            set { SetValue(OpenSpendInPeriodProperty, value); }
        }


        #endregion

        public ObservableCollection<StatsBlock> OnlyMonthStatsBlocks { get; } = new ObservableCollection<StatsBlock>();
        public ObservableCollection<StatsBlock> EarlyStatsBlocks { get; } = new ObservableCollection<StatsBlock>();

        public Func<double, string> Formatter => (x => x.ToString("F1"));

        public Command ShowEarningsCommand { get; }


        public SummaryViewModel([NotNull] ISourceControl sourceControl)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            SpendSeries = new SeriesCollection();
            EstimatesSeries = new SeriesCollection();
            ShowEarningsCommand = new Command(() => ShowingEarning = !ShowingEarning, () => true);
        }

        public void UpdateData(GitResponse data)
        {
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


            TotalSpendsStartedInPeriod = OpenSpendsStartedInPeriod + ClosedSpendsStartedInPeriod;
            TotalEstimatesStartedInPeriod = OpenEstimatesStartedInPeriod + ClosedEstimatesStartedInPeriod;
            
            TotalSpendsStartedBefore = OpenSpendsStartedBefore + ClosedSpendsStartedBefore;
            TotalEstimatesStartedBefore = OpenEstimatesStartedBefore + ClosedEstimatesStartedBefore;
            //=(70 +(A6-100))*1000
            Earning = (ClosedEstimatesStartedInPeriod - 30) * 1000;
            var minimalEarning = 14000;
            Earning = Math.Max(Earning, minimalEarning);

            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Открытые задачи", OpenSpendsStartedInPeriod, OpenEstimatesStartedInPeriod);
            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Закрытые задачи", ClosedSpendsStartedInPeriod, ClosedEstimatesStartedInPeriod);
            UpdateOrAddStatsBlock(OnlyMonthStatsBlocks, "Всего", TotalSpendsStartedInPeriod, TotalEstimatesStartedInPeriod);

            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Открытые задачи", OpenSpendsStartedBefore, OpenEstimatesStartedBefore);
            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Закрытые задачи", ClosedSpendsStartedBefore, ClosedEstimatesStartedBefore);
            UpdateOrAddStatsBlock(EarlyStatsBlocks, "Всего", TotalSpendsStartedBefore, TotalEstimatesStartedBefore);


            FillCharts();
        }

        private static void UpdateOrAddStatsBlock(ObservableCollection<StatsBlock> collection, string title, double value, double total)
        {
            if (collection == null) return;
            var f = collection.FirstOrDefault(x => x.Title == title);
            if (f == null)
            {
                var block = new StatsBlock(title, value, total);
                collection.Add(block);
            }
            else
            {
                f.Update(value, total);
            }
        }

        private void FillCharts()
        {
            SpendSeries = new SeriesCollection()
            {
                new PieSeries
                {
                    Values = new ChartValues<double> {ClosedSpendInPeriod},
                    Title = "Закрытые",
                },
                new PieSeries
                {
                    Values = new ChartValues<double> {OpenSpendInPeriod},
                    Title = "Открытые",
                },
                new PieSeries
                {
                    Values = new ChartValues<double> {ClosedSpendBefore},
                    Title = "Закрытые (старые)",
                },
                new PieSeries
                {
                    Values = new ChartValues<double> {OpenSpendBefore},
                    Title = "Открытые (старые)",
                },
                
            };

            //EstimatesSeries = new SeriesCollection
            //{
            //    new Gauge()
            //    {
            //        Value = OpenEstimatesStartedInPeriod + ClosedEstimatesStartedInPeriod,
            //        From = 0,
            //        To = 100,
            //    }
            //};
        }
    }
}

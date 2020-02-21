using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Services;
using JetBrains.Annotations;
using LiveCharts;
using LiveCharts.Wpf;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class SummaryViewModel : ViewModelBase
    {
        public static readonly PropertyData TotalSpendInPeriodProperty = RegisterProperty<SummaryViewModel, int>(x => x.TotalSpendInPeriod);
        public static readonly PropertyData OpenSpendInPeriodProperty = RegisterProperty<SummaryViewModel, int>(x => x.OpenSpendInPeriod);
        public static readonly PropertyData ClosedSpendInPeriodProperty = RegisterProperty<SummaryViewModel, int>(x => x.ClosedSpendInPeriod);
        public static readonly PropertyData PropertyDataProperty = RegisterProperty<SummaryViewModel, SeriesCollection>(x => x.SpendInPeriodSeries);

        public SeriesCollection SpendInPeriodSeries
        {
            get { return (SeriesCollection) GetValue(PropertyDataProperty); }
            set { SetValue(PropertyDataProperty, value); }
        }

        public int ClosedSpendInPeriod
        {
            get { return (int) GetValue(ClosedSpendInPeriodProperty); }
            set { SetValue(ClosedSpendInPeriodProperty, value); }
        }

        public int OpenSpendInPeriod
        {
            get { return (int) GetValue(OpenSpendInPeriodProperty); }
            set { SetValue(OpenSpendInPeriodProperty, value); }
        }

        public int TotalSpendInPeriod
        {
            get { return (int) GetValue(TotalSpendInPeriodProperty); }
            set { SetValue(TotalSpendInPeriodProperty, value); }
        }
        
        public SummaryViewModel()
        {
            
        }

        public void UpdateData(GitResponse data)
        {
            TotalSpendInPeriod = data.TotalSpendInPeriod;
            OpenSpendInPeriod = data.OpenSpendInPeriod;
            ClosedSpendInPeriod = data.ClosedSpendInPeriod;

            FillCharts();
        }

        private void FillCharts()
        {
            int allHours = 160;
            SpendInPeriodSeries = new SeriesCollection()
            {
                new PieSeries
                {
                    Values = new ChartValues<int> {TotalSpendInPeriod},
                    Title = "Закрытые задачи: потрачено в этом месяце",
                },
                new PieSeries
                {
                    Values = new ChartValues<int> {TotalSpendInPeriod},
                    Title = "Открытые задачи: потрачено в этом месяце",
                },
                new PieSeries
                {
                    Values = new ChartValues<int> {allHours - TotalSpendInPeriod},
                    Title = "Осталось до конца месяца",
                },
            };
        }
    }
}

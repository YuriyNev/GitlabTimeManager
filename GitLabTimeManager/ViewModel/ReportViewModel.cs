﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Models;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class ReportViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData ReportIssuesProperty = RegisterProperty<ReportViewModel, ObservableCollection<ReportIssue>>(x => x.ReportIssues);
        [UsedImplicitly] public static readonly PropertyData LastMonthsProperty = RegisterProperty<ReportViewModel, ObservableCollection<DateTime>>(x => x.LastMonths, new ObservableCollection<DateTime>());
        [UsedImplicitly] public static readonly PropertyData SelectedMonthProperty = RegisterProperty<ReportViewModel, DateTime>(x => x.SelectedMonth);
        [UsedImplicitly] public static readonly PropertyData DataProperty = RegisterProperty<ReportViewModel, GitResponse>(x => x.Data);
        [UsedImplicitly] public static readonly PropertyData ValuesForPeriodProperty = RegisterProperty<ReportViewModel, ObservableCollection<TimeStatsProperty>>(x => x.ValuesForPeriod);

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

        private IDataRequestService DataRequestService { get; }
        private ICalendar Calendar { get; }
        private IDataSubscription DataSubscription { get; }
        
        public ReportViewModel([NotNull] IDataRequestService dataRequestService, [NotNull] ICalendar calendar)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;

            LastMonths = AddLastMonths();
            SelectedMonth = LastMonths.Max();
        }
        
        private static ObservableCollection<DateTime> AddLastMonths()
        {
            var collection = new ObservableCollection<DateTime>();
            var currentMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            collection.Add(currentMonth);

            const int monthCount = 6;
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
           
            ReportIssues = CreateCollection(response.WrappedIssues, startTime, endTime);

            ValuesForPeriod = ExtractSums(response.WrappedIssues, startTime, endTime);
        }

        private static ObservableCollection<TimeStatsProperty> ExtractSums(IReadOnlyList<WrappedIssue> wrappedIssues, DateTime startTime, DateTime endTime)
        {
            var statistics = StatisticsExtractor.Process(wrappedIssues, startTime, endTime);
            return new ObservableCollection<TimeStatsProperty>
            {
                new TimeStatsProperty("Оценка по закрытым задачам", statistics.ClosedEstimatesStartedInPeriod, "ч"),
                new TimeStatsProperty("Оценка по открытым задачам", statistics.OpenEstimatesStartedInPeriod, "ч"),
                new TimeStatsProperty("Оценка по всем задачам", statistics.AllEstimatesStartedInPeriod, "ч"),
                new TimeStatsProperty("Затраты на старые задачи", statistics.AllSpendsStartedBefore, "ч"),
                new TimeStatsProperty("Затраты на текущие задачи", statistics.AllSpendsStartedInPeriod, "ч"),
                new TimeStatsProperty("Затраты по всем задачам", statistics.AllSpendsForPeriod, "ч"),
            };
        }

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
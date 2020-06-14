using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class ReportViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData ReportIssuesProperty = RegisterProperty<ReportViewModel, ObservableCollection<ReportIssue>>(x => x.ReportIssues);

        public ObservableCollection<ReportIssue> ReportIssues
        {
            get => GetValue<ObservableCollection<ReportIssue>>(ReportIssuesProperty);
            private set => SetValue(ReportIssuesProperty, value);
        }

        private IDataRequestService DataRequestService { get; }
        private IDataSubscription DataSubscription { get; }

        public ReportViewModel([NotNull] IDataRequestService dataRequestService)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e) => 
            ReportIssues = CreateCollection(e.WrappedIssues, TimeHelper.StartDate, TimeHelper.EndDate);

        private static ObservableCollection<ReportIssue> CreateCollection(IEnumerable<WrappedIssue> wrappedIssues, DateTime startDate, DateTime endDate) =>
            new ObservableCollection<ReportIssue>(
                wrappedIssues.Select(x => new ReportIssue
                {
                    Iid = x.Issue.Iid,
                    Title = x.Issue.Title,
                    Estimate = TimeHelper.SecondsToHours(x.Issue.TimeStats.TimeEstimate),
                    SpendForPeriod = StatisticsExtractor.SpendsSum(x, startDate, endDate)
                }).Where(x => x.SpendForPeriod > 0));

        protected override Task CloseAsync()
        {
            DataSubscription.NewData -= DataSubscriptionOnNewData;
            DataSubscription.Dispose();

            return base.CloseAsync();
        }
    }
}
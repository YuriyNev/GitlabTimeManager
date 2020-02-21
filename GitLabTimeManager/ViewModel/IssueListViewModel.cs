using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Catel.MVVM;
using GitLabApiClient;
using GitLabApiClient.Models.Issues.Responses;
using JetBrains.Annotations;
using Catel.Data;
using GitLabTimeManager.Services;


namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class IssueListViewModel : ViewModelBase
    {
        public static readonly PropertyData SpendPerMonthProperty = RegisterProperty<IssueListViewModel, DateTime>(x => x.SpendPerMonth);
        public static readonly PropertyData EstimatePerMonthProperty = RegisterProperty<IssueListViewModel, DateTime>(x => x.EstimatePerMonth);
        public static readonly PropertyData OpenIssuesCountProperty = RegisterProperty<IssueListViewModel, int>(x => x.OpenIssuesCount);
        public static readonly PropertyData ClosedIssuesCountProperty = RegisterProperty<IssueListViewModel, int>(x => x.ClosedIssuesCount);
        public static readonly PropertyData TotalSpentProperty = RegisterProperty<IssueListViewModel, int>(x => x.TotalSpent);
        public static readonly PropertyData TotalEstimateProperty = RegisterProperty<IssueListViewModel, int>(x => x.TotalEstimate);
        public static readonly PropertyData ClosedTotalEstimateProperty = RegisterProperty<IssueListViewModel, int>(x => x.ClosedTotalEstimate);
        public static readonly PropertyData ClosedTotalSpentProperty = RegisterProperty<IssueListViewModel, int>(x => x.ClosedTotalSpent);
        public static readonly PropertyData OpenTotalSpentProperty = RegisterProperty<IssueListViewModel, int>(x => x.OpenTotalSpent);
        public static readonly PropertyData OpenTotalEstimateProperty = RegisterProperty<IssueListViewModel, int>(x => x.OpenTotalEstimate);
        public static readonly PropertyData OpenSpendInPeriodProperty = RegisterProperty<IssueListViewModel, int>(x => x.OpenSpendInPeriod);
        public static readonly PropertyData ClosedSpendInPeriodProperty = RegisterProperty<IssueListViewModel, int>(x => x.ClosedSpendInPeriod);

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

        public int OpenTotalEstimate
        {
            get { return (int) GetValue(OpenTotalEstimateProperty); }
            set { SetValue(OpenTotalEstimateProperty, value); }
        }


        public int OpenTotalSpent
        {
            get { return (int) GetValue(OpenTotalSpentProperty); }
            set { SetValue(OpenTotalSpentProperty, value); }
        }

        public int ClosedTotalSpent
        {
            get { return (int) GetValue(ClosedTotalSpentProperty); }
            set { SetValue(ClosedTotalSpentProperty, value); }
        }

        public int ClosedTotalEstimate
        {
            get { return (int) GetValue(ClosedTotalEstimateProperty); }
            set { SetValue(ClosedTotalEstimateProperty, value); }
        }

        public int TotalEstimate
        {
            get { return (int) GetValue(TotalEstimateProperty); }
            set { SetValue(TotalEstimateProperty, value); }
        }

        public int TotalSpent
        {
            get { return (int) GetValue(TotalSpentProperty); }
            set { SetValue(TotalSpentProperty, value); }
        }

        public int ClosedIssuesCount
        {
            get { return (int) GetValue(ClosedIssuesCountProperty); }
            set { SetValue(ClosedIssuesCountProperty, value); }
        }

        public int OpenIssuesCount
        {
            get { return (int) GetValue(OpenIssuesCountProperty); }
            set { SetValue(OpenIssuesCountProperty, value); }
        }

        public DateTime SpendPerMonth
        {
            get => (DateTime) GetValue(SpendPerMonthProperty);
            set => SetValue(SpendPerMonthProperty, value);
        }

        public DateTime EstimatePerMonth
        {
            get => (DateTime)GetValue(EstimatePerMonthProperty);
            set => SetValue(EstimatePerMonthProperty, value);
        }

        public ObservableCollection<Issue> OpenIssues { get; } = new ObservableCollection<Issue>();
        public ObservableCollection<Issue> ClosedIssues { get; } = new ObservableCollection<Issue>();
        public ObservableCollection<Issue> AllIssues { get; } = new ObservableCollection<Issue>();
        [NotNull] private ISourceControl SourceControl { get; }

        public IssueListViewModel([NotNull] ModelProperty modelProperty)
        {
            if (modelProperty == null) throw new ArgumentNullException(nameof(modelProperty));
            SourceControl = modelProperty.SourceControl;
           
        }

        protected override Task InitializeAsync()
        {
            return base.InitializeAsync();
        }
    }
}

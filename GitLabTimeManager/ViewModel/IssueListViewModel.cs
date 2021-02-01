using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Catel.Data;
using Catel.MVVM;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class IssueListViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData CurrentIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.CurrentIssue);
        [UsedImplicitly] public static readonly PropertyData IssueTimerVmProperty = RegisterProperty<IssueListViewModel, IssueTimerViewModel>(x => x.IssueTimerVm);
        [UsedImplicitly] public static readonly PropertyData SelectedIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.SelectedIssue);
        [UsedImplicitly] public static readonly PropertyData IsFullscreenProperty = RegisterProperty<IssueListViewModel, bool>(x => x.IsFullscreen);

        [ViewModelToModel][UsedImplicitly]
        public bool IsFullscreen
        {
            get => GetValue<bool>(IsFullscreenProperty);
            set => SetValue(IsFullscreenProperty, value);
        }

        public WrappedIssue SelectedIssue
        {
            get => GetValue<WrappedIssue>(SelectedIssueProperty);
            set => SetValue(SelectedIssueProperty, value);
        }

        [Model(SupportIEditableObject = false)][NotNull]
        [UsedImplicitly]
        public IssueTimerViewModel IssueTimerVm
        {
            get => GetValue<IssueTimerViewModel>(IssueTimerVmProperty);
            set => SetValue(IssueTimerVmProperty, value);
        }

        private WrappedIssue CurrentIssue => GetValue<WrappedIssue>(CurrentIssueProperty);

        private ObservableCollection<WrappedIssue> WrappedIssues { get; }

        [NotNull] private IDataRequestService DataRequestService { get; }
        [NotNull] private IViewModelFactory ViewModelFactory { get; }
        [NotNull] private ILabelService LabelService { get; }
        [NotNull] private IDataSubscription DataSubscription { get; }

        [NotNull] public CollectionView IssueCollectionView { get; }

        public IssueListViewModel([NotNull] IDataRequestService dataRequestService, [NotNull] IViewModelFactory modelFactory, [NotNull] ILabelService labelService)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));
            ViewModelFactory = modelFactory ?? throw new ArgumentNullException(nameof(modelFactory));
            LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;

            WrappedIssues = new ObservableCollection<WrappedIssue> ();
            IssueCollectionView = (CollectionView)CollectionViewSource.GetDefaultView(WrappedIssues);
            IssueCollectionView.SortDescriptions.Add(new SortDescription(nameof(WrappedIssue.Issue.Iid), ListSortDirection.Descending));
            IssueCollectionView.SortDescriptions.Add(new SortDescription(nameof(WrappedIssue.Spend), ListSortDirection.Descending));
            IssueCollectionView.Filter = Filter;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            UpdateData(e);
        }

        private static bool Filter(object obj) => obj is WrappedIssue wi
                                                  && wi.Issue.State == IssueState.Opened;

        private void UpdateData(GitResponse data)
        {
            CopyIssueValues(WrappedIssues, data.WrappedIssues);

            SelectedIssue ??= WrappedIssues.FirstOrDefault();
        }

        private static void CopyIssueValues(ICollection<WrappedIssue> dst, IReadOnlyList<WrappedIssue> src)
        {
            foreach (var issue in src)
            {
                var destIssue = dst.FirstOrDefault(x => x.Issue.Iid == issue.Issue.Iid);
                if (destIssue != null)
                {
                    if (destIssue.Equals(issue)) continue;

                    destIssue.Issue = issue.Issue;
                    destIssue.Labels = issue.Labels;
                    destIssue.StartTime = issue.StartTime;
                    destIssue.EndTime = issue.EndTime;
                }
                else
                {
                    dst.Add(issue);
                }
            }
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(SelectedIssue))
            {
                if (SelectedIssue != null)
                    IssueTimerVm = ViewModelFactory.CreateViewModel<IssueTimerViewModel>(SelectedIssue);
            }
        }

        protected override Task OnClosingAsync()
        {
            IssueTimerVm.CancelAndCloseViewModelAsync();
            return base.OnClosingAsync();
        }
    }
}
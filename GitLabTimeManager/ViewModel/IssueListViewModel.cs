using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Catel.MVVM;
using JetBrains.Annotations;
using Catel.Data;
using GitLabApiClient.Models.Issues.Responses;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class IssueListViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData PropertyDataProperty = RegisterProperty<IssueListViewModel, ObservableCollection<WrappedIssue>>(x => x.WrappedIssues);
        [UsedImplicitly] public static readonly PropertyData CurrentIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.CurrentIssue);
        [UsedImplicitly] public static readonly PropertyData IssueTimerVmProperty = RegisterProperty<IssueListViewModel, IssueTimerViewModel>(x => x.IssueTimerVm);
        [UsedImplicitly] public static readonly PropertyData SelectedIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.SelectedIssue);
        [UsedImplicitly] public static readonly PropertyData IsFullscreenProperty = RegisterProperty<IssueListViewModel, bool>(x => x.IsFullscreen);

        [ViewModelToModel, UsedImplicitly]
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

        [Model(SupportIEditableObject = false), NotNull]
        [UsedImplicitly]
        public IssueTimerViewModel IssueTimerVm
        {
            get => GetValue<IssueTimerViewModel>(IssueTimerVmProperty);
            set => SetValue(IssueTimerVmProperty, value);
        }

        public WrappedIssue CurrentIssue => GetValue<WrappedIssue>(CurrentIssueProperty);

        public ObservableCollection<WrappedIssue> WrappedIssues
        {
            get => GetValue<ObservableCollection<WrappedIssue>>(PropertyDataProperty);
            set => SetValue(PropertyDataProperty, value);
        }

        private ISourceControl SourceControl { get; }
        private IDataRequestService DataRequestService { get; }
        public CollectionView IssueCollectionView { get; }
        private IDataSubscription DataSubscription { get; }

        public IssueListViewModel([NotNull] ISourceControl sourceControl, [NotNull] IDataRequestService dataRequestService)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;


            WrappedIssues = new ObservableCollection<WrappedIssue> ();
            IssueCollectionView = (CollectionView)CollectionViewSource.GetDefaultView(WrappedIssues);
            IssueCollectionView.SortDescriptions.Add(new SortDescription(nameof(WrappedIssue.Issue.Iid), ListSortDirection.Descending));
            IssueCollectionView.Filter = Filter;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            UpdateData(e);
        }

        private static bool Filter(object obj) => obj is WrappedIssue wi 
                                                  && wi.Issue.State == IssueState.Opened 
                                                  && !wi.LabelExes.Contains(LabelsCollection.DistrLabel)
                                                  && !wi.LabelExes.Contains(LabelsCollection.RevisionLabel);

        private void UpdateData(GitResponse data)
        {
            CopyIssueValues(WrappedIssues, data.WrappedIssues);

            SelectedIssue ??= WrappedIssues.FirstOrDefault();
        }

        private static void CopyIssueValues(ICollection<WrappedIssue> dst, IEnumerable<WrappedIssue> src)
        {
            foreach (var issue in src)
            {
                var destIssue = dst.FirstOrDefault(x => x.Issue.Iid == issue.Issue.Iid);
                if (destIssue != null)
                {
                    destIssue.Issue = issue.Issue;
                    destIssue.LabelExes = issue.LabelExes;
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
                    IssueTimerVm = new IssueTimerViewModel(SourceControl, SelectedIssue);
            }
        }

        protected override Task OnClosingAsync()
        {
            IssueTimerVm.CancelAndCloseViewModelAsync();
            return base.OnClosingAsync();
        }
    }

}

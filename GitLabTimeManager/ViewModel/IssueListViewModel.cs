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
using GitLabTimeManager.Services;

namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class IssueListViewModel : ViewModelBase
    {
        public static readonly PropertyData PropertyDataProperty = RegisterProperty<IssueListViewModel, ObservableCollection<WrappedIssue>>(x => x.WrappedIssues);
        public static readonly PropertyData CurrentIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.CurrentIssue);
        public static readonly PropertyData IssueTimerVmProperty = RegisterProperty<IssueListViewModel, IssueTimerViewModel>(x => x.IssueTimerVm);
        public static readonly PropertyData SelectedIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.SelectedIssue);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<IssueListViewModel, bool>(x => x.IsFullscreen);

        [ViewModelToModel]
        public bool IsFullscreen
        {
            get => (bool) GetValue(IsFullscreenProperty);
            set => SetValue(IsFullscreenProperty, value);
        }

        public WrappedIssue SelectedIssue
        {
            get => (WrappedIssue) GetValue(SelectedIssueProperty);
            set => SetValue(SelectedIssueProperty, value);
        }

        [Model(SupportIEditableObject = false), NotNull]
        [UsedImplicitly]
        public IssueTimerViewModel IssueTimerVm
        {
            get => (IssueTimerViewModel) GetValue(IssueTimerVmProperty);
            set => SetValue(IssueTimerVmProperty, value);
        }

        public WrappedIssue CurrentIssue
        {
            get => (WrappedIssue) GetValue(CurrentIssueProperty);
            set => SetValue(CurrentIssueProperty, value);
        }

        public ObservableCollection<WrappedIssue> WrappedIssues
        {
            get => (ObservableCollection<WrappedIssue>) GetValue(PropertyDataProperty);
            set => SetValue(PropertyDataProperty, value);
        }

        private ISourceControl SourceControl { get; }
        public CollectionView IssueCollectionView { get; }

        public IssueListViewModel([NotNull] SuperParameter superParameter)
        {
            if (superParameter == null) throw new ArgumentNullException(nameof(superParameter));
            SourceControl = superParameter.SourceControl ?? throw new ArgumentNullException(nameof(superParameter.SourceControl));

            WrappedIssues = new ObservableCollection<WrappedIssue> ();
            IssueCollectionView = (CollectionView)CollectionViewSource.GetDefaultView(WrappedIssues);
            IssueCollectionView.SortDescriptions.Add(new SortDescription(nameof(WrappedIssue.Issue.Iid), ListSortDirection.Descending));
            IssueCollectionView.Filter = Filter;
        }

        private static bool Filter(object obj) => obj is WrappedIssue wi && wi.Issue.State == IssueState.Opened;

        public void UpdateData(GitResponse data)
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

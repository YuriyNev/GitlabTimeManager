using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Catel.Collections;
using Catel.MVVM;
using JetBrains.Annotations;
using Catel.Data;
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
        public CollectionView IssueView { get; set; }

        public IssueListViewModel([NotNull] SuperParameter superParameter)
        {
            if (superParameter == null) throw new ArgumentNullException(nameof(superParameter));
            SourceControl = superParameter.SourceControl ?? throw new ArgumentNullException(nameof(superParameter.SourceControl));

            WrappedIssues = new ObservableCollection<WrappedIssue> ();
            
        }

        public void UpdateData(GitResponse data)
        {
            if (SelectedIssue != null)
            {
                WrappedIssues = new ObservableCollection<WrappedIssue> { SelectedIssue };
                WrappedIssues.AddRange(data.WrappedIssues.Where(x => x.Issue.Iid != SelectedIssue.Issue.Iid));
            }
            else
            {
                WrappedIssues = data.WrappedIssues;
                //IssueView = (CollectionView)CollectionViewSource.GetDefaultView(WrappedIssues);
                //IssueView.SortDescriptions.Add(
                //    new SortDescription(nameof(WrappedIssue.StartTime), ListSortDirection.Descending));
            }

            if (SelectedIssue == null)
                SelectedIssue = WrappedIssues.FirstOrDefault();
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

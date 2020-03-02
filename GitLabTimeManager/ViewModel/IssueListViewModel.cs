using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        public IssueListViewModel([NotNull] ISourceControl sourceControl)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
        }

        public void UpdateData(GitResponse data)
        {
            WrappedIssues = data.WrappedIssues;
            if (SelectedIssue == null)
                SelectedIssue = WrappedIssues.FirstOrDefault();
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(SelectedIssue))
            {
                IssueTimerVm = new IssueTimerViewModel(SourceControl, SelectedIssue);
            }
        }
    }

}

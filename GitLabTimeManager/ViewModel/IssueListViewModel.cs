using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Catel.MVVM;
using GitLabApiClient;
using GitLabApiClient.Models.Issues.Responses;
using JetBrains.Annotations;
using Catel.Data;
using GitLabApiClient.Models.Releases.Responses;
using GitLabTimeManager.Services;
using MahApps.Metro.Controls;


namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class IssueListViewModel : ViewModelBase
    {
        public static readonly PropertyData PropertyDataProperty = RegisterProperty<IssueListViewModel, ObservableCollection<WrappedIssue>>(x => x.WrappedIssues);
        public static readonly PropertyData CurrentIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.CurrentIssue);
        public static readonly PropertyData HasStartedIssueProperty = RegisterProperty<IssueListViewModel, bool>(x => x.HasStartedIssue);
        public static readonly PropertyData IssueTimerVmProperty = RegisterProperty<IssueListViewModel, IssueTimerViewModel>(x => x.IssueTimerVm);
        public static readonly PropertyData SelectedIssueProperty = RegisterProperty<IssueListViewModel, WrappedIssue>(x => x.SelectedIssue);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<IssueListViewModel, bool>(x => x.IsFullscreen);

        [ViewModelToModel]
        public bool IsFullscreen
        {
            get { return (bool) GetValue(IsFullscreenProperty); }
            set { SetValue(IsFullscreenProperty, value); }
        }

        public WrappedIssue SelectedIssue
        {
            get { return (WrappedIssue) GetValue(SelectedIssueProperty); }
            set { SetValue(SelectedIssueProperty, value); }
        }

        [Model(SupportIEditableObject = false), NotNull]
        [UsedImplicitly]
        public IssueTimerViewModel IssueTimerVm
        {
            get { return (IssueTimerViewModel) GetValue(IssueTimerVmProperty); }
            set { SetValue(IssueTimerVmProperty, value); }
        }

        public bool HasStartedIssue
        {
            get { return (bool) GetValue(HasStartedIssueProperty); }
            set { SetValue(HasStartedIssueProperty, value); }
        }

        public WrappedIssue CurrentIssue
        {
            get { return (WrappedIssue) GetValue(CurrentIssueProperty); }
            set { SetValue(CurrentIssueProperty, value); }
        }

        public ObservableCollection<WrappedIssue> WrappedIssues
        {
            get { return (ObservableCollection<WrappedIssue>) GetValue(PropertyDataProperty); }
            set { SetValue(PropertyDataProperty, value); }
        }

        public ISourceControl SourceControl { get; }

        public Command<WrappedIssue> StartIssueCommand { get; }
        public Command<WrappedIssue> PauseIssueCommand { get; }


        public IssueListViewModel([NotNull] ISourceControl sourceControl)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            StartIssueCommand = new Command<WrappedIssue>(StartIssue);
            PauseIssueCommand = new Command<WrappedIssue>(PauseIssue);
        }

        private void PauseIssue(WrappedIssue issue)
        {
            issue.InProgress = false;
            HasStartedIssue = false;

            IssueTimerVm = null;
        }

        private void StartIssue(WrappedIssue issue)
        {
            issue.InProgress = true;
            HasStartedIssue = true;
            IssueTimerVm = new IssueTimerViewModel(SourceControl, issue);
        }

        public void UpdateData(GitResponse data)
        {
            WrappedIssues = data.WrappedIssues;
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

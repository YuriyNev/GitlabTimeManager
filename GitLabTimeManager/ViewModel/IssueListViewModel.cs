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
        public static readonly PropertyData PropertyDataProperty = RegisterProperty<IssueListViewModel, ObservableCollection<WrappedIssue>>(x => x.WrappedIssues);

        public ObservableCollection<WrappedIssue> WrappedIssues
        {
            get { return (ObservableCollection<WrappedIssue>) GetValue(PropertyDataProperty); }
            set { SetValue(PropertyDataProperty, value); }
        }

        public IssueListViewModel()
        {
           
        }

        protected override Task InitializeAsync()
        {
            return base.InitializeAsync();
        }

        public void UpdateData(GitResponse data)
        {
            WrappedIssues = data.WrappedIssues;
        }
    }
}

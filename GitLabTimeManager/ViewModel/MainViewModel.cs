using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public IViewModelFactory ViewModelFactory { get; }
        public CancellationTokenSource LifeTime { get; }

        public static readonly PropertyData IssueListVmProperty = RegisterProperty<MainViewModel, IssueListViewModel>(x => x.IssueListVm);
        public static readonly PropertyData SummaryVmProperty = RegisterProperty<MainViewModel, SummaryViewModel>(x => x.SummaryVm);
        public static readonly PropertyData IsProgressProperty = RegisterProperty<MainViewModel, bool>(x => x.IsProgress);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFullscreen);
        public static readonly PropertyData ShowOnTaskbarProperty = RegisterProperty<MainViewModel, bool>(x => x.ShowOnTaskbar, true);

        public bool ShowOnTaskbar
        {
            get => (bool) GetValue(ShowOnTaskbarProperty);
            set => SetValue(ShowOnTaskbarProperty, value);
        }
        
        [ViewModelToModel]
        public bool IsFullscreen
        {
            get => (bool) GetValue(IsFullscreenProperty);
            set => SetValue(IsFullscreenProperty, value);
        }

        public SummaryViewModel SummaryVm
        {
            get => (SummaryViewModel)GetValue(SummaryVmProperty);
            set => SetValue(SummaryVmProperty, value);
        }

        [Model(SupportIEditableObject = false), NotNull]
        [UsedImplicitly]
        public IssueListViewModel IssueListVm
        {
            get => (IssueListViewModel)GetValue(IssueListVmProperty);
            set => SetValue(IssueListVmProperty, value);
        }

        public bool IsProgress
        {
            get => (bool) GetValue(IsProgressProperty);
            set => SetValue(IsProgressProperty, value);
        }

        private ISourceControl SourceControl { get; }


        public MainViewModel()
        {
            LifeTime = new CancellationTokenSource();
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            SourceControl = new SourceControl();

            IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(SourceControl);
            SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(SourceControl);

            RequestDataAsync(LifeTime.Token).Watch("Bad =(");

            Application.Current.Exit += Current_Exit;
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            CloseViewModelAsync(false);
        }

        private async Task RequestDataAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                IsProgress = true;

                var data = await SourceControl.RequestDataAsync().ConfigureAwait(true);

                IsProgress = false;
                SummaryVm.UpdateData(data);
                IssueListVm.UpdateData(data);
                await Task.Delay(Int32.MaxValue, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override Task CloseAsync()
        {
            LifeTime.Cancel();
            LifeTime.Dispose();
            return base.CloseAsync();
        }


        protected override Task OnClosingAsync()
        {
            IssueListVm.CancelAndCloseViewModelAsync();
            return base.OnClosingAsync();
        }
    }
}

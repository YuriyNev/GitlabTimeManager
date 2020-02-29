using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
using ControlzEx.Standard;
using GitLabTimeManager.Services;
using JetBrains.Annotations;
using Timer = Catel.Threading.Timer;

namespace GitLabTimeManager.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public IViewModelFactory ViewModelFactory { get; }
        public CancellationTokenSource LifeTime { get; }

        public static readonly PropertyData IssueListVmProperty = RegisterProperty<MainViewModel, IssueListViewModel>(x => x.IssueListVm);
        public static readonly PropertyData SummaryVmProperty = RegisterProperty<MainViewModel, SummaryViewModel>(x => x.SummaryVm);
        public static readonly PropertyData IsProgressProperty = RegisterProperty<MainViewModel, bool>(x => x.IsProgress);


        public SummaryViewModel SummaryVm
        {
            get => (SummaryViewModel)GetValue(SummaryVmProperty);
            set => SetValue(SummaryVmProperty, value);
        }
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
        private Timer RequestTimer { get; }

        public MainViewModel()
        {
            LifeTime = new CancellationTokenSource();
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            SourceControl = new SourceControl();

            IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(SourceControl);
            SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(SourceControl);

            RequestDataAsync(LifeTime.Token);
        }

        private async void RequestDataAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                IsProgress = true;
                
                var data = await SourceControl.RequestData();

                IsProgress = false;
                SummaryVm.UpdateData(data);
                IssueListVm.UpdateData(data);
                await Task.Delay(12000_000, cancellationToken);
            }
        }

        protected override Task CloseAsync()
        {
            LifeTime.Cancel();
            LifeTime.Dispose();
            return base.CloseAsync();
        }

    }

    public class ModelProperty
    {
        public ISourceControl SourceControl { get; }

        public ModelProperty([NotNull] ISourceControl sourceControl)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
        }
    }
}

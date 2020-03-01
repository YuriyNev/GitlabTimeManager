using System.Threading;
using System.Threading.Tasks;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
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
                await Task.Delay(600_000, cancellationToken);
            }
        }

        protected override Task CloseAsync()
        {
            LifeTime.Cancel();
            LifeTime.Dispose();
            return base.CloseAsync();
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsFullscreen))
            {
            }
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public static MainViewModel CreateInstance()
        {
            return new MainViewModel();
        }

        private IViewModelFactory ViewModelFactory { get; }
        private IDataRequestService DataRequestService { get; }
        private IDataSubscription DataSubscription { get; }
        private CancellationTokenSource LifeTime { get; }

        public static readonly PropertyData IssueListVmProperty = RegisterProperty<MainViewModel, IssueListViewModel>(x => x.IssueListVm);
        public static readonly PropertyData SummaryVmProperty = RegisterProperty<MainViewModel, SummaryViewModel>(x => x.SummaryVm);
        public static readonly PropertyData IsProgressProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFirstLoading, true);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFullscreen);
        public static readonly PropertyData ShowOnTaskBarProperty = RegisterProperty<MainViewModel, bool>(x => x.ShowOnTaskBar, true);
        public static readonly PropertyData GanttVmProperty = RegisterProperty<MainViewModel, GanttViewModel>(x => x.GanttVm);
        public static readonly PropertyData TodayVmProperty = RegisterProperty<MainViewModel, TodayViewModel>(x => x.TodayVm);

       
        public bool ShowOnTaskBar
        {
            get => GetValue<bool>(ShowOnTaskBarProperty);
            set => SetValue(ShowOnTaskBarProperty, value);
        }
        
        [ViewModelToModel, UsedImplicitly]
        public bool IsFullscreen
        {
            get => GetValue<bool>(IsFullscreenProperty);
            set => SetValue(IsFullscreenProperty, value);
        }

        public SummaryViewModel SummaryVm
        {
            get => GetValue<SummaryViewModel>(SummaryVmProperty);
            private set => SetValue(SummaryVmProperty, value);
        }

        public TodayViewModel TodayVm
        {
            get => GetValue<TodayViewModel>(TodayVmProperty);
            set => SetValue(TodayVmProperty, value);
        }

        public GanttViewModel GanttVm
        {
            get => GetValue<GanttViewModel>(GanttVmProperty);
            set => SetValue(GanttVmProperty, value);
        }

        [Model(SupportIEditableObject = false), NotNull]
        [UsedImplicitly]
        public IssueListViewModel IssueListVm
        {
            get => GetValue<IssueListViewModel>(IssueListVmProperty);
            set => SetValue(IssueListVmProperty, value);
        }

        public bool IsFirstLoading
        {
            get => GetValue<bool>(IsProgressProperty);
            private set => SetValue(IsProgressProperty, value);
        }

        private MainViewModel()
        {
            LifeTime = new CancellationTokenSource();
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            DataRequestService = dependencyResolver.Resolve<IDataRequestService>();
            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscription_NewData;

            IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(null);
            SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(null);
            TodayVm = ViewModelFactory.CreateViewModel<TodayViewModel>(null);

            Application.Current.Exit += Current_Exit;
        }

        private void DataSubscription_NewData(object sender, GitResponse e)
        {
            LoadingFinished();
        }

        private void LoadingFinished() => IsFirstLoading = false;

        private void Current_Exit(object sender, ExitEventArgs e) => CloseViewModelAsync(false);

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

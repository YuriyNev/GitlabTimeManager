using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using Catel.Threading;
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
        public static readonly PropertyData IsProgressProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFirstLoading, true);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFullscreen);
        public static readonly PropertyData ShowOnTaskbarProperty = RegisterProperty<MainViewModel, bool>(x => x.ShowOnTaskbar, true);
        public static readonly PropertyData GanttVmProperty = RegisterProperty<MainViewModel, GanttViewModel>(x => x.GanttVm);
        public static readonly PropertyData TodayVmProperty = RegisterProperty<MainViewModel, TodayViewModel>(x => x.TodayVm);

       
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
            get => (IssueListViewModel)GetValue(IssueListVmProperty);
            set => SetValue(IssueListVmProperty, value);
        }

        public bool IsFirstLoading
        {
            get => (bool) GetValue(IsProgressProperty);
            private set => SetValue(IsProgressProperty, value);
        }

        private ISourceControl SourceControl { get; }


        public MainViewModel()
        {
            LifeTime = new CancellationTokenSource();
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            SourceControl = new SourceControl();

            var calendar = new WorkingCalendar();

            var superParameter = new SuperParameter
            {
                Calendar = calendar,
                SourceControl = SourceControl
            };
            IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(superParameter);
            SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(superParameter);
            TodayVm = ViewModelFactory.CreateViewModel<TodayViewModel>(superParameter);
            //GanttVm = ViewModelFactory.CreateViewModel<GanttViewModel>(null);

            RequestDataAsync(LifeTime.Token).Watch("Bad =(");

            Application.Current.Exit += Current_Exit;
        }

        private void LoadingFinished()
        {
            IsFirstLoading = false;
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

                var data = await SourceControl.RequestDataAsync().ConfigureAwait(true);

                SummaryVm.UpdateData(data);
                IssueListVm.UpdateData(data);
                TodayVm.UpdateData(data);
                //GanttVm.UpdateData(data);

                LoadingFinished();
#if DEBUG
                await Task.Delay(20_000, cancellationToken).ConfigureAwait(true);
#else
                await Task.Delay(10_60_000, cancellationToken).ConfigureAwait(true);
#endif
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

    public class SuperParameter
    {
        public ISourceControl SourceControl { get; set; }
        public ICalendar Calendar { get; set; }
    }
}

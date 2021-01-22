using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using GitLabTimeManager.Services;
using GitLabTimeManager.Types;
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
        private INotificationMessageService NotificationService { get; }

        private CancellationTokenSource LifeTime { get; } = new CancellationTokenSource();

        [UsedImplicitly] public static readonly PropertyData IssueListVmProperty = RegisterProperty<MainViewModel, IssueListViewModel>(x => x.IssueListVm);
        [UsedImplicitly] public static readonly PropertyData SummaryVmProperty = RegisterProperty<MainViewModel, SummaryViewModel>(x => x.SummaryVm);
        [UsedImplicitly] public static readonly PropertyData IsProgressProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFirstLoading, true);
        [UsedImplicitly] public static readonly PropertyData IsFullscreenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFullscreen);
        [UsedImplicitly] public static readonly PropertyData ShowOnTaskBarProperty = RegisterProperty<MainViewModel, bool>(x => x.ShowOnTaskBar, true);
        [UsedImplicitly] public static readonly PropertyData TodayVmProperty = RegisterProperty<MainViewModel, TodayViewModel>(x => x.TodayVm);
        [UsedImplicitly] public static readonly PropertyData ReportVmProperty = RegisterProperty<MainViewModel, ReportViewModel>(x => x.ReportVm);
        [UsedImplicitly] public static readonly PropertyData ErrorProperty = RegisterProperty<MainViewModel, string>(x => x.Error);
        [UsedImplicitly] public static readonly PropertyData LaunchIsSuccessProperty = RegisterProperty<MainViewModel, bool>(x => x.LaunchIsFinished);
        [UsedImplicitly] public static readonly PropertyData MessageProperty = RegisterProperty<MainViewModel, string>(x => x.Message);
        [UsedImplicitly] public static readonly PropertyData IsMessageOpenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsMessageOpen);

        public bool IsMessageOpen
        {
            get => GetValue<bool>(IsMessageOpenProperty);
            set => SetValue(IsMessageOpenProperty, value);
        }

        public string Message
        {
            get => GetValue<string>(MessageProperty);
            private set => SetValue(MessageProperty, value);
        }

        public string Error
        {
            get => GetValue<string>(ErrorProperty);
            private set => SetValue(ErrorProperty, value);
        }
        
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
            private set => SetValue(TodayVmProperty, value);
        }
        
        public ReportViewModel ReportVm
        {
            get => GetValue<ReportViewModel>(ReportVmProperty);
            set => SetValue(ReportVmProperty, value);
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
        
        public bool LaunchIsFinished
        {
            get => GetValue<bool>(LaunchIsSuccessProperty);
            private set => SetValue(LaunchIsSuccessProperty, value);
        }

        private MainViewModel()
        {
            Application.Current.Exit += Current_Exit;
            
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;

            if (!GitTestLaunch(dependencyResolver))
                return;

            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            DataRequestService = dependencyResolver.Resolve<IDataRequestService>();
            NotificationService = dependencyResolver.Resolve<INotificationMessageService>();
            
            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscription_NewData;
            DataSubscription.NewException += DataSubscription_NewException;

            NotificationService.NewMessage += Notification_NewMessage;

            IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(null);
            SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(null);
            TodayVm = ViewModelFactory.CreateViewModel<TodayViewModel>(null);
            ReportVm = ViewModelFactory.CreateViewModel<ReportViewModel>(null);
        }

        private void Notification_NewMessage(object sender, string message)
        {
            Message = message;
            IsMessageOpen = true;
        }

        private bool GitTestLaunch(IDependencyResolver dependencyResolver)
        {
            try
            {
                dependencyResolver.Resolve<ISourceControl>();
                
                return true;
            }
            catch (Exception ex)
            {
                LaunchWithError(ex);

                return false;
            }
        }

        private void DataSubscription_NewException(object sender, Exception e)
        {
            LaunchWithError(e);
        }

        private void LaunchWithError(Exception e)
        {
            LoadingFinished();

            Error = e switch
            {
                IncorrectProfileException _ => "Не удалось загрузить профиль :(",
                UnableConnectionException _ => "Не удалось подключиться =(",
                _ => "Ошибочка вышла ;("
            };
        }

        private void DataSubscription_NewData(object sender, GitResponse e)
        {
            SuccessLoading();
        }

        private void LoadingFinished()
        {
            LaunchIsFinished = true;
        }

        private void SuccessLoading()
        {
            IsFirstLoading = false;
            LaunchIsFinished = true;
        }

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Responses;
using GitLabApiClient.Models.Users.Responses;
using GitLabTimeManager.Services;
using GitLabTimeManager.Types;
using GitLabTimeManager.ViewModel.Settings;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public static MainViewModel CreateInstance()
        {
            return new();
        }

        private IViewModelFactory ViewModelFactory { get; }
        private IDataRequestService DataRequestService { get; }
        private IDataSubscription DataSubscription { get; }
        private INotificationMessageService MessageService { get; }
        private IMessageSubscription MessageSubscription { get; }
        private ISourceControl SourceControl { get; }

        private CancellationTokenSource LifeTime { get; } = new();

        [UsedImplicitly] public static readonly PropertyData IssueListVmProperty = RegisterProperty<MainViewModel, IssueListViewModel>(x => x.IssueListVm);
        [UsedImplicitly] public static readonly PropertyData SummaryVmProperty = RegisterProperty<MainViewModel, SummaryViewModel>(x => x.SummaryVm);
        [UsedImplicitly] public static readonly PropertyData ShowOnTaskBarProperty = RegisterProperty<MainViewModel, bool>(x => x.ShowOnTaskBar, true);
        [UsedImplicitly] public static readonly PropertyData ReportVmProperty = RegisterProperty<MainViewModel, ReportViewModel>(x => x.ReportVm);
        [UsedImplicitly] public static readonly PropertyData ErrorProperty = RegisterProperty<MainViewModel, string>(x => x.Error);
        [UsedImplicitly] public static readonly PropertyData LaunchIsSuccessProperty = RegisterProperty<MainViewModel, bool>(x => x.LaunchIsFinished);
        [UsedImplicitly] public static readonly PropertyData MessageProperty = RegisterProperty<MainViewModel, string>(x => x.Message);
        [UsedImplicitly] public static readonly PropertyData IsMessageOpenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsMessageOpen);
        [UsedImplicitly] public static readonly PropertyData IsSettingsOpenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsSettingsOpen);
        [UsedImplicitly] public static readonly PropertyData IsDefaultTabProperty = RegisterProperty<MainViewModel, bool>(x => x.IsDefaultTab);
        [UsedImplicitly] public static readonly PropertyData ConnectionSettingsVmProperty = RegisterProperty<MainViewModel, ConnectionSettingsViewModel>(x => x.ConnectionSettingsVm);
        [UsedImplicitly] public static readonly PropertyData GanttViewModelProperty = RegisterProperty<MainViewModel, GanttViewModel>(x => x.GanttViewModel);
        [UsedImplicitly] public static readonly PropertyData InProgressProperty = RegisterProperty<MainViewModel, bool>(x => x.InProgress);
        [UsedImplicitly] public static readonly PropertyData LabelSettingsVmProperty = RegisterProperty<MainViewModel, LabelSettingsViewModel>(x => x.LabelSettingsVm);
        [UsedImplicitly] public static readonly PropertyData UserGroupsVmProperty = RegisterProperty<MainViewModel, UserGroupsSettingsViewModel>(x => x.UserGroupsVm);
        [UsedImplicitly] public static readonly PropertyData LoadingStatusProperty = RegisterProperty<MainViewModel, string>(x => x.LoadingStatus);

        public string LoadingStatus
        {
            get => GetValue<string>(LoadingStatusProperty);
            set => SetValue(LoadingStatusProperty, value);
        }

        public UserGroupsSettingsViewModel UserGroupsVm
        {
            get => GetValue<UserGroupsSettingsViewModel>(UserGroupsVmProperty);
            set => SetValue(UserGroupsVmProperty, value);
        }

        public bool InProgress
        {
            get => GetValue<bool>(InProgressProperty);
            set => SetValue(InProgressProperty, value);
        }
        
        public GanttViewModel GanttViewModel
        {
            get => GetValue<GanttViewModel>(GanttViewModelProperty);
            set => SetValue(GanttViewModelProperty, value);
        }

        public bool IsDefaultTab
        {
            get => GetValue<bool>(IsDefaultTabProperty);
            set => SetValue(IsDefaultTabProperty, value);
        }

        public bool IsSettingsOpen
        {
            get => GetValue<bool>(IsSettingsOpenProperty);
            set => SetValue(IsSettingsOpenProperty, value);
        }

        public ConnectionSettingsViewModel ConnectionSettingsVm
        {
            get => GetValue<ConnectionSettingsViewModel>(ConnectionSettingsVmProperty);
            private set => SetValue(ConnectionSettingsVmProperty, value);
        }

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
        
        public SummaryViewModel SummaryVm
        {
            get => GetValue<SummaryViewModel>(SummaryVmProperty);
            private set => SetValue(SummaryVmProperty, value);
        }

        public ReportViewModel ReportVm
        {
            get => GetValue<ReportViewModel>(ReportVmProperty);
            private set => SetValue(ReportVmProperty, value);
        }

        [Model(SupportIEditableObject = false)]
        [NotNull]
        [UsedImplicitly]
        public IssueListViewModel IssueListVm
        {
            get => GetValue<IssueListViewModel>(IssueListVmProperty);
            set => SetValue(IssueListVmProperty, value);
        }
        public bool LaunchIsFinished
        {
            get => GetValue<bool>(LaunchIsSuccessProperty);
            private set => SetValue(LaunchIsSuccessProperty, value);
        }

        public LabelSettingsViewModel LabelSettingsVm
        {
            get => GetValue<LabelSettingsViewModel>(LabelSettingsVmProperty);
            private set => SetValue(LabelSettingsVmProperty, value);
        }

        [UsedImplicitly]
        public Command SwitchSettingsCommand { get; }

        private MainViewModel()
        {
            Application.Current.Exit += Current_Exit;
            
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            DataRequestService = dependencyResolver.Resolve<IDataRequestService>();
            MessageService = dependencyResolver.Resolve<INotificationMessageService>();
            
            MessageSubscription = MessageService.CreateSubscription();
            MessageSubscription.NewMessage += Notification_NewMessage;

            if (!GitTestLaunch(dependencyResolver))
                return;

            SourceControl = dependencyResolver.Resolve<ISourceControl>();

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscription_NewData;
            DataSubscription.NewException += DataSubscription_NewException;
            DataSubscription.Requested += DataSubscription_Requested;
            DataSubscription.LoadingStatus += DataSubscription_LoadingStatus;

            IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(null);
            SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(null);
            ReportVm = ViewModelFactory.CreateViewModel<ReportViewModel>(null);
            GanttViewModel = ViewModelFactory.CreateViewModel<GanttViewModel>(null);

            SwitchSettingsCommand = new Command(SwitchSettings);
        }

        private void DataSubscription_LoadingStatus(object sender, string e)
        {
            LoadingStatus = e;
        }

        private void DataSubscription_Requested(object sender, EventArgs e)
        {
            OnProgress();
        }

        private void SwitchSettings()
        {
            IsSettingsOpen = !IsSettingsOpen;
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

            IsSettingsOpen = true;
        }

        private void DataSubscription_NewData(object sender, GitResponse e)
        {
            LoadingFinished();
        }

        private void LoadingFinished()
        {
            LaunchIsFinished = true;
            InProgress = false;
        }

        private void OnProgress()
        {
            LaunchIsFinished = false;
            InProgress = true;
        }

        private void Current_Exit(object sender, ExitEventArgs e) => CloseViewModelAsync(false);

        protected override Task CloseAsync()
        {
            MessageSubscription.NewMessage -= Notification_NewMessage;

            LifeTime.Cancel();
            LifeTime.Dispose();
            return base.CloseAsync();
        }

        protected override Task OnClosingAsync()
        {
            IssueListVm.CancelAndCloseViewModelAsync();
            return base.OnClosingAsync();
        }
        protected override async void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsSettingsOpen))
            {
                // Was Settings then close
                if (e.OldValue is true)
                {
                    if (ConnectionSettingsVm != null)
                    {
                        await ConnectionSettingsVm?.CloseViewModelAsync(false);
                        ConnectionSettingsVm = null;
                    }
                    if (LabelSettingsVm != null)
                    {
                        await LabelSettingsVm?.CloseViewModelAsync(false);
                        LabelSettingsVm = null;
                    }
                }

                if (e.NewValue is true)
                {
                    ConnectionSettingsVm = ViewModelFactory.CreateViewModel<ConnectionSettingsViewModel>(null);
                    ConnectionSettingsVm.SetOnClose(() =>
                    {
                        IsSettingsOpen = false;
                    });

                    var labels = await SourceControl.FetchGroupLabelsAsync().ConfigureAwait(false);
                    LabelSettingsVm = ViewModelFactory.CreateViewModel<LabelSettingsViewModel>(ConvertLabel(labels));
                    LabelSettingsVm.SetOnClose(() =>
                    {
                        IsSettingsOpen = false;
                    });

                    var users = await SourceControl.FetchAllUsersAsync().ConfigureAwait(false);
                    var usersArgument = new UsersArgument(users.Select(x => x.Name).ToList());
                    UserGroupsVm = ViewModelFactory.CreateViewModel<UserGroupsSettingsViewModel>(usersArgument);
                    UserGroupsVm.SetOnClose(() =>
                    {
                        IsSettingsOpen = false;
                    });
                }
            }
        }

        private static SettingsArgument ConvertLabel(IReadOnlyList<GroupLabel> labels)
        {
            return new()
            {
                Labels = labels
                    .Select(x => x.ConvertToLabel())
                    .ToList()
            };
        }
    }
}
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using GitLabTimeManager.Services;
using GitLabTimeManager.Types;
using GitLabTimeManager.ViewModel.Settings;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel;

public class MainViewModel : ViewModelBase
{
    public static MainViewModel CreateInstance()
    {
        return new MainViewModel();
    }

    private IViewModelFactory ViewModelFactory { get; }
    private IDataRequestService DataRequestService { get; }
    private IDataSubscription DataSubscription { get; }
    private INotificationMessageService MessageService { get; }
    private IMessageSubscription MessageSubscription { get; }
    private ISourceControl SourceControl { get; }

    private CancellationTokenSource LifeTime { get; } = new CancellationTokenSource();

    [UsedImplicitly] public static readonly IPropertyData IssueListVmProperty = RegisterProperty<MainViewModel, IssueListViewModel>(x => x.IssueListVm);
    [UsedImplicitly] public static readonly IPropertyData SummaryVmProperty = RegisterProperty<MainViewModel, SummaryViewModel>(x => x.SummaryVm);
    [UsedImplicitly] public static readonly IPropertyData IsFullscreenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsFullscreen);
    [UsedImplicitly] public static readonly IPropertyData ShowOnTaskBarProperty = RegisterProperty<MainViewModel, bool>(x => x.ShowOnTaskBar, true);
    [UsedImplicitly] public static readonly IPropertyData TodayVmProperty = RegisterProperty<MainViewModel, TodayViewModel>(x => x.TodayVm);
    [UsedImplicitly] public static readonly IPropertyData ReportVmProperty = RegisterProperty<MainViewModel, ReportViewModel>(x => x.ReportVm);
    [UsedImplicitly] public static readonly IPropertyData ErrorProperty = RegisterProperty<MainViewModel, string>(x => x.Error);
    [UsedImplicitly] public static readonly IPropertyData LaunchIsSuccessProperty = RegisterProperty<MainViewModel, bool>(x => x.LaunchIsFinished);
    [UsedImplicitly] public static readonly IPropertyData MessageProperty = RegisterProperty<MainViewModel, string>(x => x.Message);
    [UsedImplicitly] public static readonly IPropertyData IsMessageOpenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsMessageOpen);
    [UsedImplicitly] public static readonly IPropertyData IsSettingsOpenProperty = RegisterProperty<MainViewModel, bool>(x => x.IsSettingsOpen);
    [UsedImplicitly] public static readonly IPropertyData IsDefaultTabProperty = RegisterProperty<MainViewModel, bool>(x => x.IsDefaultTab);
    [UsedImplicitly] public static readonly IPropertyData ConnectionSettingsVmProperty = RegisterProperty<MainViewModel, ConnectionSettingsViewModel>(x => x.ConnectionSettingsVm);
    [UsedImplicitly] public static readonly IPropertyData LabelSettingsVmProperty = RegisterProperty<MainViewModel, LabelSettingsViewModel>(x => x.LabelSettingsVm);
    [UsedImplicitly] public static readonly IPropertyData GanttViewModelProperty = RegisterProperty<MainViewModel, GanttViewModel>(x => x.GanttViewModel);

    public GanttViewModel GanttViewModel
    {
        get => GetValue<GanttViewModel>(GanttViewModelProperty);
        set => SetValue(GanttViewModelProperty, value);
    }

    [CanBeNull]
    public LabelSettingsViewModel LabelSettingsVm
    {
        get => GetValue<LabelSettingsViewModel>(LabelSettingsVmProperty);
        private set => SetValue(LabelSettingsVmProperty, value);
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

    [CanBeNull]
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
        
    [ViewModelToModel][UsedImplicitly]
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
        private set => SetValue(ReportVmProperty, value);
    }

    [Model(SupportIEditableObject = false)][NotNull]
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

        IssueListVm = ViewModelFactory.CreateViewModel<IssueListViewModel>(null);
        SummaryVm = ViewModelFactory.CreateViewModel<SummaryViewModel>(null);
        TodayVm = ViewModelFactory.CreateViewModel<TodayViewModel>(null);
        ReportVm = ViewModelFactory.CreateViewModel<ReportViewModel>(null);
        GanttViewModel = ViewModelFactory.CreateViewModel<GanttViewModel>(null);

        SwitchSettingsCommand = new Command(SwitchSettings);
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

        IsFullscreen = true;
        IsSettingsOpen = true;
    }

    private void DataSubscription_NewData(object sender, GitResponse e)
    {
        LoadingFinished();
    }

    private void LoadingFinished()
    {
        LaunchIsFinished = true;
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
        
    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsSettingsOpen))
        {
            if (IsSettingsOpen)
            {
                var labels = SourceControl?.GetLabels();
                ConnectionSettingsVm = ViewModelFactory.CreateViewModel<ConnectionSettingsViewModel>(null);
                ConnectionSettingsVm?.SetOnClose(() => { IsSettingsOpen = false; });

                var argument = new SettingsArgument(labels: labels);
                LabelSettingsVm = ViewModelFactory.CreateViewModel<LabelSettingsViewModel>(argument);
                LabelSettingsVm?.SetOnClose(() => IsSettingsOpen = false);
            }
            else
            {
                if (ConnectionSettingsVm != null)
                {
                    await ConnectionSettingsVm.CloseViewModelAsync(false);
                    ConnectionSettingsVm = null;
                }

                if (LabelSettingsVm != null)
                {
                    await LabelSettingsVm.CloseViewModelAsync(false);
                    LabelSettingsVm = null;
                }
            }
        }
    }
}
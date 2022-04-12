using System.Diagnostics;
using System.Windows;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
using GitLabTimeManager.Services;
using GitLabTimeManager.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}

namespace GitLabTimeManager
{
    /// <summary> Interaction logic for App.xaml </summary>
    public partial class App
    {
        private TaskbarIcon _notifyIcon;
        private IViewModelFactory ViewModelFactory { get; set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            BuildContainer();

            InitTrayIcon();
#if DEBUG
            LogManager.AddDebugListener();
#endif
        }

        private void InitTrayIcon()
        {
            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();

            _notifyIcon = (TaskbarIcon)Current.FindResource("NotifyIcon");
            if (_notifyIcon == null) return;
            var viewModel = ViewModelFactory.CreateViewModel<TrayViewModel>(null);
            _notifyIcon.DataContext = viewModel;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();

            base.OnExit(e);
        }

        private void BuildContainer()
        {
            var serviceLocator = this.GetServiceLocator();
            serviceLocator.MissingType += ServiceLocator_MissingType;
#if !DEBUG
            //serviceLocator.RegisterTypeAndInstantiate<ExceptionWatcher>();
#endif
            var calendarService = new WorkingCalendar();
            var _ = calendarService.InitializeAsync();

            serviceLocator.RegisterType<ISourceControl, SourceControl>();
            serviceLocator.RegisterType<ILabelService, LabelProcessor>();
            serviceLocator.RegisterType<IMoneyCalculator, MoneyCalculator>(RegistrationType.Transient);
            serviceLocator.RegisterInstance<ICalendar>(calendarService);
            serviceLocator.RegisterType<IDataRequestService, DataRequestService>();
            serviceLocator.RegisterType<IProfileService, ProfileService>();
            serviceLocator.RegisterType<IUserProfile, UserProfile>();
            serviceLocator.RegisterType<INotificationMessageService, NotificationMessageService>();
            serviceLocator.RegisterType<IHttpService, HttpService>();
        }

        private static void ServiceLocator_MissingType(object sender, MissingTypeEventArgs e)
        {
            Debug.WriteLine(e.Tag);
        }
    }
}
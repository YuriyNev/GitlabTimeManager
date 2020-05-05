using System.Diagnostics;
using System.Windows;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
using GitLabTimeManager.Services;
using GitLabTimeManager.ViewModel;
using Hardcodet.Wpf.TaskbarNotification;

namespace GitLabTimeManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
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

            _notifyIcon = (TaskbarIcon) Current.FindResource("NotifyIcon");
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
            serviceLocator.RegisterTypeAndInstantiate<ExceptionWatcher>();
#endif
           ServiceLocator.Default.RegisterType<ITestService, TestService>();
           ServiceLocator.Default.RegisterType<ISourceControl, SourceControl>();
           ServiceLocator.Default.RegisterType<IMoneyCalculator, MoneyCalculator>();
           ServiceLocator.Default.RegisterType<ICalendar, WorkingCalendar>();
           ServiceLocator.Default.RegisterType<IDataRequestService, DataRequestService>();

        }

        
        private static void ServiceLocator_MissingType(object sender, MissingTypeEventArgs e)
        {
            Debug.WriteLine(e.Tag);
        }
    }

}

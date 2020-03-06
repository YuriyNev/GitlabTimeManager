using System.Windows;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
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

            var dependencyResolver = IoCConfiguration.DefaultDependencyResolver;
            ViewModelFactory = dependencyResolver.Resolve<IViewModelFactory>();
            
            _notifyIcon = (TaskbarIcon)Current.FindResource("NotifyIcon");
            if (_notifyIcon == null) return;
            var viewModel = ViewModelFactory.CreateViewModel<TrayViewModel>(null);

            _notifyIcon.DataContext = viewModel;

#if DEBUG
            LogManager.AddDebugListener();
#endif
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();

            base.OnExit(e);
        }
    }

}

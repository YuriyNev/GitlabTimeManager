using System.Windows;
using System.Windows.Input;
using Catel.MVVM;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel;

[UsedImplicitly]
internal class TrayViewModel : ViewModelBase
{
    public ICommand IconDoubleClickCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    public TrayViewModel()
    {
        IconDoubleClickCommand = new Command(RestoreWindow);
        ExitApplicationCommand = new Command(CloseApplication);
    }

    private static void CloseApplication()
    {
        Application.Current.Shutdown();
    }

    private static void RestoreWindow()
    {
        if (Application.Current.MainWindow != null) 
            Application.Current.MainWindow.WindowState = WindowState.Normal;
    }
}
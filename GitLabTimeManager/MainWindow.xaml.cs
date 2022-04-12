using GitLabTimeManager.ViewModel;

namespace GitLabTimeManager;

public partial class MainWindow 
{
    public MainWindow()
    {
        InitializeComponent();

        DataContext = MainViewModel.CreateInstance();
    }
}
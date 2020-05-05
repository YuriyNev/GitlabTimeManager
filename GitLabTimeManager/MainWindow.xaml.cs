using Catel.IoC;
using Catel.MVVM;
using GitLabTimeManager.ViewModel;

namespace GitLabTimeManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = MainViewModel.CreateInstance();
        }
    }
}

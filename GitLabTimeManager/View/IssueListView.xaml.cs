using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace GitLabTimeManager.View
{
    /// <summary>
    /// Interaction logic for View.xaml
    /// </summary>
    public partial class IssueListView 
    {
        public IssueListView()
        {
            InitializeComponent();
        }


        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            string navigateUri = (sender as Hyperlink)?.NavigateUri.ToString();
            //// if the URI somehow came from an untrusted source, make sure to
            //// validate it before calling Process.Start(), e.g. check to see
            //// the scheme is HTTP, etc.
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
    }
}

using System.Windows;

namespace GitLabTimeManager.View
{
    public partial class ReportView
    {
        public static readonly DependencyProperty IsSingleModeUserProperty = DependencyProperty.Register(
            "IsSingleModeUser", typeof(bool), typeof(ReportView), new PropertyMetadata(default(bool)));

        public bool IsSingleModeUser
        {
            get { return (bool) GetValue(IsSingleModeUserProperty); }
            set { SetValue(IsSingleModeUserProperty, value); }
        }

        public ReportView()
        {
            InitializeComponent();
        }
    }
}

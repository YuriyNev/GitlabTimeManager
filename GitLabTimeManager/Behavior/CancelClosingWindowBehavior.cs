using System.Windows;
using System.Windows.Interactivity;
using MahApps.Metro.Controls;

namespace GitLabTimeManager.Behavior
{
    public class CancelClosingWindowBehavior : Behavior<MetroWindow>
    {
        public static readonly DependencyProperty CanCloseWindowProperty =
            DependencyProperty.Register(nameof(CanCloseWindow), typeof(bool), typeof(CancelClosingWindowBehavior),
                new PropertyMetadata(true));

        public bool CanCloseWindow
        {
            get => (bool)GetValue(CanCloseWindowProperty);
            set => SetValue(CanCloseWindowProperty, value);
        }

        private void AssociatedObject_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CanCloseWindow)
            {
                AssociatedObject.WindowState = WindowState.Minimized;
                e.Cancel = true;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
            {
                AssociatedObject.Closing += AssociatedObject_Closing;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Closing -= AssociatedObject_Closing;
            }

            base.OnDetaching();
        }
    }
}

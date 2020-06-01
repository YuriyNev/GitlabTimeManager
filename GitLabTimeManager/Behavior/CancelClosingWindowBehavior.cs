using System;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors;

namespace GitLabTimeManager.Behavior
{
    public class CancelClosingWindowBehavior : Behavior<MetroWindow>
    {
        public static readonly DependencyProperty ShowOnTaskbarProperty =
            DependencyProperty.Register(nameof(ShowOnTaskbar), typeof(bool), typeof(CancelClosingWindowBehavior),
                new PropertyMetadata(true));

        public static readonly DependencyProperty CanCloseWindowProperty =
            DependencyProperty.Register(nameof(CanCloseWindow), typeof(bool), typeof(CancelClosingWindowBehavior),
                new PropertyMetadata(true));

        public bool CanCloseWindow
        {
            get => (bool)GetValue(CanCloseWindowProperty);
            set => SetValue(CanCloseWindowProperty, value);
        }
        
        public bool ShowOnTaskbar
        {
            get => (bool)GetValue(ShowOnTaskbarProperty);
            set => SetValue(ShowOnTaskbarProperty, value);
        }

        private void AssociatedObject_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CanCloseWindow)
            {
                AssociatedObject.WindowState = WindowState.Minimized;
                ShowOnTaskbar = false;
                e.Cancel = true;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject != null)
            {
                AssociatedObject.Closing += AssociatedObject_Closing;
                AssociatedObject.StateChanged += AssociatedObject_StateChanged;
            }
        }

        private void AssociatedObject_StateChanged(object sender, EventArgs e)
        {
            if (sender is MainWindow window)
            {
                if (window.WindowState != WindowState.Minimized)
                {
                    ShowOnTaskbar = true;
                }
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.StateChanged -= AssociatedObject_StateChanged;
                AssociatedObject.Closing -= AssociatedObject_Closing;
            }

            base.OnDetaching();
        }
    }
}

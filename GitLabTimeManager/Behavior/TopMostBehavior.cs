using System.Windows;
using System.Windows.Interactivity;
using MahApps.Metro.Controls;

namespace GitLabTimeManager.Behavior
{
    public class TopmostBindingBehavior : Behavior<MetroWindow>
    {
        public static readonly DependencyProperty IsTopmostProperty =
            DependencyProperty.Register(nameof(IsTopmost), typeof(bool), typeof(TopmostBindingBehavior),
                new PropertyMetadata(false, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var b = (TopmostBindingBehavior)d;
            if (b.AssociatedObject != null)
                b.AssociatedObject.Topmost = (bool)e.NewValue;
        }

        public bool IsTopmost
        {
            get => (bool)GetValue(IsTopmostProperty);
            set => SetValue(IsTopmostProperty, value);
        }

    }
}

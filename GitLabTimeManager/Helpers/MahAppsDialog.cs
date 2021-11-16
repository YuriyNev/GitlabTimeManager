using System;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using UserControl = Catel.Windows.Controls.UserControl;

namespace NetVideo.UICommon
{
    public sealed class MahAppsDialog
    {
        public static readonly DependencyProperty ParticipateProperty =
            DependencyProperty.RegisterAttached("Participate", typeof(bool), typeof(MahAppsDialog),
                new PropertyMetadata(default(bool), OnParticipateChanged));

        public static void SetParticipate(DependencyObject element, bool value) => element.SetValue(ParticipateProperty, value);
        public static bool GetParticipate(DependencyObject element) => (bool)element.GetValue(ParticipateProperty);

        private static void OnParticipateChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (GetParticipate(element))
            {
                SetParticipation(element);

                //Отписываемся от всех событий, чтоб не подписаться на них дважды
                Unsubscribe(element);

                Subscribe(element);
            }
            else
            {
                Unsubscribe(element);

                RemoveParticipation(element);
            }
        }

        private static void Subscribe(DependencyObject element)
        {
            if (element is UserControl userControl)
            {
                userControl.ViewModelChanged += UserControl_ViewModelChanged;
            }
            else if (element is FrameworkElement fe)
            {
                fe.DataContextChanged += FrameworkElement_DataContextChanged;
            }

            if (element is FrameworkElement fe1)
            {
                fe1.Unloaded += FrameworkElement_Unloaded;
                fe1.Loaded += FrameworkElement_Loaded;
            }
        }

        private static void Unsubscribe(DependencyObject element)
        {
            if (element is UserControl userControl)
            {
                userControl.ViewModelChanged -= UserControl_ViewModelChanged;
            }
            else if (element is FrameworkElement fe)
            {
                fe.DataContextChanged -= FrameworkElement_DataContextChanged;
            }

            if (element is FrameworkElement fe1)
            {
                fe1.Loaded -= FrameworkElement_Loaded;
                fe1.Unloaded -= FrameworkElement_Unloaded;
            }
        }

        private static void SetParticipation(DependencyObject element)
        {
            if (element is UserControl userControl)
            {
                DialogParticipation.SetRegister(userControl, userControl.ViewModel);
            }
            else if (element is FrameworkElement fe)
            {
                DialogParticipation.SetRegister(fe, fe.DataContext);
            }
            else
            {
                DialogParticipation.SetRegister(element, null);
            }
        }

        private static void RemoveParticipation(DependencyObject element)
        {
            DialogParticipation.SetRegister(element, null);
        }

        private static void FrameworkElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject depObj)
                OnParticipateChanged(depObj, new DependencyPropertyChangedEventArgs());
        }

        private static void FrameworkElement_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject depObj)
                OnParticipateChanged(depObj, new DependencyPropertyChangedEventArgs());
        }

        private static void FrameworkElement_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetParticipation(sender as DependencyObject);
        }

        private static void UserControl_ViewModelChanged(object sender, EventArgs e)
        {
            SetParticipation(sender as DependencyObject);
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace GitLabTimeManager.Behavior
{
    public class MonthCalendarBehavior : Behavior<Calendar>
    {
        protected override void OnAttached()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.DisplayModeChanged += AssociatedObjectOnDisplayModeChanged;
                AssociatedObject.DisplayMode = CalendarMode.Year;
            }

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null) AssociatedObject.DisplayModeChanged -= AssociatedObjectOnDisplayModeChanged;

            base.OnDetaching();
        }

        private static void AssociatedObjectOnDisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            if (sender is Calendar calendar)
                calendar.DisplayMode = CalendarMode.Year;
        }

    }
}

using System.Windows.Documents;
using System.Windows.Navigation;
using GitLabTimeManager.Helpers;
using Microsoft.Xaml.Behaviors;

namespace GitLabTimeManager.Behavior;

public class HyperLinkBehavior : Behavior<Hyperlink>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.RequestNavigate += AssociatedObject_RequestNavigate;
    }

    private static void AssociatedObject_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var uri = (sender as Hyperlink)?.NavigateUri;
            
        uri.GoToBrowser();
            
        e.Handled = true;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.RequestNavigate -= AssociatedObject_RequestNavigate;

        base.OnDetaching();
    }
}
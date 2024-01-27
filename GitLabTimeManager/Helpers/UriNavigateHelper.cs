using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace GitLabTimeManager.Helpers;

public static class UriNavigateHelper
{
    public static void GoToBrowser([CanBeNull] this Uri uri)
    {
        if (uri == null)
            return;
            
        var navigateUri = uri.ToString();

        Process.Start(new ProcessStartInfo(navigateUri)
        {
            UseShellExecute = true,
        });
    }
}
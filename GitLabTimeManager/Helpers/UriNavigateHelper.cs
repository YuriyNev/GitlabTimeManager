using System;
using System.Diagnostics;

namespace GitLabTimeManager.Helpers;

public static class UriNavigateHelper
{
    public static void GoToBrowser(this Uri? uri)
    {
        if (uri == null)
            return;
            
        var navigateUri = uri.ToString();
        //// if the URI somehow came from an untrusted source, make sure to
        //// validate it before calling Process.Start(), e.g. check to see
        //// the scheme is HTTP, etc.
        Process.Start(new ProcessStartInfo(navigateUri));
    }
}
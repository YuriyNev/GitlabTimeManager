using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace GitLabTimeManager.Helpers
{
    public static class UriNavigateHelper
    {
        public static void GoToBrowser([CanBeNull] this Uri uri)
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
}

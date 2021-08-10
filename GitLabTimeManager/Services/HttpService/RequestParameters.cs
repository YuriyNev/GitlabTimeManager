using System;

namespace GitLabTimeManager.Services
{
    public class RequestParameters
    {
        public Uri ServerAddress { get; set; }
        public string Token { get; set; }
        public TimeSpan Timeout { get; set; }
    }
}
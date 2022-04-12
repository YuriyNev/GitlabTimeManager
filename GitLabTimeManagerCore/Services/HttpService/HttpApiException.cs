using System;

namespace GitLabTimeManager.Services
{
    public abstract class HttpApiException : Exception
    {
        public HttpApiException()
        {

        }

        public HttpApiException(string message)
            : base(message)
        {
        }
    }
}
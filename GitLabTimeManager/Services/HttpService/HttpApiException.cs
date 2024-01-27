using System;

namespace GitLabTimeManager.Services;

public abstract class HttpApiException : Exception
{
    protected HttpApiException()
    {
    }

    protected HttpApiException(string message)
        : base(message)
    {
    }
}
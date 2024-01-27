using System.Net;

namespace GitLabTimeManager.Services;

public sealed class HttpSendErrorException(HttpStatusCode statusCode) : HttpApiException
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
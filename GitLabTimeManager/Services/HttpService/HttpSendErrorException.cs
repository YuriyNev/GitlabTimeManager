using System.Net;

namespace GitLabTimeManager.Services
{
    public sealed class HttpSendErrorException : HttpApiException
    {
        public HttpStatusCode StatusCode { get; }

        public HttpSendErrorException(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpSendErrorException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace GitLabTimeManager.Services
{
    public static class HttpMethods
    {
        public static HttpClient HttpClient { get; } = new(
            new TimeoutHandler { InnerHandler = new HttpClientHandler() })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private static HttpContent JsonContent(object value, JsonSerializerSettings jsonSerializerSettings)
        {
            var serialized = JsonConvert.SerializeObject(value, jsonSerializerSettings);
            return new StringContent(serialized, Encoding.UTF8, MediaTypes.ApplicationJson);
        }

        public static async Task<string> HttpGetStringAsync(Uri address, TimeSpan? timeout, string token, CancellationToken cancellationToken)
        {
            using (var response = await HttpSendAsync(address, HttpMethod.Get,
                    HttpCompletionOption.ResponseContentRead, null, token, timeout, cancellationToken)
                .ConfigureAwait(false))
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public static async Task<bool> HttpPostStringAsync(Uri address, TimeSpan timeOut, HttpContent content, string token, CancellationToken cancellationToken)
        {
            using (var responseMessage = await HttpSendAsync(address, HttpMethod.Post,
                HttpCompletionOption.ResponseHeadersRead, content, token, timeOut, cancellationToken).ConfigureAwait(false))
            {
                return responseMessage.IsSuccessStatusCode;
            }
        }

        public static async Task<bool> HttpPutStringAsync(Uri address, TimeSpan timeOut, HttpContent content, string token, CancellationToken cancellationToken)
        {
            using (var responseMessage = await HttpSendAsync(address, HttpMethod.Put,
                HttpCompletionOption.ResponseHeadersRead, content, token, timeOut, cancellationToken).ConfigureAwait(false))
            {
                return responseMessage.IsSuccessStatusCode;
            }
        }

        public static async Task<bool> HttpDeleteAsync(Uri address, TimeSpan timeOut, string token, CancellationToken cancellationToken)
        {
            using (var responseMessage = await HttpSendAsync(address, HttpMethod.Delete,
                HttpCompletionOption.ResponseHeadersRead, null, token, timeOut, cancellationToken).ConfigureAwait(false))
            {
                return responseMessage.IsSuccessStatusCode;
            }
        }

        public static async Task<bool> HttpDeleteAsync(string route, RequestParameters parameters, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            using (var responseMessage = await HttpSendAsync(fullAddress, HttpMethod.Delete,
                HttpCompletionOption.ResponseHeadersRead, null, parameters.Token, parameters.Timeout, cancellationToken).ConfigureAwait(false))
            {
                return responseMessage.IsSuccessStatusCode;
            }
        }

        private static T DeserializeJsonFromStream<T>(Stream stream, JsonSerializerSettings settings = null)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                var js = JsonSerializer.CreateDefault(settings);
                var searchResult = js.Deserialize<T>(jtr);
                return searchResult;
            }
        }

        public static Task<TResponse> JsonGetAsync<TResponse>(string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            return JsonGetAsync<TResponse>(fullAddress, parameters.Timeout, parameters.Token, settings, cancellationToken);
        }

        private static async Task<TResponse> JsonGetAsync<TResponse>(Uri uri, TimeSpan? timeout, string token, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            using (var responseMessage = await HttpSendAsync(uri, HttpMethod.Get, HttpCompletionOption.ResponseContentRead, null, token, timeout, cancellationToken).ConfigureAwait(false))
            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                TResponse result = DeserializeJsonFromStream<TResponse>(responseStream, settings);
                return result;
            }
        }

        public static Task<TResponse> JsonGetAsync<TRequest, TResponse>(TRequest request, string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            return JsonGetAsync<TRequest, TResponse>(request, fullAddress, parameters.Timeout, parameters.Token, settings, cancellationToken);
        }

        private static async Task<TResponse> JsonGetAsync<TRequest, TResponse>(TRequest request, Uri uri, TimeSpan? timeout, string token, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            using (var content = JsonContent(request, settings))
            using (var responseMessage = await HttpSendAsync(uri, HttpMethod.Get, HttpCompletionOption.ResponseContentRead, content, token, timeout, cancellationToken).ConfigureAwait(false))
            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                TResponse result = DeserializeJsonFromStream<TResponse>(responseStream, settings);
                return result;
            }
        }

        public static Task<TResponse> JsonPostAsync<TRequest, TResponse>(TRequest request, string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            return JsonPostAsync<TRequest, TResponse>(request, fullAddress, parameters.Timeout, parameters.Token, settings, cancellationToken);
        }

        private static async Task<TResponse> JsonPostAsync<TRequest, TResponse>(TRequest request, Uri uri, TimeSpan timeout, string token, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            using (var content = JsonContent(request, settings))
            using (var responseMessage = await HttpSendAsync(uri, HttpMethod.Post, HttpCompletionOption.ResponseContentRead, content, token, timeout, cancellationToken).ConfigureAwait(false))
            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                TResponse result = DeserializeJsonFromStream<TResponse>(responseStream, settings);
                return result;
            }
        }

        public static Task<bool> JsonPostAsync<TRequest>(TRequest request, string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            return JsonPostAsync(request, fullAddress, parameters.Timeout, parameters.Token, settings, cancellationToken);
        }

        private static async Task<bool> JsonPostAsync<TRequest>(TRequest request, Uri uri, TimeSpan timeout, string token, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            using (var content = JsonContent(request, settings))
            using (var responseMessage = await HttpSendAsync(uri, HttpMethod.Post, HttpCompletionOption.ResponseHeadersRead, content, token, timeout, cancellationToken).ConfigureAwait(false))
            {
                return responseMessage.IsSuccessStatusCode;
            }
        }

        public static async Task<TResponse> JsonPostWithThrowAsync<TRequest, TResponse>(TRequest request, string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);

            try
            {
                using (var jsonContent = JsonContent(request, settings))
                using (var requestMessage = GetRequestMessage(fullAddress, parameters.Token, HttpMethod.Post, jsonContent, parameters.Timeout))
                using (var responseMessage = await HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var response = JsonConvert.DeserializeObject<TResponse>(content, settings);

                        return response;
                    }

                    if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
                        throw new Exception("Ошибка авторизации");

                    ThrowForStatusCode(responseMessage.StatusCode);

                    return default;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public static Task<TResponse> JsonPutAsync<TRequest, TResponse>(TRequest request, string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            return JsonPutAsync<TRequest, TResponse>(request, fullAddress, parameters.Timeout, parameters.Token, settings, cancellationToken);
        }

        private static async Task<TResponse> JsonPutAsync<TRequest, TResponse>(TRequest request, Uri uri, TimeSpan timeout, string token, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            using (var content = JsonContent(request, settings))
            using (var responseMessage = await HttpSendAsync(uri, HttpMethod.Put, HttpCompletionOption.ResponseContentRead, content, token, timeout, cancellationToken).ConfigureAwait(false))
            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                TResponse result = DeserializeJsonFromStream<TResponse>(responseStream, settings);
                return result;
            }
        }

        public static Task<bool> JsonPutAsync<TRequest>(TRequest request, string route, RequestParameters parameters, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            var fullAddress = new Uri(parameters.ServerAddress, route);
            return JsonPutAsync(request, fullAddress, parameters.Timeout, parameters.Token, settings, cancellationToken);
        }

        private static async Task<bool> JsonPutAsync<TRequest>(TRequest request, Uri uri, TimeSpan timeout, string token, JsonSerializerSettings settings, CancellationToken cancellationToken)
        {
            using (var content = JsonContent(request, settings))
            using (var responseMessage = await HttpSendAsync(uri, HttpMethod.Put, HttpCompletionOption.ResponseHeadersRead, content, token, timeout, cancellationToken).ConfigureAwait(false))
            {
                return responseMessage.IsSuccessStatusCode;
            }
        }

        private static async Task<HttpResponseMessage> HttpSendAsync(Uri address,
            HttpMethod httpMethod,
            HttpCompletionOption httpCompletionOption,
            HttpContent content,
            string token,
            TimeSpan? timeout,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var httpRequestMessage = GetRequestMessage(address, token, httpMethod, content, timeout))
                {
                    var result = await HttpClient.SendAsync(httpRequestMessage, httpCompletionOption, cancellationToken).ConfigureAwait(false);

                    ThrowForStatusCode(result.StatusCode);

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static HttpRequestMessage GetRequestMessage(Uri address, string token, HttpMethod httpMethod, HttpContent content, TimeSpan? timeout)
        {
            var msg = new HttpRequestMessage(httpMethod, address) { Content = content };
            msg.SetTimeout(timeout);
            if (token != null)
                msg.Headers.Authorization = token.GetAuthenticationHeaderValue();
            return msg;
        }

        private static void ThrowForStatusCode(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Ambiguous:
                case HttpStatusCode.BadGateway:
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Conflict:
                case HttpStatusCode.Continue:
                case HttpStatusCode.ExpectationFailed:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.Found:
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.Gone:
                case HttpStatusCode.HttpVersionNotSupported:
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.LengthRequired:
                case HttpStatusCode.MethodNotAllowed:
                case HttpStatusCode.Moved:
                case HttpStatusCode.NotAcceptable:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.NotImplemented:
                case HttpStatusCode.NotModified:
                case HttpStatusCode.PaymentRequired:
                case HttpStatusCode.PreconditionFailed:
                case HttpStatusCode.ProxyAuthenticationRequired:
                case HttpStatusCode.RedirectKeepVerb:
                case HttpStatusCode.RedirectMethod:
                case HttpStatusCode.RequestedRangeNotSatisfiable:
                case HttpStatusCode.RequestEntityTooLarge:
                case HttpStatusCode.RequestTimeout:
                case HttpStatusCode.RequestUriTooLong:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.SwitchingProtocols:
                case HttpStatusCode.UnsupportedMediaType:
                case HttpStatusCode.Unused:
                //case HttpStatusCode.UpgradeRequired:
                case HttpStatusCode.UseProxy:
                    throw new HttpSendErrorException(statusCode);
            }
        }
    }
}
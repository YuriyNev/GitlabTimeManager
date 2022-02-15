using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitLabApiClient.Models.Users.Responses;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public class HttpService : IHttpService
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings = SdkJsonSettings.Create();

        private const string LabelEventsRoutePrefix = "api/v4/";
        private JsonSerializerSettings JsonSerializerSettings { get; }
        private RequestParameters Parameters { get; }

        public HttpService([NotNull] IUserProfile userProfile)
        {
            if (userProfile == null) throw new ArgumentNullException(nameof(userProfile));

            Parameters = new RequestParameters
            {
                Token = userProfile.Token,
                ServerAddress = new Uri(userProfile.Url),
                Timeout = TimeSpan.FromSeconds(5),
            };

            JsonSerializerSettings = _jsonSerializerSettings;
        }

        public async Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync(LabelEventsRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string route = $"{LabelEventsRoutePrefix}projects/{request.ProjectId}/issues/{request.IssueIid}/resource_label_events?per_page=100";

            return await HttpMethods.JsonGetAsync<List<LabelEvent>>(route, Parameters, JsonSerializerSettings, cancellationToken).ConfigureAwait(false);
        }
    }
}

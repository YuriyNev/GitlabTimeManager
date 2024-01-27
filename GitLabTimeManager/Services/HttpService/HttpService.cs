using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace GitLabTimeManager.Services;

public class HttpService : IHttpService
{
    private readonly JsonSerializerSettings _jsonSerializerSettings = SdkJsonSettings.Create();

    private const string LabelEventsRoutePrefix = "api/v4/";
    private JsonSerializerSettings JsonSerializerSettings { get; }
    private RequestParameters Parameters { get; }

    public HttpService([JetBrains.Annotations.NotNull] IUserProfile userProfile)
    {
        if (userProfile == null) throw new ArgumentNullException(nameof(userProfile));

        Parameters = new RequestParameters
        {
            Token = userProfile.Token,
            ServerAddress = ResolveUri(userProfile.Url),
            Timeout = TimeSpan.FromSeconds(5),
        };

        JsonSerializerSettings = _jsonSerializerSettings;
    }

    private static Uri ResolveUri(string textUri)
    {
        if (TryCreateUrl(textUri, out var uri1))
            return uri1;
        
        if (TryCreateUrl($"http://{textUri}", out var uri2))
            return uri2;
        
        if (TryCreateUrl($"https://{textUri}", out var uri3))
            return uri3;

        throw new Exception($"Invalid url: {textUri}!");
    }

    private static bool TryCreateUrl(string textUri, [MaybeNullWhen(false)] out Uri uri)
    {
        try
        {
            uri = new Uri(textUri);
            return true;
        }
        catch
        {
            uri = null;
            return false;
        }
    }

    public async Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync(LabelEventsRequest request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        string route = $"{LabelEventsRoutePrefix}projects/{request.ProjectId}/issues/{request.IssueIid}/resource_label_events";

        return await HttpMethods.JsonGetAsync<List<LabelEvent>>(route, Parameters, JsonSerializerSettings, cancellationToken).ConfigureAwait(false);
    }
}
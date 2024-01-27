using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services;

public interface IHttpService
{
    Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync([NotNull] LabelEventsRequest request, CancellationToken cancellationToken);
}
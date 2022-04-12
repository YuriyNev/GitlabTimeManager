
namespace GitLabTimeManager.Services
{
    public interface IHttpService
    {
        Task<IReadOnlyList<LabelEvent>> GetLabelsEventsAsync(LabelEventsRequest request, CancellationToken cancellationToken);
    }
}
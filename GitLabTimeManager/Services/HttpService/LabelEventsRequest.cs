namespace GitLabTimeManager.Services;

public class LabelEventsRequest
{
    public int ProjectId { get; set; }

    public int IssueIid { get; set; }
}
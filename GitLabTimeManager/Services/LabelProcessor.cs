using System;
using System.Collections.Generic;
using System.Linq;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Projects.Responses;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services;

public interface ILabelService
{
    IReadOnlyList<string> StartIssue(IReadOnlyList<string> labels);

    IReadOnlyList<string> FinishIssue(IReadOnlyList<string> labels);

    IReadOnlyList<string> PauseIssue(IReadOnlyList<string> labels);

    bool IsStarted(List<string> labels);

    bool IsPaused(List<string> labels);

    IReadOnlyList<Label> FilterLabels(IReadOnlyList<Label> all, IList<string> source);

    bool ContainsExcludeLabels(IReadOnlyList<Label> labels);

    bool ContainsBoardLabels(IReadOnlyList<Label> labels);

}

public class LabelProcessor : ILabelService, IDisposable
{
    [NotNull] private IUserProfile UserProfile { get; }
    [NotNull] private IProfileService ProfileService { get; }

    private string ToDoLabel { get; set; }
    private string DoingLabel { get; set; }
    private string DoneLabel { get; set; }

    private IReadOnlyList<string> AllBoardLabels { get; set; }
    private IReadOnlyList<string> ExcludeLabels { get; set; }
    private BoardStateLabels BoardStateLabels { get; set; }

    public LabelProcessor(
        [NotNull] IUserProfile userProfile,
        [NotNull] IProfileService profileService)
    {
        UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
        ProfileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        Update(UserProfile.LabelSettings);

        ProfileService.Serialized += ProfileService_Serialized;
    }

    private void ProfileService_Serialized(object sender, IUserProfile e)
    {
        Update(UserProfile.LabelSettings);
    }

    private void Update(LabelSettings settings)
    {
        AllBoardLabels = settings.AllBoardLabels;
        ExcludeLabels = settings.ExcludeLabels;
        BoardStateLabels = settings.BoardStateLabels;

        ToDoLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.ToDoLabel);
        DoingLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.DoingLabel);
        DoneLabel = AllBoardLabels.FirstOrDefault(x => x == BoardStateLabels.DoneLabel);

        ExcludeLabels = settings.ExcludeLabels;
    }

    public IReadOnlyList<string> StartIssue(IReadOnlyList<string> labels)
    {
        return SwitchBoardLabel(labels, DoingLabel);
    }

    public IReadOnlyList<string> FinishIssue(IReadOnlyList<string> labels)
    {
        return SwitchBoardLabel(labels, DoneLabel);
    }

    public IReadOnlyList<string> PauseIssue(IReadOnlyList<string> labels)
    {
        return SwitchBoardLabel(labels, ToDoLabel);
    }

    private IReadOnlyList<string> SwitchBoardLabel(IReadOnlyList<string> labels, string selectedLabel)
    {
        var newLabels = new List<string>(labels);

        foreach (var label in AllBoardLabels)
        {
            if (label != selectedLabel)
                newLabels.Remove(label);
        }

        newLabels.Add(selectedLabel);

        return newLabels;
    }

    public bool IsStarted(List<string> labels) => labels.Contains(DoingLabel) || labels.Contains(DoneLabel);

    public bool IsPaused(List<string> labels) => !labels.Contains(DoingLabel);

    public IReadOnlyList<Label> FilterLabels(IReadOnlyList<Label> all, IList<string> source) => all.Where(x => source.Contains(x.Name)).ToList();

    public bool ContainsExcludeLabels(IReadOnlyList<Label> labels)
    {
        return labels.Any(l => ExcludeLabels.Contains(l.Name));
    }

    public bool ContainsBoardLabels(IReadOnlyList<Label> labels)
    {
        return labels.Any(l => AllBoardLabels.Contains(l.Name));
    }

    public void Dispose()
    {
        ProfileService.Serialized -= ProfileService_Serialized;
    }
}

public static class LabelHelper
{
    public static bool IsStarted(this Issue issue, ILabelService labelService) => labelService.IsStarted(issue.Labels);
    public static bool IsPaused(this Issue issue, ILabelService labelService) => labelService.IsPaused(issue.Labels);
}
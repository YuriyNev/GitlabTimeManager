﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Catel.Collections;
using GitLabApiClient;
using GitLabApiClient.Models;
using GitLabApiClient.Models.Issues.Requests;
using GitLabApiClient.Models.Issues.Responses;
using GitLabApiClient.Models.Notes.Requests;
using GitLabApiClient.Models.Notes.Responses;
using GitLabTimeManager.Helpers;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public interface ISourceControl 
    {
        [PublicAPI] Task<GitResponse> RequestDataAsync();
        [PublicAPI] Task AddSpendAsync(Issue issue, TimeSpan timeSpan);
        [PublicAPI] Task<bool> StartIssueAsync(Issue issue);
        [PublicAPI] Task<bool> PauseIssueAsync(Issue issue);
        [PublicAPI] Task<bool> FinishIssueAsync(Issue issue);
    }

    internal class SourceControl : ISourceControl
    {
#if DEBUG
        //private static readonly IReadOnlyList<int> ProjectIds = new List<int> { 17053052 };
        //private const string Token = "KajKr2cVJ4amosry9p4v";
        //private const string Uri = "https://gitlab.com";

        private static int ClientDominationId = 14;
        private static int AnalyticsServerId = 16;


        private static readonly IReadOnlyList<int> ProjectIds = new List<int>
        {
            ClientDominationId, AnalyticsServerId
        };
        private const string Token = "gTUPn2KdhEFUMR3oQL81";
        private const string Uri = "http://gitlab.domination";
#else
        private static int ClientDominationId = 14;
        private static int AnalyticsServerId = 16;

        private static readonly IReadOnlyList<int> ProjectIds = new List<int>
        {
            ClientDominationId, AnalyticsServerId
        };
        private const string Token = "gTUPn2KdhEFUMR3oQL81";
        private const string Uri = "http://gitlab.domination";
#endif
        // dont calc
        private static readonly IReadOnlyList<LabelEx> ExcludeLabels = new List<LabelEx>
        {
            LabelsCollection.ProjectControlLabel
        };

        private static DateTime Today => DateTime.Today;
        private static DateTime MonthStart => Today.AddDays(-Today.Day).AddDays(1);
        private static DateTime MonthEnd => MonthStart.AddMonths(1);


        private ObservableCollection<WrappedIssue> WrappedIssues { get; set; } = new ObservableCollection<WrappedIssue>();

        private GitLabClient GitLabClient { get; }

        public SourceControl()
        {
            GitLabClient = new GitLabClient(Uri, Token);
        }

        public async Task<GitResponse> RequestDataAsync()
        {
            await ComputeStatisticsAsync().ConfigureAwait(true);
            var response = new GitResponse
            {
                StartDate = MonthStart,
                EndDate = MonthEnd,
                
                WrappedIssues = WrappedIssues,

            };
            return response;
        }

        public async Task AddSpendAsync(Issue issue, TimeSpan timeSpan)
        {
#if DEBUG
            if (timeSpan < TimeSpan.FromSeconds(10)) return;
#else
            if (timeSpan < TimeSpan.FromMinutes(5)) return;
#endif
            var request = new CreateIssueNoteRequest(timeSpan.ConvertSpent() + "\n" + "[]");
            var note = await GitLabClient.Issues.CreateNoteAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            await GitLabClient.Issues.DeleteNoteAsync(issue.ProjectId, issue.Iid, note.Id).ConfigureAwait(false);
        }

        public async Task<bool> StartIssueAsync(Issue issue)
        {
            if (LabelProcessor.IsStarted(issue.Labels)) 
                return true;

            LabelProcessor.StartIssue(issue.Labels);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };

            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }
        
        public async Task<bool> PauseIssueAsync(Issue issue)
        {
            if (LabelProcessor.IsPaused(issue.Labels))
                return true;

            LabelProcessor.PauseIssue(issue.Labels);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> FinishIssueAsync(Issue issue)
        {
            LabelProcessor.FinishIssue(issue.Labels);

            var request = new UpdateIssueRequest
            {
                Labels = issue.Labels
            };
            await GitLabClient.Issues.UpdateAsync(issue.ProjectId, issue.Iid, request).ConfigureAwait(false);
            return true;
        }

        private async Task ComputeStatisticsAsync()
        {
            var allIssues = await RequestAllIssuesAsync().ConfigureAwait(false);
            var allNotes = await GetNotesAsync(allIssues).ConfigureAwait(false);

            WrappedIssues = ExtentIssues(allIssues, allNotes, MonthStart, MonthEnd);

            foreach (var wrappedIssue in WrappedIssues) Debug.WriteLine(wrappedIssue);
        }


        private static DateTime? FinishTime(Issue issue) => issue.ClosedAt;

        private static ObservableCollection<WrappedIssue> ExtentIssues(IEnumerable<Issue> sourceIssues, IReadOnlyDictionary<Issue, IList<Note>> notes,
            DateTime monthStart, DateTime monthEnd)
        {
            var issues = new ObservableCollection<WrappedIssue>();
            var labelsEx = new ObservableCollection<LabelEx>();
            foreach (var issue in sourceIssues)
            {
                notes.TryGetValue(issue, out var note);

                DateTime? startDate = null;
                var startedIn = false;
                double spendIn = 0;
                double spendBefore = 0;

                double totalSpend = TimeHelper.SecondsToHours(issue.TimeStats.TotalTimeSpent);
                if (note != null && note.Count > 0)
                {
                    // if more 0 notes then getting data from notes
                    var timeNotes = note.
                        Where(x => x.Body.ParseSpent() > 0 || x.Body.ParseEstimate() > 0).
                        ToList();

                    if (timeNotes.Count > 0)
                        startDate = timeNotes.Min(x => x.CreatedAt);
                    startedIn = !timeNotes.
                        Any(x => x.CreatedAt < monthStart);

                    spendIn = CollectSpendTime(timeNotes, monthStart, monthEnd).TotalHours;
                    spendBefore = CollectSpendTime(timeNotes, DateTime.MinValue, monthStart).TotalHours;
                }
                
                // spend is set when issue was created
                var startSpend = totalSpend - (spendIn + spendBefore);
                if (startSpend > 0)
                {
                    if (startedIn)
                        spendIn += startSpend;
                    else
                        spendBefore += startSpend;
                }

                LabelProcessor.UpdateLabelsEx(labelsEx, issue.Labels);

                IReadOnlyList<Note> noteList = Array.Empty<Note>();
                if (notes.TryGetValue(issue, out var list)) noteList = list.ToList();

                var extIssue = new WrappedIssue
                {
                    Issue = issue,
                    StartTime = startDate,
                    EndTime = FinishTime(issue),
                    SpendIn = spendIn,
                    SpendBefore = spendBefore,
                    StartedIn = startedIn,
                    LabelExes = new ObservableCollection<LabelEx>(labelsEx),
                    Notes = noteList
                };
                
                issues.Add(extIssue);
            }
            return issues;
        }

        private async Task<Dictionary<Issue, IList<Note>>> GetNotesAsync(IEnumerable<Issue> issues)
        {
            var dict = new Dictionary<Issue, IList<Note>>();
            foreach (var issue in issues)
            {
                var notes = await GetNotesAsync(Convert.ToInt32(issue.ProjectId), issue.Iid).ConfigureAwait(false);
                dict.Add(issue, notes);
            }

            return dict;
        }

        private async Task<ObservableCollection<Issue>> RequestAllIssuesAsync()
        {
            var allIssues = new ObservableCollection<Issue>();
            foreach (var projectId in ProjectIds)
            {
                var issues = await GitLabClient.Issues.GetAsync(projectId, options =>
                {
                    options.Scope = Scope.AssignedToMe;
                    options.CreatedAfter = Today.AddMonths(-6);
                    options.State = IssueState.All;
                }).ConfigureAwait(false);

                issues = issues.Where(x => x.State == IssueState.Closed && 
                                           x.ClosedAt != null && 
                                           x.ClosedAt > MonthStart && 
                                           x.ClosedAt < MonthEnd || 
                                           x.State == IssueState.Opened).ToList();

                allIssues.AddRange(issues);
            }

            return allIssues;
        }

        private static TimeSpan CollectSpendTime(IEnumerable<Note> notes, DateTime? startTime = null, DateTime? endTime = null)
        {
            var start = startTime ?? DateTime.MinValue;
            var end = endTime ?? DateTime.MaxValue;

            var hoursList = notes
                .Where(x => x.CreatedAt > start && x.CreatedAt < end)
                .Select(x => x.Body.ParseSpent());

            var hours = hoursList.Sum();
            return TimeSpan.FromHours(hours);
        }

        private async Task<IList<Note>> GetNotesAsync(int projectId, int issueId)
        {
            var notes = await GitLabClient.Issues.GetNotesAsync(projectId, issueId).ConfigureAwait(false);
            return notes;
        }

        public void Dispose()
        {
        }
    }

    public class GitResponse
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
       
        public ObservableCollection<WrappedIssue> WrappedIssues { get; set; }
    }
}

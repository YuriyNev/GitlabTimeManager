using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Catel.Data;
using Catel.MVVM;
using GitLabApiClient.Models.Issues.Requests;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel;

[UsedImplicitly]
public class IssueTimerViewModel : ViewModelBase
{
    [UsedImplicitly] public static readonly IPropertyData TimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.Time);
    [UsedImplicitly] public static readonly IPropertyData TotalTimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.EstimateTime);
    [UsedImplicitly] public static readonly IPropertyData DescriptionProperty = RegisterProperty<IssueTimerViewModel, string>(x => x.IssueTitle);
    [UsedImplicitly] public static readonly IPropertyData IsStartedProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsStarted);
    [UsedImplicitly] public static readonly IPropertyData IsFullscreenProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsFullscreen);
    [UsedImplicitly] public static readonly IPropertyData OverallTimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.OverallTime);
    [UsedImplicitly] public static readonly IPropertyData IsEditModeProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsEditMode);
    [UsedImplicitly] public static readonly IPropertyData EditDescriptionProperty = RegisterProperty<IssueTimerViewModel, string>(x => x.EditedTitle);
    [UsedImplicitly] public static readonly IPropertyData EstimateHoursProperty = RegisterProperty<IssueTimerViewModel, double>(x => x.EstimateHours);
    [UsedImplicitly] public static readonly IPropertyData EstimateMinutesProperty = RegisterProperty<IssueTimerViewModel, double>(x => x.EstimateMinutes);
    [UsedImplicitly] public static readonly IPropertyData EstimateSecondsProperty = RegisterProperty<IssueTimerViewModel, double>(x => x.EstimateSeconds);

    public double EstimateSeconds
    {
        get => GetValue<double>(EstimateSecondsProperty);
        set => SetValue(EstimateSecondsProperty, value);
    }

    public double EstimateMinutes
    {
        get => GetValue<double>(EstimateMinutesProperty);
        set => SetValue(EstimateMinutesProperty, value);
    }

    public double EstimateHours
    {
        get => GetValue<double>(EstimateHoursProperty);
        set => SetValue(EstimateHoursProperty, value);
    }

    public string EditedTitle
    {
        get => GetValue<string>(EditDescriptionProperty);
        set => SetValue(EditDescriptionProperty, value);
    }

    public bool IsEditMode
    {
        get => GetValue<bool>(IsEditModeProperty);
        private set => SetValue(IsEditModeProperty, value);
    }

    public TimeSpan OverallTime
    {
        get => GetValue<TimeSpan>(OverallTimeProperty);
        private set => SetValue(OverallTimeProperty, value);
    }

    public bool IsFullscreen
    {
        get => GetValue<bool>(IsFullscreenProperty);
        private set => SetValue(IsFullscreenProperty, value);
    }

    public bool IsStarted
    {
        get => GetValue<bool>(IsStartedProperty);
        private set => SetValue(IsStartedProperty, value);
    }

    public string IssueTitle
    {
        get => GetValue<string>(DescriptionProperty);
        private set => SetValue(DescriptionProperty, value);
    }

    public TimeSpan EstimateTime
    {
        get => GetValue<TimeSpan>(TotalTimeProperty);
        private set => SetValue(TotalTimeProperty, value);
    }

    public TimeSpan Time
    {
        get => GetValue<TimeSpan>(TimeProperty);
        private set => SetValue(TimeProperty, value);
    }

    [NotNull] private ISourceControl SourceControl { get; }
    [NotNull] private INotificationMessageService MessageService { get; }

    [NotNull] public WrappedIssue Issue { get; }
    private TimeSpan LastSaveTime { get; set; }

#if DEBUG
    private TimeSpan SavePeriod { get; } = TimeSpan.FromDays(1);
#else
        private TimeSpan SavePeriod { get; } = TimeSpan.FromDays(1);
#endif
    private DispatcherTimer _timer;

    public Command StartTimeCommand { get; }
    public Command PauseTimeCommand { get; }
    public Command StopTimeCommand { get; }
    public Command FullscreenCommand { get; }
    public Command<string> GoToBrowserCommand { get; }
    public Command<WrappedIssue> EditIssueCommand { get; }
    public Command ApplyCommand { get; }
    public Command CancelCommand { get; }

    public IssueTimerViewModel(
        [NotNull] WrappedIssue issue, 
        [NotNull] ISourceControl sourceControl, 
        [NotNull] INotificationMessageService messageService)
    {
        SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
        MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        Issue = issue ?? throw new ArgumentNullException(nameof(issue));

        IssueTitle = Issue.Issue.Title;
        EditedTitle = IssueTitle;
        Time = LastSaveTime = TimeSpan.FromSeconds(Issue.Issue.TimeStats.TotalTimeSpent);
        EstimateTime = TimeSpan.FromSeconds(Issue.Issue.TimeStats.TimeEstimate);

        EstimateHours = EstimateTime.Hours;
        EstimateMinutes = EstimateTime.Minutes;
        EstimateSeconds = EstimateTime.Seconds;

        CalculateOverallTime(Time, EstimateTime);

        StartTimeCommand = new Command(StartTime);
        PauseTimeCommand = new Command(PauseTime);
        FullscreenCommand = new Command(Fullscreen);
        StopTimeCommand = new Command(StopTime);

        GoToBrowserCommand = new Command<string>(GoToBrowser);

        EditIssueCommand = new Command<WrappedIssue>(EditIssue);

        ApplyCommand = new Command(Apply);
        CancelCommand = new Command(Cancel);

        CreateTimer();
    }

    private void EditIssue(WrappedIssue obj)
    {
        EditedTitle = IssueTitle;
        EstimateHours = EstimateTime.Hours;
        EstimateMinutes = EstimateTime.Minutes;
        EstimateSeconds = EstimateTime.Seconds;

        IsEditMode = !IsEditMode;
    }

    private async void Apply()
    {
        var timeSpan = new TimeSpan((int)EstimateHours, (int)EstimateMinutes, (int)EstimateSeconds);
        var request = new UpdateIssueRequest
        {
            Title = EditedTitle,
        };

        try
        {
            var newIssue = await SourceControl.UpdateIssueAsync(Issue.Issue, request).ConfigureAwait(true);

            await SourceControl.SetEstimateAsync(newIssue, timeSpan).ConfigureAwait(true);

            EstimateTime = timeSpan;
            IssueTitle = newIssue.Title;
            Issue.Issue.Title = IssueTitle;

            MessageService.OnMessage(this, "Задача обновлена");
        }
        catch
        {
            MessageService.OnMessage(this, "Не удалось отредактировать задачу");
        }
        finally
        {
            Cancel();
        }
    }

    private void Cancel()
    {
        IsEditMode = false;
    }

    private static void GoToBrowser(string textUri)
    {
        if (string.IsNullOrWhiteSpace(textUri))
            return;
        var uri = new Uri(textUri);
        uri.GoToBrowser();
    }

    protected override Task OnClosingAsync()
    {
        PauseTime();

        return base.OnClosingAsync();
    }

    private void Fullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    private void StartTime()
    {
        IsStarted = true;
        _timer.Start();
        StartIssue(Issue);
    }
        
    private void PauseTime()
    {
        SaveSpend();
        RedrawIssue();
        _timer.Stop();
        IsStarted = false;
    }

    private void RedrawIssue()
    {
        var needAddTime = (int)Time.TotalHours - Issue.Issue.TimeStats.TotalTimeSpent;
        Issue.Issue.TimeStats.TotalTimeSpent += needAddTime;
    }

    private void StopTime()
    {
        FinishIssue(Issue);

        SaveSpend();
        RedrawIssue();
        _timer.Stop();
        IsStarted = false;

        IsFullscreen = false;
    }

    private void CreateTimer()
    {
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), 
            DispatcherPriority.Normal, 
            IncrementTimer, 
            Application.Current.Dispatcher ?? throw new InvalidOperationException());
        _timer.Stop();
    }

    private void IncrementTimer(object sender, EventArgs args)
    {
        Time = Time.Add(TimeSpan.FromSeconds(1));
        CalculateOverallTime(Time, EstimateTime);
        if (TimeHelper.IsNightBreak)
            PauseTime();
    }

    private void CalculateOverallTime(TimeSpan time, TimeSpan estimateTime) => OverallTime =
        time - estimateTime > TimeSpan.Zero ? time - estimateTime : TimeSpan.Zero;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Time))
        {
            if (Time >= LastSaveTime + SavePeriod) SaveSpend();
        }
    }

    private async void SaveSpend()
    {
        await SaveSpendInternalAsync(Time - LastSaveTime).ConfigureAwait(true);
        LastSaveTime = Time;
    }

    private Task SaveSpendInternalAsync(TimeSpan timeSpan)
    {
        return SourceControl.AddSpendAsync(Issue.Issue, timeSpan);
    }

    private async void StartIssue(WrappedIssue issue)
    {
        var newIssue = await SourceControl.StartIssueAsync(issue).ConfigureAwait(true);
        issue.Labels = newIssue.Labels;
    }

    private async void FinishIssue(WrappedIssue issue)
    {
        var newIssue = await SourceControl.FinishIssueAsync(issue).ConfigureAwait(true);
        issue.Labels = newIssue.Labels;
    }
}
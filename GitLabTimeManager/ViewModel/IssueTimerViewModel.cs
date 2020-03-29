using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class IssueTimerViewModel : ViewModelBase
    {
        public static readonly PropertyData TimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.Time);
        public static readonly PropertyData TotalTimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.EstimateTime);
        public static readonly PropertyData TitleProperty = RegisterProperty<IssueTimerViewModel, string>(x => x.Description);
        public static readonly PropertyData IsStartedProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsStarted);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsFullscreen);
        public static readonly PropertyData OverallTimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.OverallTime);

        public TimeSpan OverallTime
        {
            get => GetValue<TimeSpan>(OverallTimeProperty);
            set => SetValue(OverallTimeProperty, value);
        }

        public bool IsFullscreen
        {
            get => (bool) GetValue(IsFullscreenProperty);
            private set => SetValue(IsFullscreenProperty, value);
        }

        public bool IsStarted
        {
            get => (bool) GetValue(IsStartedProperty);
            private set => SetValue(IsStartedProperty, value);
        }

        public string Description
        {
            get => (string) GetValue(TitleProperty);
            private set => SetValue(TitleProperty, value);
        }

        public TimeSpan EstimateTime
        {
            get => (TimeSpan) GetValue(TotalTimeProperty);
            private set => SetValue(TotalTimeProperty, value);
        }

        public TimeSpan Time
        {
            get => (TimeSpan) GetValue(TimeProperty);
            private set => SetValue(TimeProperty, value);
        }


        private ISourceControl SourceControl { get; }
        public WrappedIssue Issue { get; }
        private TimeSpan LastSaveTime { get; set; }

#if DEBUG
        private TimeSpan SavePeriod { get; } = TimeSpan.FromMinutes(2);
#else
        private TimeSpan SavePeriod { get; } = TimeSpan.FromHours(2);
#endif

        private DispatcherTimer _timer;

        public Command StartTimeCommand { get; }
        public Command PauseTimeCommand { get; }
        public Command StopTimeCommand { get; }
        public Command FullscreenCommand { get; }
        public Command<string> GoToBrowserCommand { get; }

        public IssueTimerViewModel([NotNull] ISourceControl sourceControl, [NotNull] WrappedIssue issue)
        {
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));

            Description = Issue.Issue.Title;
            Time = LastSaveTime = TimeSpan.FromSeconds(Issue.Issue.TimeStats.TotalTimeSpent);
            EstimateTime = TimeSpan.FromSeconds(Issue.Issue.TimeStats.TimeEstimate);
            CalculateOverallTime(Time, EstimateTime);

            StartTimeCommand = new Command(StartTime);
            PauseTimeCommand = new Command(PauseTime);
            FullscreenCommand = new Command(Fullscreen);
            StopTimeCommand = new Command(StopTime);

            GoToBrowserCommand = new Command<string>(GoToBrowser);

            CreateTimer();
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
            PauseIssue(Issue);

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

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Time))
            {
                if (Time >= LastSaveTime + SavePeriod)
                {
                    SaveSpend();
                }
            }
        }

        private void SaveSpend()
        {
            SaveSpendInternal(Time - LastSaveTime);
            LastSaveTime = Time;
        }

        private void SaveSpendInternal(TimeSpan timeSpan)
        {
            SourceControl.AddSpendAsync(Issue.Issue, timeSpan);
        }

        private async void StartIssue(WrappedIssue issue)
        {
            var result = await SourceControl.StartIssueAsync(issue.Issue).ConfigureAwait(true);
            if (result)
                UpdateLabels();
        }

        private void UpdateLabels()
        {
            LabelProcessor.UpdateLabelsEx(Issue.LabelExes, Issue.Issue.Labels);
        }

        private async void PauseIssue(WrappedIssue issue)
        {
            var result = await SourceControl.PauseIssueAsync(issue.Issue).ConfigureAwait(true);
            if (result)
                UpdateLabels();
        }

        private async void FinishIssue(WrappedIssue issue)
        {
            var result = await SourceControl.FinishIssueAsync(issue.Issue).ConfigureAwait(true);
            if (result)
                UpdateLabels();
        }
    }
}

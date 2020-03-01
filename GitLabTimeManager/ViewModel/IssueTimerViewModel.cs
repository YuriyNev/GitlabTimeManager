using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel
{
    public class IssueTimerViewModel : ViewModelBase
    {
        public static readonly PropertyData TimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.Time);
        public static readonly PropertyData TotalTimeProperty = RegisterProperty<IssueTimerViewModel, TimeSpan>(x => x.EstimateTime);
        public static readonly PropertyData TitleProperty = RegisterProperty<IssueTimerViewModel, string>(x => x.Title);
        public static readonly PropertyData IsStartedProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsStarted);
        public static readonly PropertyData IsFullscreenProperty = RegisterProperty<IssueTimerViewModel, bool>(x => x.IsFullscreen);

        public bool IsFullscreen
        {
            get => (bool) GetValue(IsFullscreenProperty);
            set => SetValue(IsFullscreenProperty, value);
        }

        public bool IsStarted
        {
            get => (bool) GetValue(IsStartedProperty);
            set => SetValue(IsStartedProperty, value);
        }

        public string Title
        {
            get => (string) GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public TimeSpan EstimateTime
        {
            get => (TimeSpan) GetValue(TotalTimeProperty);
            set => SetValue(TotalTimeProperty, value);
        }

        public TimeSpan Time
        {
            get => (TimeSpan) GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        private ISourceControl SourceControl { get; }
        private WrappedIssue Issue { get; }
        private TimeSpan LastSaveTime { get; set; }
        private TimeSpan AccumulatedTime { get; set; }
        private TimeSpan SavePeriod { get; } = TimeSpan.FromSeconds(60);

        private DispatcherTimer _timer;
        private TimeSpan _time;

        public Command StartTimeCommand { get; }
        public Command PauseTimeCommand { get; }
        public Command StopTimeCommand { get; }
        
        public Command FullscreenCommand { get; }

        public IssueTimerViewModel([NotNull] ISourceControl sourceControl, [NotNull] WrappedIssue issue)
        {
            
            SourceControl = sourceControl ?? throw new ArgumentNullException(nameof(sourceControl));
            Issue = issue ?? throw new ArgumentNullException(nameof(issue));

            Title = Issue.Issue.Title;
            Time = LastSaveTime = TimeSpan.FromSeconds(Issue.Issue.TimeStats.TotalTimeSpent);
            EstimateTime = TimeSpan.FromSeconds(Issue.Issue.TimeStats.TimeEstimate);

            StartTimeCommand = new Command(StartTime);
            PauseTimeCommand = new Command(PauseTime);
            FullscreenCommand = new Command(Fullscreen);
            StopTimeCommand = new Command(StopTime);

            CreateTimer();

            LateAssignment();
        }

        private void Fullscreen()
        {
            IsFullscreen = !IsFullscreen;
        }

        private async void LateAssignment()
        {
            await Task.Delay(1);
            Time = Time.Add(TimeSpan.FromTicks(1));
            Time = Time.Subtract(TimeSpan.FromTicks(1));
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

            _timer.Stop();

            SaveSpend(Time - LastSaveTime);
            LastSaveTime = Time;

            IsStarted = false;
        }

        private void StopTime()
        {
            FinishIssue(Issue);

            _timer.Stop();

            SaveSpend(Time - LastSaveTime);
            LastSaveTime = Time;

            IsStarted = false;
        }

        private void CreateTimer()
        {
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), 
                DispatcherPriority.Normal, 
                (sender, args) => Time = Time.Add(TimeSpan.FromSeconds(1)), 
                Application.Current.Dispatcher ?? throw new InvalidOperationException());
            _timer.Stop();
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Time))
            {
                if (Time >= LastSaveTime + SavePeriod)
                {
                    var accumulatedPeriod = Time - LastSaveTime;
                    SaveSpend(accumulatedPeriod);
                    LastSaveTime = Time;
                }
            }
        }

        private async void SaveSpend(TimeSpan timeSpan)
        {
            await SourceControl.AddSpend(Issue.Issue, timeSpan);
        }

        private async void StartIssue(WrappedIssue issue)
        {
            await SourceControl.StartIssue(issue.Issue);
        }
        
        private async void PauseIssue(WrappedIssue issue)
        {
            await SourceControl.PauseIssue(issue.Issue);
        }

        private async void FinishIssue(WrappedIssue issue)
        {
            await SourceControl.FinishIssue(issue.Issue);
        }
    }
}

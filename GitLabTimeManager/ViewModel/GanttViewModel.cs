using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using JetBrains.Annotations;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;

namespace GitLabTimeManager.ViewModel
{
    public class GanttViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData WrappedIssuesProperty = RegisterProperty<GanttViewModel, ObservableCollection<WrappedIssue>>(x => x.WrappedIssues);
        [UsedImplicitly] public static readonly PropertyData FromProperty = RegisterProperty<GanttViewModel, double>(x => x.From);
        [UsedImplicitly] public static readonly PropertyData ToProperty = RegisterProperty<GanttViewModel, double>(x => x.To);

        public double To
        {
            get => GetValue<double>(ToProperty);
            set => SetValue(ToProperty, value);
        }

        public double From
        {
            get => GetValue<double>(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public ObservableCollection<WrappedIssue> WrappedIssues
        {
            get => GetValue<ObservableCollection<WrappedIssue>>(WrappedIssuesProperty);
            set => SetValue(WrappedIssuesProperty, value);
        }

        private ChartValues<IssuePointDescription> _activityValues;
        private ChartValues<IntervalPointDescription> _estimateValues;
        private ChartValues<IntervalPointDescription> _spendValues;
        public Func<double, string> Formatter { get; set; }
        public SeriesCollection ActivitySeries { get; set; }
        public SeriesCollection EstimatesSeries { get; set; }
        public string[] Labels { get; set; }


        private IDataRequestService DataRequestService { get; }
        private IDataSubscription DataSubscription { get; }

        public GanttViewModel([NotNull] IDataRequestService dataRequestService)
        {
            DataRequestService = dataRequestService ?? throw new ArgumentNullException(nameof(dataRequestService));

            DataSubscription = DataRequestService.CreateSubscription();
            DataSubscription.NewData += DataSubscriptionOnNewData;
        }

        private void DataSubscriptionOnNewData(object sender, GitResponse e)
        {
            UpdateData(e);
        }

        private void UpdateData(GitResponse data)
        {
            var mapper = Mappers.Gantt<IssuePointDescription>()
                .XStart(x => x.Start.Ticks)
                .X(x => x.End.Ticks);

            Charting.For<IssuePointDescription>(mapper);

            var estimateMapper = Mappers.Gantt<IntervalPointDescription>()
                .XStart(x => x.StartTime.Ticks)
                .X(x => x.EndTime.Ticks);

            Charting.For<IntervalPointDescription>(estimateMapper);

            WrappedIssues = new ObservableCollection<WrappedIssue>(
                data.WrappedIssues
                    .Where(x => x.EndTime > TimeHelper.MonthAgo)
                    .Where(x => x.StartTime < TimeHelper.Today));

            if (ActivitySeries != null)
            {
                if (ActivitySeries.Chart != null)
                    ActivitySeries.Clear();
                ActivitySeries = null;
            }

            if (_activityValues != null)
            {
                _activityValues.Clear();
                _activityValues = null;
            }

            if (_estimateValues != null)
            {
                _estimateValues.Clear();
                _estimateValues = null;
            }

            if (_spendValues != null)
            {
                _spendValues.Clear();
                _spendValues = null;
            }

            var labels = new List<string>();

            _activityValues = new ChartValues<IssuePointDescription>();
            _estimateValues = new ChartValues<IntervalPointDescription>();
            _spendValues = new ChartValues<IntervalPointDescription>();

            foreach (var issue in WrappedIssues)
            {
                var start = issue.StartTime;
                if (start == null)
                    continue;
                
                var end = issue.EndTime ?? DateTime.Today;

                var point = new IssuePointDescription
                {
                    Name = issue.Issue.Title,
                    WebUrl = issue.Issue.WebUrl,
                    Iid = issue.Issue.Iid,
                    Start = start.Value,
                    End = end,
                };
                labels.Add(issue.Issue.Title);

                var estimatePoint = new IntervalPointDescription
                {
                    StartTime = start.Value,
                    EndTime = start.Value.Add(TimeSpan.FromHours(issue.Estimate)),
                };

                var spendPoint = new IntervalPointDescription
                {
                    StartTime = start.Value,
                    EndTime = start.Value.Add(TimeSpan.FromHours(issue.Estimate)),
                };

                _activityValues.Add(point);
                _estimateValues.Add(estimatePoint);
                _spendValues.Add(estimatePoint);
            }

            if (_activityValues.Count == 0)
                return;

            From = _activityValues.Min(x => x.Start.Ticks);
            To = _activityValues.Max(x => x.End.Ticks);

            if (From > To)
                return;

            ActivitySeries = new SeriesCollection
            {
                new RowSeries
                {
                    Values = _estimateValues,
                    DataLabels = true,
                    Fill = new SolidColorBrush(Colors.DarkSalmon)
                },
                new RowSeries
                {
                    Values = _activityValues,
                    DataLabels = true,
                    LabelsPosition = BarLabelPosition.Top,
                    Fill = new SolidColorBrush(Colors.CornflowerBlue)
                }
            };
            Labels = labels.ToArray();

            Formatter = value => new DateTime((long)value).ToString("dd MMM yyyy");
            RaisePropertyChanged(nameof(ActivitySeries));
            RaisePropertyChanged(nameof(_activityValues));
            RaisePropertyChanged(nameof(Labels));
        }
    }

    public class IssuePointDescription
    {
        public int Iid { get; set; }
        public string Name { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string WebUrl { get; set; }
    }

    public class IntervalPointDescription
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
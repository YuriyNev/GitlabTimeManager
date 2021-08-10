using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Services;
using JetBrains.Annotations;
using LiveCharts;
using LiveCharts.Defaults;
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

        private ChartValues<GanttPoint> _values;
        public Func<double, string> Formatter { get; set; }
        public string[] Labels { get; set; }
        public SeriesCollection Series { get; set; }

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
            WrappedIssues = data.WrappedIssues;

            _values = new ChartValues<GanttPoint>();

            var labels = new List<string>();

            var minDate = WrappedIssues.SelectMany(x => x.Events).Min(x => x.CreatedAt);
            var maxDate = WrappedIssues.SelectMany(x => x.Events).Max(x => x.CreatedAt);

            foreach (var issue in WrappedIssues)
            {
                if (issue.StartTime != null)
                {
                    //TimeSpan timeSpanStart = issue.StartTime.Value.Subtract(minDate);
                    //TimeSpan timeSpanEnd = issue.EndTime.Value.Subtract(minDate);
                    if (!issue.EndTime.HasValue)
                        continue;

                    _values.Add(new GanttPoint(issue.StartTime.Value.Ticks, (issue.EndTime ?? maxDate).Ticks));
                    labels.Add(issue.Issue.Title);
                }
            }

            if (_values.Count == 0)
                return;

            From = _values.Min(x => x.StartPoint);
            To = _values.Max(x => x.EndPoint);

            if (From > To)
                return;

            Series = new SeriesCollection
            {
                new RowSeries
                {
                    Values = _values,
                    DataLabels = true
                }
            };
            Formatter = value => new DateTime((long)value).ToString("dd MMM yyyy");
            Labels = labels.ToArray();
        }
    }
}

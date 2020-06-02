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

        public void UpdateData(GitResponse data)
        {
            WrappedIssues = data.WrappedIssues;

            _values = new ChartValues<GanttPoint>();

            var labels = new List<string>();

            foreach (var issue in WrappedIssues)
            {
                if (issue.StartTime != null && issue.EndTime != null)
                {
                    _values.Add(new GanttPoint(issue.StartTime.Value.Ticks, issue.EndTime.Value.Ticks));
                    labels.Add(issue.Issue.Title);
                }
            }

            From = _values.First().StartPoint;
            To = _values.Last().EndPoint;

            Series = new SeriesCollection
            {
                new RowSeries
                {
                    Values = _values,
                    DataLabels = true
                }
            };
            Formatter = value => new DateTime((long)value).ToString("dd MMM");
            Labels = labels.ToArray();
        }
    }
}

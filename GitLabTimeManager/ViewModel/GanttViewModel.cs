using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Helpers;
using GitLabTimeManager.Services;
using GitLabTimeManager.View;
using JetBrains.Annotations;
using LiveCharts;
using LiveCharts.Configurations;
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

        private ChartValues<IssuePointDescription> _values;
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
            var mapper = Mappers.Gantt<IssuePointDescription>()
                .XStart(x => x.Start.Ticks)
                .X(x => x.End.Ticks);

            Charting.For<IssuePointDescription>(mapper);

            WrappedIssues = new ObservableCollection<WrappedIssue>(
                data.WrappedIssues
                    .Where(x => x.EndTime > TimeHelper.MonthAgo)
                    .Where(x => x.StartTime < TimeHelper.Today));

            if (Series != null)
            {
                if (Series.Chart != null)
                    Series.Clear();
                Series = null;
            }

            if (_values != null)
            {
                _values.Clear();
                _values = null;
            }

            _values = new ChartValues<IssuePointDescription>();

            var labels = new List<string>();

            foreach (var issue in WrappedIssues)
            {
                var start = issue.StartTime;
                var end = issue.EndTime;
                if (end - start < TimeSpan.FromHours(8))
                    end = end.AddHours(8);

                var point = new IssuePointDescription
                {
                    Name = issue.Issue.Title,
                    WebUrl = issue.Issue.WebUrl,
                    Iid = issue.Issue.Iid,
                    Start = start,
                    End = end,
                };

                _values.Add(point);
                labels.Add(issue.Issue.Title);
            }

            if (_values.Count == 0)
                return;

            From = _values.Min(x => x.Start.Ticks);
            To = _values.Max(x => x.End.Ticks);

            if (From > To)
                return;

            Series = new SeriesCollection
            {
                new RowSeries
                {
                    Values = _values,
                    DataLabels = true,
                }
            };
            Formatter = value => new DateTime((long)value).ToString("dd MMM yyyy");
            Labels = labels.ToArray();
            RaisePropertyChanged(nameof(Labels));
            RaisePropertyChanged(nameof(Series));
            RaisePropertyChanged(nameof(_values));
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
}

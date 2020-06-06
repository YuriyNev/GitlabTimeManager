using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using GitLabTimeManager.Tools;

namespace GitLabTimeManager.Helpers
{
    public static class LabelProcessor 
    {
        internal const string ToDoLabel = "* Можно выполнять";
        internal const string DoingLabel = "* В работе";
        internal const string DistributiveLabel = "* В дистрибутиве";
        internal const string RevisionLabel = "* Ревизия";

        internal const string ProjectControlLabel = "Проектное управление";
        internal const string SpecTaskLabel = "Спец. задача";

        public static void StartIssue(List<string> labels)
        {
            labels.Remove(ToDoLabel);
            labels.Remove(RevisionLabel);
            labels.Remove(DistributiveLabel);

            labels.Add(DoingLabel);
        }

        public static bool IsStarted(List<string> labels) => labels.Contains(DoingLabel);

        public static bool IsPaused(List<string> labels) => !labels.Contains(DoingLabel);

        public static void PauseIssue(List<string> labels)
        {
            labels.Remove(DoingLabel);
            labels.Add(ToDoLabel);
        }
        
        public static void FinishIssue(List<string> labels)
        {
            labels.Remove(DoingLabel);
            labels.Remove(ToDoLabel);
            labels.Add(RevisionLabel);
        }

        public static void UpdateLabelsEx(ObservableCollection<LabelEx> labelExes, IList<string> source)
        {
            if (labelExes == null || source == null)
                return;
            labelExes.Clear();

            foreach (var txtLabel in source)
            {
                var item = LabelsCollection.Labels.FirstOrDefault(x => x.Name == txtLabel);
                if (item != null) labelExes.Add(item);
            }
        }

        public static bool IsExcludeLabels(this IEnumerable<LabelEx> argLabelExes)
        {
            var excludeLabel = new List<LabelEx> { LabelsCollection.ProjectControlLabel };
            return argLabelExes.Any(argLabelEx => excludeLabel.Contains(argLabelEx));
        }

        public static bool IsExcludeLabels(this IEnumerable<string> argLabelExes)
        {
            var excludeLabel = new List<string> { ProjectControlLabel };
            var r = argLabelExes.Any(argLabelEx => excludeLabel.Contains(argLabelEx));
            return r;
        }

    }

    public static class LabelsCollection
    {
        public static readonly LabelEx ToDoLabel = new LabelEx
            { Name = LabelProcessor.ToDoLabel, Color = Color.FromRgb(66, 139, 202) };

        public static readonly LabelEx DoingLabel = new LabelEx
            { Name = LabelProcessor.DoingLabel, Color = Color.FromRgb(0, 51, 204) };

        public static readonly LabelEx DistributiveLabel = new LabelEx
            { Name = LabelProcessor.DistributiveLabel, Color = Color.FromRgb(209, 0, 105) };

        public static readonly LabelEx RevisionLabel = new LabelEx
            { Name = LabelProcessor.RevisionLabel, Color = Color.FromRgb(68, 173, 142) };
        
        public static readonly LabelEx ProjectControlLabel = new LabelEx
            { Name = LabelProcessor.ProjectControlLabel, Color = Color.FromRgb(209, 209, 0) };
        
        public static readonly LabelEx SpecTaskLabel= new LabelEx
            { Name = LabelProcessor.SpecTaskLabel, Color = Color.FromRgb(0, 0, 0) };

        public static IEnumerable<LabelEx> Labels { get; } = new List<LabelEx>
        {
            ToDoLabel, DoingLabel, DistributiveLabel, RevisionLabel, ProjectControlLabel, SpecTaskLabel
        };
    }

    public class LabelEx : NotifyObject
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }
}
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
            //66 139 20
            //00 51 204
            //209 0 105


        public static void StartIssue(List<string> labels)
        {
            labels.Remove(ToDoLabel);
            labels.Remove(RevisionLabel);
            labels.Remove(DistributiveLabel);

            labels.Add(DoingLabel);
        }

        public static bool IsStarted(List<string> labels)
        {
            return labels.Contains(DoingLabel);
        }

        public static bool IsPaused(List<string> labels)
        {
            return !labels.Contains(DoingLabel);
        }

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
    }

    public static class LabelsCollection
    {
        public static LabelEx ToDoLabel = new LabelEx
            { Name = LabelProcessor.ToDoLabel, Color = Color.FromRgb(66, 139, 202) };

        public static LabelEx DoingLabel = new LabelEx
            { Name = LabelProcessor.DoingLabel, Color = Color.FromRgb(0, 51, 204) };

        public static LabelEx DistrLabel = new LabelEx
            { Name = LabelProcessor.DistributiveLabel, Color = Color.FromRgb(209, 0, 105) };

        public static LabelEx RevisionLabel = new LabelEx
            { Name = LabelProcessor.RevisionLabel, Color = Color.FromRgb(68, 173, 142) };
        
        public static LabelEx ProjectControlLabel = new LabelEx
            { Name = LabelProcessor.ProjectControlLabel, Color = Color.FromRgb(0, 0, 0) };

        public static IList<LabelEx> Labels { get; } = new List<LabelEx>
        {
            ToDoLabel, DoingLabel, DistrLabel, RevisionLabel, ProjectControlLabel
        };
    }

    public class LabelEx : NotifyObject
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }
}
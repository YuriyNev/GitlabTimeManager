using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Catel.Data;
using Catel.MVVM;
using GitLabTimeManager.Services;
using GitLabTimeManager.ViewModel.Settings;
using GongSolutions.Wpf.DragDrop;

using Label = GitLabApiClient.Models.Projects.Responses.Label;

namespace GitLabTimeManager.ViewModel;

public class LabelSettingsViewModel : SettingsViewModelBase
{
    public static readonly PropertyData AvailableLabelsProperty = RegisterProperty<LabelSettingsViewModel, ObservableCollection<Label>>(x => x.AvailableLabels);
    public static readonly PropertyData BoardLabelsProperty = RegisterProperty<LabelSettingsViewModel, ObservableCollection<Label>>(x => x.BoardLabels, () => new ObservableCollection<Label>());
    public static readonly PropertyData StartLabelProperty = RegisterProperty<LabelSettingsViewModel, Label?>(x => x.StartLabel);
    public static readonly PropertyData PauseLabelProperty = RegisterProperty<LabelSettingsViewModel, Label?>(x => x.PauseLabel);
    public static readonly PropertyData FinishLabelProperty = RegisterProperty<LabelSettingsViewModel, Label?>(x => x.FinishLabel);
    public static readonly PropertyData PassedLabelsProperty = RegisterProperty<LabelSettingsViewModel, ObservableCollection<Label>>(x => x.PassedLabels);

    public ObservableCollection<Label> PassedLabels
    {
        get => GetValue<ObservableCollection<Label>>(PassedLabelsProperty);
        set => SetValue(PassedLabelsProperty, value);
    }

    public Label? FinishLabel
    {
        get => GetValue<Label>(FinishLabelProperty);
        set => SetValue(FinishLabelProperty, value);
    }

    public Label? PauseLabel
    {
        get => GetValue<Label>(PauseLabelProperty);
        set => SetValue(PauseLabelProperty, value);
    }
    public Label? StartLabel
    {
        get => GetValue<Label>(StartLabelProperty);
        set => SetValue(StartLabelProperty, value);
    }

    public ObservableCollection<Label> BoardLabels
    {
        get => GetValue<ObservableCollection<Label>>(BoardLabelsProperty);
        set => SetValue(BoardLabelsProperty, value);
    }

    public ObservableCollection<Label> AvailableLabels
    {
        get => GetValue<ObservableCollection<Label>>(AvailableLabelsProperty);
        set => SetValue(AvailableLabelsProperty, value);
    }

    public IReadOnlyList<Label>? Labels { get; }

    public LabelDropHandler LabelDropHandler { get; } = new();
    public LabelCopyHandler LabelDropCopyHandler { get; } = new();
    public SingleDropHandler StartLabelDropHandler { get; } = new();
    public SingleDropHandler PauseLabelDropHandler { get; } = new();
    public SingleDropHandler FinishLabelDropHandler { get; } = new();

    public Command ResetStartLabelCommand { get; }
    public Command ResetPauseLabelCommand { get; }
    public Command ResetFinishLabelCommand { get; }

    private ILabelService LabelService { get; }

    public LabelSettingsViewModel(
        SettingsArgument? settingsArgument,
        ILabelService labelService,
        IProfileService profileService,
        IUserProfile userProfile,
        INotificationMessageService messageService)
        : base(profileService, userProfile, messageService)
    {
        LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
        Labels = settingsArgument?.Labels;

        ResetStartLabelCommand = new Command(ResetStartLabel);
        ResetPauseLabelCommand = new Command(ResetPauseLabel);
        ResetFinishLabelCommand = new Command(ResetFinishLabel);

        StartLabelDropHandler.Dropped += StartLabel_Dropped;
        PauseLabelDropHandler.Dropped += PauseLabel_Dropped;
        FinishLabelDropHandler.Dropped += FinishLabel_Dropped;

        ApplyOptions(UserProfile);
    }

    private void ResetFinishLabel() => FinishLabel = null;

    private void ResetPauseLabel() => PauseLabel = null;

    private void ResetStartLabel() => StartLabel = null;

    protected override void ApplyOptions(IUserProfile userProfile)
    {
        var labelSetting = userProfile.LabelSettings;

        var allLabels = Labels;

        if (labelSetting == null || allLabels == null) return;

        var allBoardLabels = LabelService.FilterLabels(allLabels, labelSetting.AllBoardLabels.ToList());

        BoardLabels = new ObservableCollection<Label>(allBoardLabels);

        var labels = allLabels.Except(BoardLabels);
        AvailableLabels = new ObservableCollection<Label>(labels);

        StartLabel = allLabels.FirstOrDefault(l => l.Name == labelSetting.BoardStateLabels?.DoingLabel);
        PauseLabel = allLabels.FirstOrDefault(l => l.Name == labelSetting.BoardStateLabels?.ToDoLabel);
        FinishLabel = allLabels.FirstOrDefault(l => l.Name == labelSetting.BoardStateLabels?.DoneLabel);

        var passedLabels = LabelService.FilterLabels(allLabels, labelSetting.PassedLabels.ToList());
        PassedLabels = new ObservableCollection<Label>(passedLabels);
    }

    public override Action<IUserProfile> SaveAction =>
        profile =>
        {
            if (Labels != null)
            {
                var boardLabels = profile.LabelSettings.BoardStateLabels;

                boardLabels.ToDoLabel = PauseLabel?.Name;
                boardLabels.DoingLabel = StartLabel?.Name;
                boardLabels.DoneLabel = FinishLabel?.Name;
                UserProfile.LabelSettings.BoardStateLabels = boardLabels;

                UserProfile.LabelSettings.OtherBoardLabels = BoardLabels
                    .Select(x => x.Name)
                    .Where(x => x != boardLabels.ToDoLabel)
                    .Where(x => x != boardLabels.DoingLabel)
                    .Where(x => x != boardLabels.DoneLabel)
                    .ToList();

                UserProfile.LabelSettings.PassedLabels = PassedLabels
                    .Select(x => x.Name)
                    .ToList();
            }
        };

    private void FinishLabel_Dropped(object? sender, Label e) => FinishLabel = e;

    private void PauseLabel_Dropped(object? sender, Label e) => PauseLabel = e;

    private void StartLabel_Dropped(object? sender, Label e) => StartLabel = e;

    protected override Task CloseAsync()
    {
        FinishLabelDropHandler.Dropped -= FinishLabel_Dropped;
        PauseLabelDropHandler.Dropped -= PauseLabel_Dropped;
        StartLabelDropHandler.Dropped -= StartLabel_Dropped;

        return base.CloseAsync();
    }
}

public class LabelDropHandler : IDropTarget
{
    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        dropInfo.Effects = dropInfo.Data switch
        {
            Label _ => DragDropEffects.Move,
            _ => DragDropEffects.None
        };
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        var droppedObject = dropInfo.Data as Label;
        var dropTargetItem = dropInfo.TargetItem as Label;
        var dropTargetCollection = (IList)dropInfo.TargetCollection;
        var sourceCollection = (IList)dropInfo.DragInfo.SourceCollection;

        if (dropTargetItem != null)
        {

        }

        if (dropTargetCollection != null)
        {
            if (!dropTargetCollection.Contains(droppedObject))
            {
                dropTargetCollection.Add(droppedObject);
                sourceCollection.Remove(droppedObject);
            }
        }
    }
}
public class LabelCopyHandler : IDropTarget
{
    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        dropInfo.Effects = dropInfo.Data switch
        {
            Label _ => DragDropEffects.Move,
            _ => DragDropEffects.None
        };
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        var droppedObject = dropInfo.Data as Label;
        var dropTargetItem = dropInfo.TargetItem as Label;
        var dropTargetCollection = (IList)dropInfo.TargetCollection;

        if (dropTargetItem != null)
        {

        }

        if (dropTargetCollection != null)
        {
            if (!dropTargetCollection.Contains(droppedObject))
            {
                dropTargetCollection.Add(droppedObject);
            }
        }
    }
}

public class SingleDropHandler : IDropTarget
{
    public event EventHandler<Label> Dropped;

    public void DragOver(IDropInfo dropInfo)
    {
        dropInfo.Effects = DragDropEffects.Move;
        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is Label droppedObject)
        {
            Dropped?.Invoke(this, droppedObject);
        }
    }
}

public class SettingsArgument
{
    public IReadOnlyList<Label> Labels { get; set; }
}
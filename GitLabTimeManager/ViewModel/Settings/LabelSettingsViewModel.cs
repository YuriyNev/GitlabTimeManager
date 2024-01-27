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
using GongSolutions.Wpf.DragDrop;
using JetBrains.Annotations;
using Label = GitLabApiClient.Models.Projects.Responses.Label;

namespace GitLabTimeManager.ViewModel.Settings;

[UsedImplicitly]
public class LabelSettingsViewModel : SettingsViewModelBase
{
    [UsedImplicitly] public static readonly IPropertyData AvailableLabelsProperty = RegisterProperty<LabelSettingsViewModel, ObservableCollection<Label>>(x => x.AvailableLabels);
    [UsedImplicitly] public static readonly IPropertyData BoardLabelsProperty = RegisterProperty<LabelSettingsViewModel, ObservableCollection<Label>>(x => x.BoardLabels, () => new ObservableCollection<Label>());
    [UsedImplicitly] public static readonly IPropertyData StartLabelProperty = RegisterProperty<LabelSettingsViewModel, Label>(x => x.StartLabel);
    [UsedImplicitly] public static readonly IPropertyData PauseLabelProperty = RegisterProperty<LabelSettingsViewModel, Label>(x => x.PauseLabel);
    [UsedImplicitly] public static readonly IPropertyData FinishLabelProperty = RegisterProperty<LabelSettingsViewModel, Label>(x => x.FinishLabel);

    public Label FinishLabel
    {
        get => GetValue<Label>(FinishLabelProperty);
        set => SetValue(FinishLabelProperty, value);
    }

    public Label PauseLabel
    {
        get => GetValue<Label>(PauseLabelProperty);
        set => SetValue(PauseLabelProperty, value);
    }
    public Label StartLabel
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

    [CanBeNull] public IReadOnlyList<Label> Labels { get; }

    public LabelDropHandler LabelDropHandler { get; } = new LabelDropHandler();
    public SingleDropHandler StartLabelDropHandler { get; } = new SingleDropHandler();
    public SingleDropHandler PauseLabelDropHandler { get; } = new SingleDropHandler();
    public SingleDropHandler FinishLabelDropHandler { get; } = new SingleDropHandler();

    public Command ResetStartLabelCommand { get; }
    public Command ResetPauseLabelCommand { get; }
    public Command ResetFinishLabelCommand { get; }

    [NotNull] private ILabelService LabelService { get; }

    public LabelSettingsViewModel(
        [CanBeNull] SettingsArgument settingsArgument,
        [NotNull] ILabelService labelService,
        [NotNull] IProfileService profileService,
        [NotNull] IUserProfile userProfile,
        [NotNull] INotificationMessageService messageService) 
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

    private void ApplyOptions(IUserProfile userProfile)
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
    }

    protected override void SaveOptions()
    {
        try
        {
            if (Labels != null)
            {
                var boardLabels = UserProfile.LabelSettings.BoardStateLabels;

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
            }

            ProfileService.Serialize(UserProfile);

            MessageService.OnMessage(this, "Saved");
        }
        catch
        {
            MessageService.OnMessage(this, "Failed to save settings!");
        }
        finally
        {
            OnClose?.Invoke();
        }
    }

    private void FinishLabel_Dropped(object sender, Label e) => FinishLabel = e;

    private void PauseLabel_Dropped(object sender, Label e) => PauseLabel = e;

    private void StartLabel_Dropped(object sender, Label e) => StartLabel = e;

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
    public SettingsArgument()
    {
    }

    public SettingsArgument(IReadOnlyList<Label> labels)
    {
        Labels = labels;
    }

    public IReadOnlyList<Label> Labels { get; set; }
}
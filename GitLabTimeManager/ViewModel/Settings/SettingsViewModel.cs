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

// ReSharper disable once CheckNamespace
namespace GitLabTimeManager.ViewModel
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsViewModel : ViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData TokenProperty = RegisterProperty<SettingsViewModel, string>(x => x.Token);
        [UsedImplicitly] public static readonly PropertyData UriProperty = RegisterProperty<SettingsViewModel, string>(x => x.Uri);
        [UsedImplicitly] public static readonly PropertyData AvailableLabelsProperty = RegisterProperty<SettingsViewModel, ObservableCollection<Label>>(x => x.AvailableLabels);
        [UsedImplicitly] public static readonly PropertyData BoardLabelsProperty = RegisterProperty<SettingsViewModel, ObservableCollection<Label>>(x => x.BoardLabels, () => new ObservableCollection<Label>());
        [UsedImplicitly] public static readonly PropertyData StartLabelProperty = RegisterProperty<SettingsViewModel, Label>(x => x.StartLabel);
        [UsedImplicitly] public static readonly PropertyData PauseLabelProperty = RegisterProperty<SettingsViewModel, Label>(x => x.PauseLabel);
        [UsedImplicitly] public static readonly PropertyData FinishLabelProperty = RegisterProperty<SettingsViewModel, Label>(x => x.FinishLabel);

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

        public string Uri
        {
            get => GetValue<string>(UriProperty);
            set => SetValue(UriProperty, value);
        }

        public string Token
        {
            get => GetValue<string>(TokenProperty);
            set => SetValue(TokenProperty, value);
        }

        public Command ApplyCommand { get; }
        public Command ResetStartLabelCommand { get; }
        public Command ResetPauseLabelCommand { get; }
        public Command ResetFinishLabelCommand { get; }

        [CanBeNull] public IReadOnlyList<Label> Labels { get; }
        [NotNull] private IProfileService ProfileService { get; }
        [NotNull] private IUserProfile UserProfile { get; }
        [NotNull] private ILabelService LabelService { get; }
        [NotNull] private INotificationMessageService MessageService { get; }

        public LabelDropHandler LabelDropHandler { get; } = new LabelDropHandler();
        public SingleDropHandler StartLabelDropHandler { get; } = new SingleDropHandler();
        public SingleDropHandler PauseLabelDropHandler { get; } = new SingleDropHandler();
        public SingleDropHandler FinishLabelDropHandler { get; } = new SingleDropHandler();
        
        public SettingsViewModel(
            [CanBeNull] SettingsArgument settingsArgument,
            [NotNull] IProfileService profileService,
            [NotNull] IUserProfile userProfile, 
            [NotNull] ILabelService labelService,
            [NotNull] INotificationMessageService messageService)
        {
            ProfileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
            LabelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
            MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            Labels = settingsArgument?.Labels;

            StartLabelDropHandler.Dropped += StartLabel_Dropped;
            PauseLabelDropHandler.Dropped += PauseLabel_Dropped;
            FinishLabelDropHandler.Dropped += FinishLabel_Dropped;

            ApplyCommand = new Command(SaveOptions);
            ResetStartLabelCommand = new Command(ResetStartLabel);
            ResetPauseLabelCommand = new Command(ResetPauseLabel);
            ResetFinishLabelCommand = new Command(ResetFinishLabel);
            
            ApplyOptions(UserProfile);
        }

        private void ResetFinishLabel() => FinishLabel = null;

        private void ResetPauseLabel() => PauseLabel = null;

        private void ResetStartLabel() => StartLabel = null;

        private void ApplyOptions(IUserProfile userProfile)
        {
            Token = userProfile.Token;
            Uri = userProfile.Url;

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

        private void SaveOptions()
        {
            try
            {
                UserProfile.Token = Token;
                UserProfile.Url = Uri;

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

                MessageService.OnMessage(this, "Настройки сохранены. Требуется перезапуск приложения.");
            }
            catch
            {
                MessageService.OnMessage(this, "Не удалось сохранить настройки!");
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
            var sourceCollection = (IList) dropInfo.DragInfo.SourceCollection;

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
        public IReadOnlyList<Label> Labels { get; set; }
    }

}

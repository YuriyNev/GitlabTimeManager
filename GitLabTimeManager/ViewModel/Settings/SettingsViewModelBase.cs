using System;
using Catel.MVVM;
using GitLabTimeManager.Services;
using JetBrains.Annotations;

namespace GitLabTimeManager.ViewModel.Settings;

public abstract class SettingsViewModelBase : ViewModelBase
{
    [NotNull] protected INotificationMessageService MessageService { get; }
    [NotNull] protected IProfileService ProfileService { get; }
    [NotNull] protected IUserProfile UserProfile { get; }

    public Command ApplyCommand { get; }

    protected SettingsViewModelBase(
        [NotNull] IProfileService profileService,
        [NotNull] IUserProfile userProfile,
        [NotNull] INotificationMessageService messageService)
    {
        ProfileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
        MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        ApplyCommand = new Command(SaveOptions);
    }

    protected abstract void SaveOptions();

    protected Action OnClose { get; private set; }

    public void SetOnClose(Action action) => OnClose = action;
}
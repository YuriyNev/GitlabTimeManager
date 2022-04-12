using System;
using Catel.MVVM;
using GitLabTimeManager.Services;

namespace GitLabTimeManager.ViewModel.Settings;

public abstract class SettingsViewModelBase : ViewModelBase
{
    protected INotificationMessageService MessageService { get; }
    protected IProfileService ProfileService { get; }
    protected IUserProfile UserProfile { get; }

    public Command ApplyCommand { get; }
    public abstract Action<IUserProfile> SaveAction { get; }
    protected abstract void ApplyOptions(IUserProfile userProfile);

    protected SettingsViewModelBase(
        IProfileService profileService,
        IUserProfile userProfile,
        INotificationMessageService messageService)
    {
        ProfileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
        MessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        ApplyCommand = new Command(SaveOptions);
    }

    private void SaveOptions()
    {
        try
        {
            SaveAction(UserProfile);

            ProfileService.Serialize(UserProfile);

            MessageService.OnMessage(this, "Настройки сохранены");
        }
        catch
        {
            MessageService.OnMessage(this, "Не удалось сохранить настройки!");
        }
        finally
        {
            OnClose?.Invoke();
        }
    }

    protected Action OnClose { get; private set; }

    public void SetOnClose(Action action) => OnClose = action;
}
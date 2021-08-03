using System;
using Catel.Data;
using GitLabTimeManager.Services;
using GitLabTimeManager.ViewModel.Settings;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace GitLabTimeManager.ViewModel
{
    [UsedImplicitly]
    public class ConnectionSettingsViewModel : SettingsViewModelBase
    {
        [UsedImplicitly] public static readonly PropertyData TokenProperty = RegisterProperty<ConnectionSettingsViewModel, string>(x => x.Token);
        [UsedImplicitly] public static readonly PropertyData UriProperty = RegisterProperty<ConnectionSettingsViewModel, string>(x => x.Uri);
        
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

        public ConnectionSettingsViewModel(
            [NotNull] IProfileService profileService,
            [NotNull] IUserProfile userProfile, 
            [NotNull] INotificationMessageService messageService)
            : base(profileService, userProfile, messageService)
        {
            ApplyOptions(UserProfile);
        }

        private void ApplyOptions(IUserProfile userProfile)
        {
            Token = userProfile.Token;
            Uri = userProfile.Url;
        }

        protected override void SaveOptions()
        {
            try
            {
                UserProfile.Token = Token;
                UserProfile.Url = Uri;

                ProfileService.Serialize(UserProfile);

                MessageService.OnMessage(this, "Настройки сохранены. Требуется перезапуск приложения.");
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
    }
}
using System;

namespace GitLabTimeManager.Services
{
    public class NotificationMessageService : INotificationMessageService
    {
        public event EventHandler<string> NewMessage;

        public void OnSendMessage(object sender, string message)
        {
            NewMessage?.Invoke(sender, message);
        }
    }

    public interface INotificationMessageService
    {
        event EventHandler<string> NewMessage;

        void OnSendMessage(object sender, string message);
    }
}
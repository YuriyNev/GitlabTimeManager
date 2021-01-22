using System;

namespace GitLabTimeManager.Services
{
    public class MessageSubscription : IMessageSubscription
    {
        public event EventHandler<string> NewMessage;

        public void OnSendMessage(object sender, string message)
        {
            NewMessage?.Invoke(sender, message);
        }
    }

    public class NotificationMessageService : INotificationMessageService
    {
        public IMessageSubscription CreateSubscription()
        {
            return new MessageSubscription();
        }
    }

    public interface INotificationMessageService
    {
        IMessageSubscription CreateSubscription();

    }

    public interface IMessageSubscription
    {
        event EventHandler<string> NewMessage;

        void OnSendMessage(object sender, string message);
    }
}
using System;
using System.Collections.Generic;

namespace GitLabTimeManager.Services;

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
    private IList<MessageSubscription> Subscriptions { get; } = new List<MessageSubscription>();

    public IMessageSubscription CreateSubscription()
    {
        var subscription = new MessageSubscription();

        Subscriptions.Add(subscription);

        return subscription;
    }

    public void OnMessage(object sender, string message)
    {
        foreach (var subscription in Subscriptions)
        {
            subscription.OnSendMessage(sender, message);
        }
    }
}

public interface INotificationMessageService
{
    IMessageSubscription CreateSubscription();

    void OnMessage(object sender, string message);
}

public interface IMessageSubscription
{
    event EventHandler<string> NewMessage;
}
namespace TelegramSender;

public class BotStorage
{
    public IList<long> SubscriptionChats { get; set; } = new List<long>();
}
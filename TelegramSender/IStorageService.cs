namespace TelegramSender;

public interface IStorageService
{
    BotStorage Deserialize();

    void Serialize(BotStorage profile);

    event EventHandler<BotStorage> Serialized;
}
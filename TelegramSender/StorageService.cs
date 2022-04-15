using System.Text.Json;

namespace TelegramSender;

public static class StorageProvider
{
    public static IStorageService Instance { get; } = new StorageService();
}

public class StorageService : IStorageService
{
    private const string Location = "chat_list.json";

    public BotStorage Deserialize()
    {
        var botStorage = new BotStorage();
        try
        {
            using var stream = new StreamReader(Location);
            var json = stream.ReadToEnd();
            botStorage = JsonSerializer.Deserialize<BotStorage>(json);
        }
        catch (FileNotFoundException)
        {
            Serialize(botStorage);
        }
        catch
        {
            throw new InvalidDataException();
        }

        return botStorage;
    }

    public void Serialize(BotStorage profile)
    {
        using var stream = new StreamWriter(Location);
        var json = JsonSerializer.Serialize(profile);
        stream.Write(json);

        Serialized?.Invoke(this, profile);
    }

    public event EventHandler<BotStorage> Serialized;
}
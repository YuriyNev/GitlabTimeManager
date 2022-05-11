namespace TelegramSender;

public static class FormatStringHelper
{
    public static string FitTo(this string value, int maxSize) => $"{value.Substring(0, Math.Min(value.Length, maxSize))}{new string(' ', Math.Max(maxSize - value.Length, 0))}";
}
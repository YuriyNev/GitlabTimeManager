namespace TelegramSender;

public static class FormatStringHelper
{
    public static string FitTo(this string value, int maxSize) => $"{value}{new string(' ', Math.Max(maxSize - value.Length, 0))}";
}
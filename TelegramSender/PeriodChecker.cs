namespace TelegramSender;

public class PeriodChecker
{
    private DateTime? _old;

    public void RememberTime(DateTime time)
    {
        _old = time;
    }

    public bool IsNewDay => !_old.HasValue || _old.Value.Minute != DateTime.Now.Minute;
}
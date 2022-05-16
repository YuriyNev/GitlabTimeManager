namespace GitLabTimeManagerCore.Services;

public class TimeManager : ITimeManager
{
    //public DateTime StartTime => DateTime.Now.Date;
    //public DateTime EndTime => DateTime.Now;

    public DateTime StartTime => DateTime.Now.Date.AddDays(-7);
    public DateTime EndTime => DateTime.Now.Date.AddDays(-0);
}

public interface ITimeManager
{
    DateTime StartTime { get; }
    DateTime EndTime { get; }
}
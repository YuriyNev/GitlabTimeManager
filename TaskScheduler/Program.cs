// See https://aka.ms/new-console-template for more information

using TaskScheduler;

async Task Delay(int id)
{
    Console.WriteLine($"[{DateTime.Now:T}] started '{id}'");
    await Task.Delay(5_000);
    Console.WriteLine($"[{DateTime.Now:T}] finished '{id}'");
}

var schedule = new Scheduler(_ => false);
var now = DateTime.Now;
schedule.AddTask(new ScheduleTime(now.AddSeconds(5)), async () => await Delay(1), "1");
schedule.AddTask(new ScheduleTime(now.AddSeconds(5)), async () => await Delay(2), "2");
schedule.AddTask(new ScheduleTime(now.AddSeconds(6)), async () => await Delay(3), "3");
schedule.AddTask(new ScheduleTime(now.AddSeconds(7)), async () => await Delay(4), "4");
//schedule.AddTask(new ScheduleTime(now), async () => await Delay(2), now.ToString("u"));

await Task.Delay(-1);

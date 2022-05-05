using System.Diagnostics;
using System.Timers;

namespace TaskScheduler
{
    public class Scheduler : IDisposable
    {
        private Func<DateTime, bool> IsHoliday { get; }
        private readonly List<ScheduleTask> _tasks = new();
        private readonly CancellationTokenSource _tokenSource = new();

        private readonly System.Timers.Timer _timer;

        private bool _taskRan;

        public Scheduler(Func<DateTime, bool> isHoliday)
        {
            IsHoliday = isHoliday;
            _timer = new System.Timers.Timer(1000);
            _timer.Start();
            _timer.Elapsed += TimerOnElapsed;
        }

        public void AddTask(ScheduleTime scheduleTime, Func<Task> action, string description)
        {
            _tasks.Add(new ScheduleTask
            {
                Action = action,
                ScheduleTime = scheduleTime,
                Description = description,
            });

            Debug.WriteLine($"Task '{description}' will be planned at {scheduleTime.Hours}:{scheduleTime.Minutes}:{scheduleTime.Seconds}");
        }

        private bool CheckTime(ScheduleTime time)
        {
            if (time.Hours != DateTime.Now.Hour || time.Minutes != DateTime.Now.Minute || time.Seconds != DateTime.Now.Second)
                return false;

            if (IsHoliday(DateTime.Now))
                return false;

            return true;
        }

        private List<ScheduleTask> PlannedTasks { get; } = new();

        private void TimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            var scheduleTasks = _tasks
                .Where(x => !x.IsPlanned)
                .Where(x => CheckTime(x.ScheduleTime))
                .ToList();

            if (_taskRan)
            {
                foreach (var scheduleTask in scheduleTasks)
                {
                    scheduleTask.MarkForRun();
                    PlannedTasks.Add(scheduleTask);
                }

                return;
            }

            _taskRan = true;

            foreach (var scheduleTask in scheduleTasks) 
                scheduleTask.MarkForRun();

            var compositeTask = new Task(async () =>
            {
                var plannedTasks = PlannedTasks.ToList();
                var allTasks = plannedTasks
                    .Union(scheduleTasks)
                    .ToList();

                foreach (var scheduleTask in allTasks)
                {
                    var task = scheduleTask.Action();
                    await task;
                }
                
                foreach (var scheduleTask in allTasks) 
                    scheduleTask.UnMarkForRun();

                for (int i = PlannedTasks.Count - 1; i >= 0; i--)
                {
                    if (!PlannedTasks[i].IsPlanned) 
                        PlannedTasks.RemoveAt(i);
                }

                _taskRan = false;
            });
            compositeTask.RunSynchronously();
            //TaskPool.Add(compositeTask);
        }

        private class ScheduleTask
        {
            public bool IsPlanned { get; private set; }

            public ScheduleTime ScheduleTime { get; init; }

            public string Description { get; init; }

            public Func<Task> Action { get; init; }

            private DateTime? _lastLaunch;

            public void UnMarkForRun()
            {
                IsPlanned = false;
            }

            public void MarkForRun()
            {
                if (_lastLaunch != null)
                {
                    if (ScheduleTime.Hours == _lastLaunch.Value.Hour &&
                        ScheduleTime.Minutes == _lastLaunch.Value.Minute &&
                        ScheduleTime.Seconds == _lastLaunch.Value.Second)
                    {
                        Console.WriteLine("Task already started!");
                        Debug.Assert(false, "LastLaunch == ScheduleTime");
                        return;
                    }
                }

                Debug.Assert(!IsPlanned);

                IsPlanned = true;
                _lastLaunch = DateTime.Now;

                Debug.WriteLine($"[{DateTime.Now}] Task '{Description}' will be started!");
            }
        }

        public void Dispose()
        {
            _timer.Elapsed -= TimerOnElapsed;
            _timer.Dispose();

            _tokenSource.Cancel();
            _tokenSource.Dispose();
        }
    }
    
    public struct ScheduleTime
    {
        public int Hours { get; }
        public int Minutes { get; }
        public int Seconds { get; }

        public ScheduleTime(DateTime time)
        {
            Hours = time.Hour;
            Minutes = time.Minute;
            Seconds = time.Second;
        }

        public ScheduleTime(int hours, int minutes, int seconds)
        {
            if (hours <= 0 || hours >= 24) throw new ArgumentOutOfRangeException(nameof(hours));
            if (minutes <= 0 || minutes >= 60) throw new ArgumentOutOfRangeException(nameof(minutes));
            if (seconds <= 0 || seconds >= 60) throw new ArgumentOutOfRangeException(nameof(seconds));

            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
        }
    }
}

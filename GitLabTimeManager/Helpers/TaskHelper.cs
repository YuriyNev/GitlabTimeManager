using System.Diagnostics;
using System.Threading.Tasks;

namespace GitLabTimeManager.Helpers
{
    public static class TaskWatcher
    {
        public static void Watch(this Task task, string failMessage)
        {
            task.ContinueWith(t => Debug.Assert(!t.IsFaulted, failMessage, t.Exception?.ToString() ?? "No exception!"), TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}

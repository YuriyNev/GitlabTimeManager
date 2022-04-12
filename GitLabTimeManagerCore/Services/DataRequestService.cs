using Catel.IoC;

namespace GitLabTimeManager.Services
{
    public interface IDataSubscription : IDisposable
    {
        event EventHandler Requested;
        event EventHandler<GitResponse> NewData;
        event EventHandler<Exception> NewException;
        event EventHandler<string> LoadingStatus;
    }

    public class DataSubscription : IDataSubscription
    {
        public void Dispose()
        {
        }

        public void OnNewData(GitResponse data)
        {
            try
            {
                NewData?.Invoke(this, data);
            }
            catch (Exception e)
            {
                NewException?.Invoke(this, e);

                Console.WriteLine(e);
            }
        }

        public void OnException(Exception exception)
        {
            NewException?.Invoke(this, exception);
        }
        
        public void OnRequested()
        {
            Requested?.Invoke(this, EventArgs.Empty);
        }

        public void OnNewLoadingMessage(string message)
        {
            LoadingStatus?.Invoke(this, message);
        }

        public event EventHandler? Requested;
        public event EventHandler<GitResponse>? NewData;
        public event EventHandler<Exception>? NewException;
        public event EventHandler<string>? LoadingStatus;
    }


    public interface IDataRequestService
    {
        IDataSubscription CreateSubscription();

        void Restart(DateTime start, DateTime end, IReadOnlyList<string> users, IReadOnlyList<string> labels);
    }

    public class DataRequestService : IDataRequestService, IDisposable
    {
        private IReadOnlyList<DataSubscription> _dataSubscriptions = Array.Empty<DataSubscription>();
        private CancellationTokenSource? _cancellation = new();

        public IDataSubscription CreateSubscription()
        {
            var subscription = new DataSubscription();

            _dataSubscriptions = new List<DataSubscription>(_dataSubscriptions) {subscription};

            return subscription;
        }
        
        public void Restart(DateTime start, DateTime end, IReadOnlyList<string> users, IReadOnlyList<string> labels)
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();

            var sourceControl = InitializeSource();
            if (sourceControl == null)
                return;

            foreach (var dataSubscription in _dataSubscriptions)
            {
                dataSubscription.OnRequested();
            }

            RunAsync(sourceControl, start, end, users,labels).ConfigureAwait(true);
        }

        private ISourceControl? InitializeSource()
        {
            try
            {
                return IoCConfiguration.DefaultDependencyResolver.Resolve<ISourceControl>();
            }
            catch (Exception e)
            {
                foreach (var subscription in _dataSubscriptions)
                    subscription.OnException(e);

                Console.WriteLine(e);
            }

            return null;
        }

        private void RequestStatusChanged(string message)
        {
            foreach (var subscription in _dataSubscriptions)
                subscription.OnNewLoadingMessage(message);
        }
        
        private async Task RunAsync(ISourceControl sourceControl, DateTime start, DateTime end, IReadOnlyList<string> users, IReadOnlyList<string> labels)
        {
            var data = await sourceControl.RequestDataAsync(start, end, users, labels ,RequestStatusChanged).ConfigureAwait(true);

            foreach (var subscription in _dataSubscriptions)
            {
                subscription.OnNewData(data);
            }
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
        }
    }
}
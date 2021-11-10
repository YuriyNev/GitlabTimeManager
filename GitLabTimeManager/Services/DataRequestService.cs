using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catel.IoC;
using JetBrains.Annotations;

namespace GitLabTimeManager.Services
{
    public interface IDataSubscription : IDisposable
    {
        event EventHandler Requested;
        event EventHandler<GitResponse> NewData;
        event EventHandler<Exception> NewException;
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

        public event EventHandler Requested;
        public event EventHandler<GitResponse> NewData;
        public event EventHandler<Exception> NewException;
    }


    public interface IDataRequestService
    {
        IDataSubscription CreateSubscription();

        void Restart(DateTime start, DateTime end, IReadOnlyList<string> users);
    }

    public class DataRequestService : IDataRequestService, IDisposable
    {
        private IReadOnlyList<DataSubscription> _dataSubscriptions = Array.Empty<DataSubscription>();
        private CancellationTokenSource _cancellation = new CancellationTokenSource();

        public IDataSubscription CreateSubscription()
        {
            var subscription = new DataSubscription();

            _dataSubscriptions = new List<DataSubscription>(_dataSubscriptions) {subscription};

            return subscription;
        }
        
        public void Restart(DateTime start, DateTime end, IReadOnlyList<string> users)
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

            RunAsync(sourceControl, start, end, users).ConfigureAwait(true);
        }

        private ISourceControl InitializeSource()
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
        
        private async Task RunAsync([NotNull] ISourceControl sourceControl, DateTime start, DateTime end, IReadOnlyList<string> users)
        {
            var data = await sourceControl.RequestDataAsync(start, end, users).ConfigureAwait(true);

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
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

        public event EventHandler<GitResponse> NewData;
        public event EventHandler<Exception> NewException;
    }


    public interface IDataRequestService
    {
        IDataSubscription CreateSubscription();
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

        public DataRequestService()
        {
            var sourceControl = InitializeSource();
            if (sourceControl == null)
                return;
            
            RunAsync(sourceControl).ConfigureAwait(true);
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

        private async Task RunAsync([NotNull] ISourceControl sourceControl)
        {
            //var newestData = await sourceControl.RequestNewestDataAsync().ConfigureAwait(true);
            //foreach (var subscription in _dataSubscriptions)
            //{
            //    subscription.OnNewData(newestData);
            //}

            while (true)
            {
                if (_cancellation.IsCancellationRequested)
                    return;

                var data = await sourceControl.RequestDataAsync().ConfigureAwait(true);

                foreach (var subscription in _dataSubscriptions)
                {
                    subscription.OnNewData(data);
                }
#if DEBUG
                await Task.Delay(1_60_000, _cancellation.Token).ConfigureAwait(true);
#else
                await Task.Delay(2_60_000, _cancellation.Token).ConfigureAwait(true);
#endif
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
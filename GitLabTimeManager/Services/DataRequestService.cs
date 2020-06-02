using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catel.IoC;

namespace GitLabTimeManager.Services
{
    public interface IDataSubscription : IDisposable
    {
        event EventHandler<GitResponse> NewData;
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
                Console.WriteLine(e);
            }
        }

        public event EventHandler<GitResponse> NewData;
    }


    public interface IDataRequestService
    {
        IDataSubscription CreateSubscription();
    }

    public class DataRequestService : IDataRequestService, IDisposable
    {
        private ISourceControl SourceControl { get; }
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
            SourceControl = IoCConfiguration.DefaultDependencyResolver.Resolve<ISourceControl>();

            RunAsync().ConfigureAwait(true);
        }

        private async Task RunAsync()
        {
            while (true)
            {
                if (_cancellation.IsCancellationRequested)
                    return;
                
                var data = await SourceControl.RequestDataAsync().ConfigureAwait(true);

                foreach (var subscription in _dataSubscriptions)
                {
                    subscription.OnNewData(data);
                }
                await Task.Delay(2_60_000, _cancellation.Token).ConfigureAwait(false);
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

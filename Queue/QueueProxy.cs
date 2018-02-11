using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rezgar.Crawler.Queue
{
    public abstract class QueueProxy
    {
        private readonly HashSet<Statuses> _processingStatuses = new HashSet<Statuses>
        {
            Statuses.Inactive,
            Statuses.WaitingForDependencies,
            Statuses.Fetching
        };

        public Statuses Status { get; private set; }
        public event Action<Statuses> StatusChanged;
        private ICollection<QueueProxy> _dependencies;

        protected QueueProxy(params QueueProxy[] dependencies)
        {
            _dependencies = dependencies;

            if (_dependencies.Count > 0)
                Status = Statuses.WaitingForDependencies;
            else
                Status = Statuses.Inactive;
        }

        public async Task MonitorDependenciesAsync()
        {
            // Wait for all dependencies to be fully crawled, then continue
            var notDepletedDependencies = _dependencies?.Where(pred => pred.Status != Statuses.Depleted).ToArray();
            if (notDepletedDependencies != null && notDepletedDependencies.Length > 0)
            {
                // No dependencies, that still have items, not fully processed
                var notDepletedCountdown = new AsyncCountdownEvent(notDepletedDependencies.Length);

                foreach (var dependency in notDepletedDependencies)
                {
                    dependency.StatusChanged += status =>
                    {
                        if (status == Statuses.Depleted)
                        {
                            notDepletedCountdown.Signal();
                        }
                    };
                }

                await notDepletedCountdown.WaitAsync();
            }

            Status = Statuses.Inactive;
        }

        public abstract Task<IList<CrawlingQueueItem>> FetchAsync(int portionSize, CancellationTokenSource cts);


        public Statuses ChangeStatus(Statuses status)
        {
            StatusChanged?.Invoke(status);
            return Status = status;
        }

        public bool IsPending()
        {
            return _processingStatuses.Contains(Status);
        }

        #region Declarations

        public enum Statuses
        {
            Inactive,
            WaitingForDependencies,
            Fetching,
            Depleted,
            Error
        }

        #endregion
    }
}

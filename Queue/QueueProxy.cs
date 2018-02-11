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
        public Statuses Status { get; private set; } = Statuses.Inactive;
        public event Action<Statuses> StatusChanged;
        public abstract Task<IList<CrawlingQueueItem>> FetchAsync(int portionSize, CancellationTokenSource cts);
        
        public Statuses ChangeStatus(Statuses status)
        {
            StatusChanged?.Invoke(status);
            return Status = status;
        }

        #region Declarations

        public enum Statuses
        {
            Inactive,
            Fetching,
            Depleted,
            Error
        }

        #endregion
    }
}

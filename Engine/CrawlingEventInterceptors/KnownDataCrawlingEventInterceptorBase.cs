using Rezgar.Crawler.Configuration;
using Rezgar.Utils.Collections;
using Rezgar.Utils.MessageQueue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rezgar.Crawler.Queue;

namespace Rezgar.Crawler.Engine.CrawlingEventInterceptors
{
    public abstract class KnownDataCrawlingEventInterceptorBase : CrawlingEventInterceptorBase
    {
        // NOTE: We actually use only .Key of concurrent dictionary, since we need a concurrenthashset, which is not available by default in .net ConncurrentCollections namespace
        private readonly ConcurrentDictionary<string, string> _knownData = new ConcurrentDictionary<string, string>();

        private readonly IMessageQueue _dataSyncMessageQueue;
        private System.Timers.Timer _knownDataSyncTimer;

        public KnownDataCrawlingEventInterceptorBase(
            IList<string> knownData,
            IMessageQueue dataSyncMessageQueue
        )
        {
            _knownData = new ConcurrentDictionary<string, string>(knownData.Select(data => new KeyValuePair<string, string>(data, null)));
            _dataSyncMessageQueue = dataSyncMessageQueue;

            if (_dataSyncMessageQueue != null)
                SubscribeToDataUpdates();
        }

        #region Private

        protected bool AddKnownData(string data)
        {
            if (_knownData.TryAdd(data, null))
            {
                if (_dataSyncMessageQueue != null)
                    BroadcastDataAsync(data);

                return true;
            }
            return false;
        }

        private Task BroadcastDataAsync(string data)
        {
            return _dataSyncMessageQueue.SendMessageAsync(
                    new Message(data, Environment.MachineName),
                    TimeSpan.FromMinutes(1)
                );
        }
        
        private void SubscribeToDataUpdates()
        {
            _knownDataSyncTimer = new System.Timers.Timer(TimeSpan.FromSeconds(15).TotalMilliseconds);
            _knownDataSyncTimer.AutoReset = true;
            _knownDataSyncTimer.Elapsed += (sender, args) =>
            {
                var getMessagesTask = _dataSyncMessageQueue.GetMessagesAsync();
                getMessagesTask.Wait();

                if (getMessagesTask.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var message in getMessagesTask.Result)
                    {
                        if (message.Sender != Environment.MachineName)
                        {
                            _knownData.TryAdd(message.Text, null);
                        }
                    }
                }
            };
        }
        
        #endregion

        public override void Dispose()
        {
            if (_knownDataSyncTimer != null)
            {
                _knownDataSyncTimer.Stop();
                _knownDataSyncTimer.Dispose();
            }
        }
    }
}

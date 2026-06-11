using BetfairNG.Data;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace BetfairNG
{
    public class MarketListenerMultiPeriod : MarketListenerBase, IDisposable
    {
        private readonly BetfairClient _client;
        private readonly object _lockObj = new object();

        private readonly ConcurrentDictionary<double, ConcurrentDictionary<string, bool>> _marketPollInterval =
            new ConcurrentDictionary<double, ConcurrentDictionary<string, bool>>();

        private readonly ConcurrentDictionary<double, Poller> _polling =
            new ConcurrentDictionary<double, Poller>();

        private readonly PriceProjection _priceProjection;

        private MarketListenerMultiPeriod(BetfairClient client,
            PriceProjection priceProjection)
        {
            _client = client;
            _priceProjection = priceProjection;
        }

        public static MarketListenerMultiPeriod Create(BetfairClient client,
            PriceProjection priceProjection)
        {
            return new MarketListenerMultiPeriod(client, priceProjection);
        }

        public void Dispose()
        {
            foreach (var poll in _polling)
            {
                poll.Value?.Dispose();
            }
        }

        public IObservable<MarketBook> SubscribeMarketBook(string marketId, double pollIntervalInSeconds)
        {
            return GetOrCreateMarketBookObservable(marketId,
                () => SetupMarketPolling(marketId, pollIntervalInSeconds));
        }

        public IObservable<Runner> SubscribeRunner(string marketId, long selectionId, long pollinterval)
        {
            return CreateRunnerObservable(SubscribeMarketBook(marketId, pollinterval), selectionId);
        }

        public void UpdatePollInterval(string marketId, double newPollIntervalInSeconds)
        {
            if (!Markets.Keys.Contains(marketId)) return;

            lock (_lockObj)
            {
                // First, remove the existing entry
                CleanUpPolling(marketId);
                // Now put this marketId into the new interval
                SetupMarketPolling(marketId, newPollIntervalInSeconds);
            }
        }

        protected override void OnMarketUnsubscribed(string marketId)
        {
            CleanUpPolling(marketId);
        }

        private void CleanUpPolling(string marketId)
        {
            // Find the interval that the market is now running under
            var entry = _marketPollInterval.FirstOrDefault(search => search.Value.Keys.Contains(marketId));
            if (entry.Value == null) return;
            var interval = entry.Key;

            if (_marketPollInterval.TryGetValue(interval, out ConcurrentDictionary<string, bool> mpi))
            {
                mpi.TryRemove(marketId, out bool pi);

                if (!mpi.IsEmpty) return;
                // All the markets have gone for this interval, so clean the interval + polling up as well
                if (_marketPollInterval.TryRemove(interval, out ConcurrentDictionary<string, bool> pis))
                {
                    if (_polling.TryRemove(interval, out Poller poll))
                    {
                        poll.Dispose();
                    }
                }
            }
        }

        private void DoWork(double pollinterval)
        {
            if (!_marketPollInterval.TryGetValue(pollinterval, out ConcurrentDictionary<string, bool> bag)) return;

            try
            {
                Poll(pollinterval, bag);
            }
            catch (Exception ex)
            {
                // a throw here would tear down the Interval subscription and
                // silently stop polling for this interval; OnError the affected subscriptions instead
                foreach (var observer in Observers.Where(k => bag.Keys.Contains(k.Key)))
                    observer.Value.OnError(ex);
            }
        }

        private void Poll(double pollinterval, ConcurrentDictionary<string, bool> bag)
        {
            var book = _client.ListMarketBook(bag.Keys, _priceProjection).Result;

            if (book.HasError)
            {
                foreach (var observer in Observers.Where(k => bag.Keys.Contains(k.Key)))
                {
                    observer.Value.OnError(book.Error);
                }
                return;
            }

            // we may have fresher data than the response to this pollinterval request
            if (!_polling.TryGetValue(pollinterval, out Poller p)) return;

            if (book.RequestStart < p.LatestDataRequestStart && book.LastByte > p.LatestDataRequestFinish)
                return;

            lock (_lockObj)
            {
                p.LatestDataRequestStart = book.RequestStart;
                p.LatestDataRequestFinish = book.LastByte;
            }

            PublishMarketBooks(book.Response);
        }

        private void SetupMarketPolling(string marketId, double pollIntervalInSeconds)
        {
            // Keep the poll interval reasonable...
            if (pollIntervalInSeconds < 0.15) pollIntervalInSeconds = 0.15;

            if (_marketPollInterval.TryGetValue(pollIntervalInSeconds, out ConcurrentDictionary<string, bool> marketIdsForPollInterval))
            {
                marketIdsForPollInterval.TryAdd(marketId, false);
            }
            else
            {
                marketIdsForPollInterval = new ConcurrentDictionary<string, bool>();
                marketIdsForPollInterval.TryAdd(marketId, false);

                _marketPollInterval.TryAdd(pollIntervalInSeconds, marketIdsForPollInterval);
                _polling.TryAdd(pollIntervalInSeconds, new Poller(
                    Observable.Interval(TimeSpan.FromSeconds(pollIntervalInSeconds), NewThreadScheduler.Default)
                        .Subscribe(
                            onNext: l => DoWork(pollIntervalInSeconds)
                            //, onCompleted: TODO: do I need some clean up here?
                        )));
            }
        }
    }

    internal class Poller : IDisposable
    {
        internal DateTime LatestDataRequestFinish = DateTime.Now;
        internal DateTime LatestDataRequestStart = DateTime.Now;
        private readonly IDisposable _poller;

        public Poller(IDisposable poller)
        {
            this._poller = poller;
        }

        public void Dispose()
        {
            _poller.Dispose();
        }
    }
}

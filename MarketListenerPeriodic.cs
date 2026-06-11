using BetfairNG.Data;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace BetfairNG
{
    public class MarketListenerPeriodic : MarketListenerBase, IDisposable
    {
        private readonly BetfairClient _client;
        private readonly object _lockObj = new object();

        private readonly IDisposable _polling;
        private readonly PriceProjection _priceProjection;
        private DateTime _latestDataRequestFinish = DateTime.Now;
        private DateTime _latestDataRequestStart = DateTime.Now;

        private MarketListenerPeriodic(BetfairClient client,
            PriceProjection priceProjection,
            double periodInSec)
        {
            _client = client;
            _priceProjection = priceProjection;

            _polling = Observable.Interval(TimeSpan.FromSeconds(periodInSec),
                                          NewThreadScheduler.Default).Subscribe(l => DoWork());
        }

        public static MarketListenerPeriodic Create(BetfairClient client,
            PriceProjection priceProjection,
            double periodInSec)
        {
            return new MarketListenerPeriodic(client, priceProjection, periodInSec);
        }

        public void Dispose()
        {
            _polling?.Dispose();
        }

        public IObservable<MarketBook> SubscribeMarketBook(string marketId)
        {
            return GetOrCreateMarketBookObservable(marketId);
        }

        public IObservable<Runner> SubscribeRunner(string marketId, long selectionId)
        {
            return CreateRunnerObservable(SubscribeMarketBook(marketId), selectionId);
        }

        private void DoWork()
        {
            try
            {
                Poll();
            }
            catch (Exception ex)
            {
                // a throw here would tear down the Interval subscription and
                // silently stop all polling; OnError the affected subscriptions instead
                foreach (var observer in Observers)
                    observer.Value.OnError(ex);
            }
        }

        private void Poll()
        {
            var book = _client.ListMarketBook(Markets.Keys.ToList(), this._priceProjection).Result;

            if (book.HasError)
            {
                foreach (var observer in Observers)
                    observer.Value.OnError(book.Error);
                return;
            }

            // we may have fresher data than the response to this request
            if (book.RequestStart < _latestDataRequestStart && book.LastByte > _latestDataRequestFinish)
                return;

            lock (_lockObj)
            {
                _latestDataRequestStart = book.RequestStart;
                _latestDataRequestFinish = book.LastByte;
            }

            PublishMarketBooks(book.Response);
        }
    }
}

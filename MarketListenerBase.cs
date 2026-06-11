using BetfairNG.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace BetfairNG
{
    /// <summary>
    /// Shared market/runner subscription management for the market listener variants.
    /// </summary>
    public abstract class MarketListenerBase
    {
        protected readonly ConcurrentDictionary<string, IObservable<MarketBook>> Markets =
            new ConcurrentDictionary<string, IObservable<MarketBook>>();

        protected readonly ConcurrentDictionary<string, IObserver<MarketBook>> Observers =
            new ConcurrentDictionary<string, IObserver<MarketBook>>();

        protected IObservable<MarketBook> GetOrCreateMarketBookObservable(string marketId, Action onCreated = null)
        {
            return Markets.GetOrAdd(marketId, id =>
            {
                var observable = Observable.Create<MarketBook>(
                    (IObserver<MarketBook> observer) =>
                    {
                        Observers.AddOrUpdate(id, observer, (key, existingVal) => existingVal);
                        return Disposable.Create(() =>
                        {
                            Markets.TryRemove(id, out IObservable<MarketBook> o);
                            Observers.TryRemove(id, out IObserver<MarketBook> ob);
                            OnMarketUnsubscribed(id);
                        });
                    })
                    .Publish()
                    .RefCount();

                onCreated?.Invoke();
                return observable;
            });
        }

        protected static IObservable<Runner> CreateRunnerObservable(IObservable<MarketBook> marketTicks, long selectionId)
        {
            return Observable.Create<Runner>(
              (IObserver<Runner> observer) =>
              {
                  var subscription = marketTicks.Subscribe(tick =>
                      {
                          var runner = tick.Runners.First(c => c.SelectionId == selectionId);
                          // attach the book
                          runner.MarketBook = tick;
                          observer.OnNext(runner);
                      });

                  return Disposable.Create(() => subscription.Dispose());
              })
              .Publish()
              .RefCount();
        }

        protected void PublishMarketBooks(IEnumerable<MarketBook> marketBooks)
        {
            foreach (var market in marketBooks)
            {
                if (!Observers.TryGetValue(market.MarketId, out IObserver<MarketBook> o)) continue;

                // check to see if the market is finished
                if (market.Status == MarketStatus.CLOSED ||
                    market.Status == MarketStatus.INACTIVE)
                    o.OnCompleted();
                else
                    o.OnNext(market);
            }
        }

        protected virtual void OnMarketUnsubscribed(string marketId)
        {
        }
    }
}

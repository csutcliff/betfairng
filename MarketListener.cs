using BetfairNG.Data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetfairNG
{
    public class MarketListener : MarketListenerBase
    {
        private static MarketListener listener = null;
        private DateTime lastRequestStart;
        private DateTime latestDataRequestFinish = DateTime.Now;
        private DateTime latestDataRequestStart = DateTime.Now;
        private readonly object lockObj = new object();
        private readonly BetfairClient client;
        private readonly int connectionCount;
        private bool marketAdded = false;

        private readonly PriceProjection priceProjection;
        private readonly int sampleFrequency;
        private readonly int samplePeriod;

        private MarketListener(BetfairClient client,
            PriceProjection priceProjection,
            int connectionCount, int samplePeriod)
        {
            this.client = client;
            this.priceProjection = priceProjection;
            this.connectionCount = connectionCount;
            this.samplePeriod = samplePeriod;
            if (samplePeriod >= 1000)
                this.sampleFrequency = samplePeriod/1000;
            else
                this.sampleFrequency = samplePeriod/100;
            Task.Run(() => PollMarketBooks());
        }

        public static MarketListener Create(BetfairClient client,
            PriceProjection priceProjection,
            int connectionCount, int samplePeriod = 0)
        {
            listener ??= new MarketListener(client, priceProjection, connectionCount, samplePeriod);

            return listener;
        }

        public IObservable<MarketBook> SubscribeMarketBook(string marketId)
        {
            return GetOrCreateMarketBookObservable(marketId, () => marketAdded = true);
        }

        public IObservable<Runner> SubscribeRunner(string marketId, long selectionId)
        {
            return CreateRunnerObservable(SubscribeMarketBook(marketId), selectionId);
        }

        // TODO:// replace this with the Rx scheduler
        private void PollMarketBooks()
        {
            for (int i = 0; i < connectionCount; i++)
            {
                Task.Run(async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                if (Markets.Count > 0)
                                {
                                    // TODO:// look at spinwait or signalling instead of this
                                    while (connectionCount > 1 && DateTime.Now.Subtract(lastRequestStart).TotalMilliseconds < (1000 / connectionCount))
                                    {
                                        int waitMs = (1000 / connectionCount) - (int)DateTime.Now.Subtract(lastRequestStart).TotalMilliseconds;
                                        Thread.Sleep(waitMs > 0 ? waitMs : 0);
                                    }

                                    var stopWatch = new Stopwatch();
                                    stopWatch.Start();

                                    lock (lockObj)
                                        lastRequestStart = DateTime.Now;

                                    var book = client.ListMarketBook(Markets.Keys.ToList(), this.priceProjection).Result;

                                    if (!book.HasError)
                                    {
                                        // we may have fresher data than the response to this request
                                        if (book.RequestStart < latestDataRequestStart && book.LastByte > latestDataRequestFinish)
                                            continue;
                                        else
                                        {
                                            lock (lockObj)
                                            {
                                                latestDataRequestStart = book.RequestStart;
                                                latestDataRequestFinish = book.LastByte;
                                            }
                                        }

                                        PublishMarketBooks(book.Response);
                                    }
                                    else
                                    {
                                        foreach (var observer in Observers)
                                            observer.Value.OnError(book.Error);
                                    }
                                    while (stopWatch.ElapsedMilliseconds < samplePeriod && !marketAdded)
                                    {
                                        await Task.Delay(sampleFrequency);
                                    }
                                    marketAdded = false;
                                }
                                else
                                    // TODO:// will die with rx scheduler
                                    await Task.Delay(500);
                            }
                            catch (Exception ex)
                            {
                                // a failed request must not silently kill the polling loop;
                                // OnError tears down the affected subscriptions
                                foreach (var observer in Observers)
                                    observer.Value.OnError(ex);
                                await Task.Delay(500);
                            }
                        }
                    });
                Thread.Sleep(1000 / connectionCount);
            }
        }
    }
}

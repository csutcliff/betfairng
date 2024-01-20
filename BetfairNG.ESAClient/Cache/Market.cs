using Betfair.ESASwagger.Model;
using System.Collections.Generic;
using System.Linq;

namespace Betfair.ESAClient.Cache
{
    /// <summary>
    /// Thread safe, reference invariant reference to a market.
    /// Repeatedly calling <see cref="Snap"/> will return atomic snapshots of the market.
    /// </summary>
    public class Market(MarketCache marketCache, string marketId)
    {
        private readonly MarketCache _marketCache = marketCache;

        private readonly string _marketId = marketId;

        private readonly Dictionary<RunnerId, MarketRunner> _marketRunners = [];

        private MarketDefinition _marketDefinition;

        private MarketSnap _snap;

        private double _tv;

        /// <summary>
        /// Whether the market is closed.
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return _marketDefinition != null && _marketDefinition.Status == MarketDefinition.StatusEnum.Closed;
            }
        }

        /// <summary>
        /// Market id
        /// </summary>
        public string MarketId
        {
            get
            {
                return _marketId;
            }
        }

        /// <summary>
        /// An atomic snapshot of the state of the market.
        /// </summary>
        public MarketSnap Snap
        {
            get
            {
                return _snap;
            }
        }

        public override string ToString()
        {
            return "Market{" +
                "MarketId=" + MarketId +
                ", MarketDefinition=" + _marketDefinition +
                ", MarketRunners=" + string.Join(", ", _marketRunners.Values) +
                "}";
        }

        internal void OnMarketChange(MarketChange marketChange)
        {
            //initial image means we need to wipe our data
            bool isImage = marketChange.Img == true;

            if (marketChange.MarketDefinition != null)
            {
                //market definition changed
                OnMarketDefinitionChange(marketChange.MarketDefinition);
            }
            if (marketChange.Rc != null)
            {
                //runners changed
                foreach (RunnerChange runnerChange in marketChange.Rc)
                {
                    OnPriceChange(isImage, runnerChange);
                }
            }

            MarketSnap newSnap = new()
            {
                MarketId = _marketId,
                MarketDefinition = _marketDefinition,
                MarketRunners = _marketRunners.Values.Select(runner => runner.Snap).ToList(),
                TradedVolume = Utils.SelectPrice(isImage, ref _tv, marketChange.Tv)
            };
            _snap = newSnap;
        }

        private MarketRunner GetOrAdd(RunnerId rid)
        {
            if (!_marketRunners.TryGetValue(rid, out MarketRunner runner))
            {
                runner = new MarketRunner(this, rid);
                _marketRunners[rid] = runner;
            }
            return runner;
        }

        private void OnMarketDefinitionChange(MarketDefinition marketDefinition)
        {
            _marketDefinition = marketDefinition;
            if (marketDefinition.Runners != null)
            {
                foreach (RunnerDefinition runnerDefinition in marketDefinition.Runners)
                {
                    OnRunnerDefinitionChange(runnerDefinition);
                }
            }
        }

        private void OnPriceChange(bool isImage, RunnerChange runnerChange)
        {
            MarketRunner marketRunner = GetOrAdd(new RunnerId(runnerChange.Id, runnerChange.Hc));
            //update the runner
            marketRunner.OnPriceChange(isImage, runnerChange);
        }

        private void OnRunnerDefinitionChange(RunnerDefinition runnerDefinition)
        {
            MarketRunner marketRunner = GetOrAdd(new RunnerId(runnerDefinition.Id, runnerDefinition.Hc));
            //update runner
            marketRunner.OnRunnerDefinitionChange(runnerDefinition);
        }
    }
}
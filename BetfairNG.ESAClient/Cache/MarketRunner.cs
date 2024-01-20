using Betfair.ESASwagger.Model;

namespace Betfair.ESAClient.Cache
{
    /// <summary>
    /// Represents a market runner within a market
    /// </summary>
    public class MarketRunner(Market market, RunnerId runnerId)
    {
        private readonly PriceSizeLadder _atbPrices = PriceSizeLadder.NewBack();

        private readonly PriceSizeLadder _atlPrices = PriceSizeLadder.NewLay();

        // Full depth Ladders
        private readonly LevelPriceSizeLadder _batbPrices = new();

        private readonly LevelPriceSizeLadder _batlPrices = new();

        private readonly LevelPriceSizeLadder _bdatbPrices = new();

        private readonly LevelPriceSizeLadder _bdatlPrices = new();

        private readonly Market _market = market;

        private readonly RunnerId _runnerId = runnerId;

        private readonly PriceSizeLadder _spbPrices = PriceSizeLadder.NewBack();

        private readonly PriceSizeLadder _splPrices = PriceSizeLadder.NewLay();

        private readonly PriceSizeLadder _trdPrices = PriceSizeLadder.NewLay();

        private double _ltp;

        private RunnerDefinition _runnerDefinition;

        // Level / Depth Based Ladders
        private MarketRunnerPrices _runnerPrices = MarketRunnerPrices.EMPTY;

        private MarketRunnerSnap _snap;

        private double _spf;

        // special prices
        private double _spn;

        private double _tv;

        public RunnerId RunnerId
        {
            get
            {
                return _runnerId;
            }
        }

        /// <summary>
        /// Takes or returns an existing immutable snap of the runner.
        /// </summary>
        public MarketRunnerSnap Snap
        {
            get
            {
                _snap ??= new MarketRunnerSnap()
                {
                    RunnerId = RunnerId,
                    Definition = _runnerDefinition,
                    Prices = _runnerPrices
                };
                return _snap;
            }
        }

        public override string ToString()
        {
            return "MarketRunner{" +
                    "runnerId=" + _runnerId +
                    ", prices=" + _runnerPrices +
                    ", runnerDefinition=" + _runnerDefinition +
                    '}';
        }

        internal void OnPriceChange(bool isImage, RunnerChange runnerChange)
        {
            //snap is invalid
            _snap = null;

            MarketRunnerPrices newPrices = new()
            {
                AvailableToLay = _atlPrices.OnPriceChange(isImage, runnerChange.Atl),
                AvailableToBack = _atbPrices.OnPriceChange(isImage, runnerChange.Atb),
                Traded = _trdPrices.OnPriceChange(isImage, runnerChange.Trd),
                StartingPriceBack = _spbPrices.OnPriceChange(isImage, runnerChange.Spb),
                StartingPriceLay = _splPrices.OnPriceChange(isImage, runnerChange.Spl),

                BestAvailableToBack = _batbPrices.OnPriceChange(isImage, runnerChange.Batb),
                BestAvailableToLay = _batlPrices.OnPriceChange(isImage, runnerChange.Batl),
                BestDisplayAvailableToBack = _bdatbPrices.OnPriceChange(isImage, runnerChange.Bdatb),
                BestDisplayAvailableToLay = _bdatlPrices.OnPriceChange(isImage, runnerChange.Bdatl),

                StartingPriceNear = Utils.SelectPrice(isImage, ref _spn, runnerChange.Spn),
                StartingPriceFar = Utils.SelectPrice(isImage, ref _spf, runnerChange.Spf),
                LastTradedPrice = Utils.SelectPrice(isImage, ref _ltp, runnerChange.Ltp),
                TradedVolume = Utils.SelectPrice(isImage, ref _tv, runnerChange.Tv)
            };

            //copy on write
            _runnerPrices = newPrices;
        }

        internal void OnRunnerDefinitionChange(RunnerDefinition runnerDefinition)
        {
            //snap is invalid
            _snap = null;

            _runnerDefinition = runnerDefinition;
        }
    }
}
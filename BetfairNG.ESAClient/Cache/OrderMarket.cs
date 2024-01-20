using Betfair.ESASwagger.Model;
using System.Collections.Generic;
using System.Linq;

namespace Betfair.ESAClient.Cache
{
    /// <summary>
    /// The cached state of the market
    /// </summary>
    public class OrderMarket(OrderCache orderCache, string marketId)
    {
        private readonly string _marketId = marketId;
        private readonly Dictionary<RunnerId, OrderMarketRunner> _marketRunners = [];
        private readonly OrderCache _orderCache = orderCache;
        private OrderMarketSnap _snap;

        public bool IsClosed { get; private set; }

        public string MarketId
        {
            get
            {
                return _marketId;
            }
        }

        /// <summary>
        /// Takes or returns an existing immutable snap of the market.
        /// </summary>
        public OrderMarketSnap Snap
        {
            get
            {
                return _snap;
            }
        }

        public override string ToString()
        {
            return "OrderMarket{" +
                "MarketId=" + MarketId +
                ", Runners=" + string.Join(", ", _marketRunners.Values) +
                "}";
        }

        internal void OnOrderMarketChange(OrderMarketChange orderMarketChange)
        {
            OrderMarketSnap newSnap = new()
            {
                MarketId = _marketId
            };

            //update runners
            if (orderMarketChange.Orc != null)
            {
                //runners changed
                foreach (OrderRunnerChange orderRunnerChange in orderMarketChange.Orc)
                {
                    OnOrderRunnerChange(orderRunnerChange);
                }
            }
            newSnap.OrderMarketRunners = _marketRunners.Values.Select(omr => omr.Snap);

            //update closed
            IsClosed = orderMarketChange.Closed == true;
            newSnap.IsClosed = IsClosed;

            _snap = newSnap;
        }

        private void OnOrderRunnerChange(OrderRunnerChange orderRunnerChange)
        {
            RunnerId rid = new(orderRunnerChange.Id, orderRunnerChange.Hc);
            if (!_marketRunners.TryGetValue(rid, out OrderMarketRunner orderMarketRunner))
            {
                orderMarketRunner = new OrderMarketRunner(this, rid);
                _marketRunners[rid] = orderMarketRunner;
            }
            //update the runner
            orderMarketRunner.OnOrderRunnerChange(orderRunnerChange);
        }
    }
}
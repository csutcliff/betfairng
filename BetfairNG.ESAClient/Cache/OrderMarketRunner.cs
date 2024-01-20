using Betfair.ESASwagger.Model;
using System.Collections.Generic;

namespace Betfair.ESAClient.Cache
{
    /// <summary>
    /// Cached state of the runner
    /// </summary>
    public class OrderMarketRunner(OrderMarket market, RunnerId runnerId)
    {
        private readonly OrderMarket _market = market;
        private readonly RunnerId _runnerId = runnerId;

        private readonly PriceSizeLadder _matchedBack = PriceSizeLadder.NewBack();
        private readonly PriceSizeLadder _matchedLay = PriceSizeLadder.NewLay();
        private OrderMarketRunnerSnap _snap;
        private readonly Dictionary<string, Order> _unmatchedOrders = [];

        public OrderMarket Market
        {
            get
            {
                return _market;
            }
        }

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
        public OrderMarketRunnerSnap Snap
        {
            get
            {
                return _snap;
            }
        }

        public override string ToString()
        {
            return _snap == null ? "null" : _snap.ToString();
        }

        internal void OnOrderRunnerChange(OrderRunnerChange orderRunnerChange)
        {
            bool isImage = orderRunnerChange.FullImage == true;

            if (isImage)
            {
                //image so clear down
                _unmatchedOrders.Clear();
            }

            if (orderRunnerChange.Uo != null)
            {
                //have order changes
                foreach (Order order in orderRunnerChange.Uo)
                {
                    _unmatchedOrders[order.Id] = order;
                }
            }

            OrderMarketRunnerSnap newSnap = new()
            {
                RunnerId = _runnerId,
                UnmatchedOrders = new Dictionary<string, Order>(_unmatchedOrders),

                MatchedLay = _matchedLay.OnPriceChange(isImage, orderRunnerChange.Ml),
                MatchedBack = _matchedBack.OnPriceChange(isImage, orderRunnerChange.Mb)
            };

            _snap = newSnap;
        }
    }
}
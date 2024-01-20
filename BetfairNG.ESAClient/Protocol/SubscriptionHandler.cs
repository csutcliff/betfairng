using Betfair.ESASwagger.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Betfair.ESAClient.Protocol
{
    /// <summary>
    /// Generic subscription handler for change messages:
    /// 1) Tracks clocks to facilitate resubscripiton
    /// 2) Provides useful timings for initial image
    /// 3) Supports the ability to re-combine segmented messages to retain event level atomicity
    /// </summary>
    /// <typeparam name="S">Subscription request message type</typeparam>
    /// <typeparam name="C">Change message type</typeparam>
    /// <typeparam name="I">Change message item type</typeparam>
    public class SubscriptionHandler<S, C, I>(int id, S subscriptionMessage, bool isMergeSegments) where C : ChangeMessage<I> where S : RequestMessage
    {
        private readonly bool _isMergeSegments = isMergeSegments;
        private bool _isSubscribed;
        private int _itemCount;
        private List<I> _mergedChanges;
        private readonly TaskCompletionSource<bool> _subscriptionComplete = new();
        private readonly int _subscriptionId = id;
        private readonly Stopwatch _ttfm = Stopwatch.StartNew();
        private readonly Stopwatch _ttlm = Stopwatch.StartNew();

        public string Clk { get; private set; }

        public long? ConflationMs { get; private set; }

        public TimeSpan HeartbeatInterval { get; private set; }

        public long? HeartbeatMs { get; private set; }

        public string InitialClk { get; private set; }

        public bool IsSubscribed { get; private set; } = false;

        public DateTime LastArrivalTime { get; private set; }

        public long? LastPt { get; private set; }

        public Task<bool> SubscriptionComplete
        {
            get
            {
                return _subscriptionComplete.Task;
            }
        }

        public S SubscriptionMessage { get; private set; } = subscriptionMessage;

        public C ProcessChangeMessage(C changeMessage)
        {
            if (_subscriptionId != changeMessage.Id)
            {
                //previous subscription id - ignore
                return null;
            }

            //Every message store timings
            LastPt = changeMessage.Pt;
            LastArrivalTime = changeMessage.ArrivalTime;

            if (changeMessage.IsStartOfRecovery)
            {
                //Start of recovery
                _ttfm.Stop();
                Trace.TraceInformation("{0}: Start of image", typeof(S).Name);
            }

            if (changeMessage.ChangeType == ChangeType.HEARTBEAT)
            {
                //Swallow heartbeats
                changeMessage = null;
            }
            else if (changeMessage.SegmentType != SegmentType.NONE && _isMergeSegments)
            {
                //Segmented message and we're instructed to merge (which makes segments look atomic).
                changeMessage = MergeMessage(changeMessage);
            }

            if (changeMessage != null)
            {
                //store clocks
                if (changeMessage.InitialClk != null)
                {
                    InitialClk = changeMessage.InitialClk;
                }
                if (changeMessage.Clk != null)
                {
                    Clk = changeMessage.Clk;
                }

                if (!_isSubscribed)
                {
                    //During recovery
                    if (changeMessage.Items != null)
                    {
                        _itemCount += changeMessage.Items.Count;
                    }
                }

                if (changeMessage.IsEndOfRecovery)
                {
                    //End of recovery
                    _isSubscribed = true;
                    HeartbeatMs = changeMessage.HeartbeatMs;
                    HeartbeatInterval = TimeSpan.FromMilliseconds((double)HeartbeatMs);
                    ConflationMs = changeMessage.ConflateMs;
                    _ttlm.Stop();
                    Trace.TraceInformation("{0}: End of image: type:{6}, ttfm:{1}, ttlm:{2}, conflation:{3}, heartbeat:{4}, change.items:{5}",
                        typeof(S).Name,
                        _ttfm.Elapsed,
                        _ttlm.Elapsed,
                        ConflationMs,
                        HeartbeatMs,
                        _itemCount,
                        changeMessage.ChangeType);

                    //unwind future
                    _subscriptionComplete.TrySetResult(true);
                }
            }
            return changeMessage;
        }

        internal void Cancel()
        {
            //unwind waiters
            _subscriptionComplete.TrySetCanceled();
        }

        private C MergeMessage(C changeMessage)
        {
            //merge segmented messages so client sees atomic view across segments
            if (changeMessage.SegmentType == SegmentType.SEG_START)
            {
                //start merging
                _mergedChanges = [];
            }
            //accumulate
            _mergedChanges.AddRange(changeMessage.Items);

            if (changeMessage.SegmentType == SegmentType.SEG_END)
            {
                //finish merging
                changeMessage.SegmentType = SegmentType.NONE;
                changeMessage.Items = _mergedChanges;
                _mergedChanges = null;
            }
            else
            {
                //swallow message as we're still merging
                changeMessage = null;
            }
            return changeMessage;
        }
    }
}
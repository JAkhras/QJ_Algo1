using System;

namespace QJExternalTool
{
    internal sealed class Trade
    {
        public int Position { get; }
        public decimal OpenPrice { get; }
        public decimal ClosePrice { get; set; }
        public DateTime OpenedAt { get; }
        public DateTime ClosedAt { get; set; }
        public decimal Drawdown { get; set; }

        public Trade(decimal lastPrice, int lots)
        {
            OpenPrice = lastPrice;
            Position = lots;

            OpenedAt = DateTime.Now;
        }

    }
}

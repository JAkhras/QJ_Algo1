using System;
using QJInterface;

namespace QJExternalTool
{
    class Trade
    {

        public int Position { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public decimal Drawdown { get; set; }

        //Also need excel file info

        public Trade(decimal lastPrice, int lots, char side)
        {
            OpenPrice = lastPrice;
            Position = lots;

            if (side == '2')
                Position *= -1;

            OpenedAt = DateTime.Now;

        }

    }
}

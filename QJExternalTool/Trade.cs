using System;

namespace QJExternalTool
{
    class Trade
    {

        public int Position { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public int Symbol { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public decimal Drawdown { get; set; }

        public void Update()
        {
            
        }

        public void Save()
        {
            
        }



    }
}

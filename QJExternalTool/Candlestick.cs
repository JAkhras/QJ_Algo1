using System;

namespace QJExternalTool
{
    public class Candlestick
    {

        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public bool IsNull { get; set; }
        public int Year { get; set; }

        public Candlestick()
        {
            IsNull = true;
            Open = -1;
            Close = -1;
            High = -1;
            Low = -1;
            Year = DateTime.Now.Year;

        }

        public void Update(decimal last)
        {

            if (IsNull)
            {
                IsNull = false;
                Open = last;
                Low = last;
            }

            Close = last;
            if (last > High)
                High = last;
            if (last < Low)
                Low = last;

        }
    }
}
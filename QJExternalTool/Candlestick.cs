namespace QJExternalTool
{
    public sealed class Candlestick
    {
        internal decimal Open { get; set; }
        internal decimal Close { get; set; }
        internal decimal High { get; set; }
        internal decimal Low { get; set; }
        internal bool IsNull { get; set; }

        internal Candlestick()
        {
            IsNull = true;
            Open = -1;
            Close = -1;
            High = -1;
            Low = -1;
        }

        internal void Update(decimal last)
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
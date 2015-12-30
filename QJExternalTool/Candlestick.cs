namespace QJExternalTool
{
    public class Candlestick
    {

        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public bool IsNull { get; set; }
        public int Frequency { get; set; }

        public Candlestick(int frequency)
        {
            IsNull = true;
            Frequency = frequency;
            Open = -1;
            Close = -1;
            High = -1;
            Low = -1;
        }

        public void Update(decimal last)
        {
            IsNull = false;
            Close = last;
            if (last > High)
                High = last;
            if (last < Low)
                Low = last;

        }
    }
}
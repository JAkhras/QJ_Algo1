using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QJInterface;
using Excel = Microsoft.Office.Interop.Excel;
using Timer = System.Timers.Timer;

namespace QJExternalTool
{
    public class CandlestickChart
    {
        public enum Point
        {
            Open, Close, High, Low
        }

        public enum CandleFrequency
        {
            Candles5, Candles15, Candles60
        }

        public enum Signals
        {
            Buy, Sell, None
        }

        public List<Candlestick> Candlesticks5 { get; }
        public List<Candlestick> Candlesticks15 { get; }
        public List<Candlestick> Candlesticks60 { get; }

        private int _frequency;

        public Candlestick CurrentCandlestick5 { get; private set; }
        public Candlestick CurrentCandlestick15 { get; private set; }
        public Candlestick CurrentCandlestick60 { get; private set; }

        private readonly int _fastLength;
        private readonly int _slowLength;

        public decimal HighAtSignal { get; private set; }
        public decimal LowAtSignal { get; private set; }

        public Signals Signal { get; private set; }

        public decimal Fast { get; private set; }

        private TextBox _box;

        private int _lastVolume;

        private decimal _slow;
        public decimal Slow
        {
            get { return _slow; }
            private set
            {
                _slow = value;

                var crossedUp = Fast > value && _lastFast < _lastSlow;
                var crossedDown = Fast < value && _lastFast > _lastSlow;

                if (crossedUp || crossedDown)
                {
                    HighAtSignal = High(CandleFrequency.Candles5, 1);
                    LowAtSignal = Low(CandleFrequency.Candles5, 1);
                    Signal = crossedUp ? Signals.Buy : Signals.Sell;

                }

                _lastFast = Fast;
                _lastSlow = value;
            }
        }

        private decimal _lastFast;
        private decimal _lastSlow;

        private readonly ILevel1 _level1;

        public CandlestickChart(string product, int timerInterval, int fastLength, int slowLength, ILevel1 level1, TextBox box)
        {

            
            _box = box;
            _level1 = level1;

            _lastVolume = _level1.Volume;

            _fastLength = fastLength;
            _slowLength = slowLength;


            Candlesticks5 = new List<Candlestick>();
            Candlesticks15 = new List<Candlestick>();
            Candlesticks60 = new List<Candlestick>();

            var excelApp = new Excel.Application {Visible = true};

            var excelWorkbook = excelApp.Workbooks.Open(product,
        0, false, 5, "", "", false, Excel.XlPlatform.xlWindows, "",
        true, false, 0, true, false, false);

            var excelSheets = excelWorkbook.Worksheets;

            var excelWorksheet = (Excel.Worksheet)excelSheets.Item["ES"];

            var excelRange = excelWorksheet.Range["K3", "N29"];

            for (var i = 1; i <= _slowLength; ++i)
            {

                var candlestick5 = new Candlestick()
                {
                    IsNull = false,
                    High = (decimal) ((Excel.Range) excelRange.Cells[i, 1]).Value2,
                    Low = (decimal) ((Excel.Range) excelRange.Cells[i, 3]).Value2,
                    Open = (decimal) ((Excel.Range) excelRange.Cells[i, 4]).Value2,
                    Close = (decimal) ((Excel.Range) excelRange.Cells[i, 2]).Value2
                };

                Candlesticks5.Add(candlestick5);
            }

            excelWorkbook.Close();
            excelApp.Quit();

            ReleaseObject(excelWorksheet);
            ReleaseObject(excelWorkbook);
            ReleaseObject(excelApp);

            Candlesticks5.Reverse();

            _frequency = 0;

            CurrentCandlestick5 = new Candlestick();
            CurrentCandlestick15 = new Candlestick();
            CurrentCandlestick60 = new Candlestick();

            Fast = AverageLast(Point.Close, _fastLength, CandleFrequency.Candles5);
            Slow = AverageLast(Point.Close, _fastLength, CandleFrequency.Candles5);

            //Set up timer
            var timer = new Timer
            {
                Interval = timerInterval,
                Enabled = false,
                AutoReset = true
            };

            timer.Elapsed += TimerOnTick;

            Signal = Signals.None;
            timer.Start();

        }

        private static void ReleaseObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
            }
            catch
            {
                // ignored
            }
            finally
            {
                GC.Collect();
            }
        }


        private void TimerOnTick(object sender, EventArgs eventArgs)
        {

            _frequency += 5;

            if (!CurrentCandlestick5.IsNull)
                Candlesticks5.Add(CurrentCandlestick5);
            CurrentCandlestick5 = new Candlestick();


            if (_frequency % 15 == 0)
            {
                if (!CurrentCandlestick15.IsNull)
                    Candlesticks15.Add(CurrentCandlestick15);
                CurrentCandlestick15 = new Candlestick();

            }

            if (_frequency != 60) return;
            if (!CurrentCandlestick60.IsNull)
                Candlesticks60.Add(CurrentCandlestick60);
            CurrentCandlestick60 = new Candlestick();

            _frequency = 0;
        }

        public List<Candlestick> Last(int n, CandleFrequency candleFrequency)
        {

            if (n < 1)
                return null;

            Candlestick currentCandlestick;

 

            var candlesticks = new List<Candlestick>();

            List<Candlestick> candlesticksFrequency;

            switch (candleFrequency)
            {
                case CandleFrequency.Candles5:
                    candlesticksFrequency = Candlesticks5;
                    currentCandlestick = CurrentCandlestick5;
                    break;
                case CandleFrequency.Candles15:
                    candlesticksFrequency = Candlesticks15;
                    currentCandlestick = CurrentCandlestick15;
                    break;
                case CandleFrequency.Candles60:
                    candlesticksFrequency = Candlesticks60;
                    currentCandlestick = CurrentCandlestick60;
                    break;
                default:
                    return null;
            }

            if (!currentCandlestick.IsNull)
                n--;

            var count = candlesticksFrequency.Count;

            for (var i = count - n; i < candlesticksFrequency.Count; ++i)
                candlesticks.Add(candlesticksFrequency[i]);

            if (!currentCandlestick.IsNull)
                candlesticks.Add(currentCandlestick);

            return candlesticks;
        }

        public decimal AverageLast(Point point, int n, CandleFrequency candleFrequency)
        {
            var candlesticks = Last(n, candleFrequency);

            switch (point)
            {
                case Point.Open:
                    return candlesticks.Sum(candlestick => candlestick.Open)/candlesticks.Count;
                case Point.Close:
                    return candlesticks.Sum(candlestick => candlestick.Close)/candlesticks.Count;
                case Point.High:
                    return candlesticks.Sum(candlestick => candlestick.High)/candlesticks.Count;
                case Point.Low:
                    return candlesticks.Sum(candlestick => candlestick.Low)/candlesticks.Count;
                default:
                    return 0;
            }
        }

        public decimal High(CandleFrequency candleFrequency, int n)
        {
            switch (candleFrequency)
            {
                case CandleFrequency.Candles5:
                    return (Candlesticks5[Candlesticks5.Count - n].High);
                case CandleFrequency.Candles15:
                    return (Candlesticks15[Candlesticks15.Count - n].High);
                case CandleFrequency.Candles60:
                    return (Candlesticks60[Candlesticks60.Count - n].High);
                default:
                    return 0;
            }
        }

        public decimal Low(CandleFrequency candleFrequency, int n)
        {
            switch (candleFrequency)
            {
                case CandleFrequency.Candles5:
                    return (Candlesticks5[Candlesticks5.Count - n].Low);
                case CandleFrequency.Candles15:
                    return (Candlesticks15[Candlesticks15.Count - n].Low);
                case CandleFrequency.Candles60:
                    return (Candlesticks60[Candlesticks60.Count - n].Low);
                default:
                    return 0;
            }
        }

        public decimal Open(CandleFrequency candleFrequency, int n)
        {
            switch (candleFrequency)
            {
                case CandleFrequency.Candles5:
                    return (Candlesticks5[Candlesticks5.Count - n].Open);
                case CandleFrequency.Candles15:
                    return (Candlesticks15[Candlesticks15.Count - n].Open);
                case CandleFrequency.Candles60:
                    return (Candlesticks60[Candlesticks60.Count - n].Open);
                default:
                    return 0;
            }
        }

        public decimal Close(CandleFrequency candleFrequency, int n)
        {
            switch (candleFrequency)
            {
                case CandleFrequency.Candles5:
                    return (Candlesticks5[Candlesticks5.Count - n].Close);
                case CandleFrequency.Candles15:
                    return (Candlesticks15[Candlesticks15.Count - n].Close);
                case CandleFrequency.Candles60:
                    return (Candlesticks60[Candlesticks60.Count - n].Close);
                default:
                    return 0;
            }
        }

        public void Update()
        {

            var volume = _level1.Volume;

            if (volume == _lastVolume) return;

            _lastVolume = volume;
            var last = _level1.Last;

            CurrentCandlestick5.Update(last);
            CurrentCandlestick15.Update(last);
            CurrentCandlestick60.Update(last);

            Fast = AverageLast(Point.Close, _fastLength, CandleFrequency.Candles5);
            Slow = AverageLast(Point.Close, _slowLength, CandleFrequency.Candles5);
        }


    }
}
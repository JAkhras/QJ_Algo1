using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using Timer = System.Timers.Timer;

namespace QJExternalTool
{
    internal sealed class CandlestickChart
    {
        internal enum Point
        {
            Open, Close, High, Low
        }

        internal List<Candlestick> Candlesticks { get; }

        private Candlestick CurrentCandlestick { get; set; }

        private int _lastVolume;

        private readonly Timer _timer;

        private readonly ILevel1 _level1;

        internal decimal MarketConditionCoefficient { get; }

        internal CandlestickChart(string file, string sheet, string topLeftCorner, string bottomRightCorner, int timerInterval, int slowLength, ILevel1 level1, TextBox box)
        {
            _level1 = level1;

            _lastVolume = _level1.Volume;

            Candlesticks = new List<Candlestick>();

            var excelApp = new Excel.Application {Visible = true};

            var excelWorkbook = excelApp.Workbooks.Open(file,
                                0, false, 5, "", "", false, Excel.XlPlatform.xlWindows, "",
                                true, false, 0, true, false, false);

            var excelSheets = excelWorkbook.Worksheets;

            var excelWorksheet = (Excel.Worksheet)excelSheets.Item[sheet];

            //notice the cell numbers, change them as required
            var excelRange = excelWorksheet.Range[topLeftCorner, bottomRightCorner];

            for (var i = 1; i <= slowLength; ++i)
                Candlesticks.Add(new Candlestick()
                {
                    IsNull = false,
                    High = (decimal) ((Excel.Range) excelRange.Cells[i, 1]).Value2,
                    Low = (decimal) ((Excel.Range) excelRange.Cells[i, 3]).Value2,
                    Open = (decimal) ((Excel.Range) excelRange.Cells[i, 4]).Value2,
                    Close = (decimal) ((Excel.Range) excelRange.Cells[i, 2]).Value2
                });

            excelRange = excelWorksheet.Range["Y2", "Y2"];
            MarketConditionCoefficient = (decimal) ((Excel.Range) excelRange.Cells[1,1]).Value2;

            excelWorkbook.Close();
            excelApp.Quit();

            ReleaseObject(excelWorksheet);
            ReleaseObject(excelWorkbook);
            ReleaseObject(excelApp);

            Candlesticks.Reverse();

            CurrentCandlestick = new Candlestick();

            //Set up timer
            _timer = new Timer
            {
                Interval = timerInterval,
                Enabled = false,
                AutoReset = true
            };

            _timer.Elapsed += TimerOnTick;
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
            if (!CurrentCandlestick.IsNull)
                Candlesticks.Add(CurrentCandlestick);
            CurrentCandlestick = new Candlestick();
        }

        private List<Candlestick> Last(int n)
        {
            if (n < 1)
                return null;

            var lastCandlesticks = new List<Candlestick>();

            if (!CurrentCandlestick.IsNull)
                n--;

            var count = Candlesticks.Count;

            for (var i = count - n; i < count; ++i)
                lastCandlesticks.Add(Candlesticks[i]);

            if (!CurrentCandlestick.IsNull)
                lastCandlesticks.Add(CurrentCandlestick);

            return lastCandlesticks;
        }

        internal decimal AverageLast(Point point, int n)
        {
            var candlesticks = Last(n);

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

        internal decimal High(int n) => Candlesticks[Candlesticks.Count - n].High;

        internal decimal Low(int n) => (Candlesticks[Candlesticks.Count - n].Low);

        internal void Update()
        {

            var volume = _level1.Volume;

            if (volume == _lastVolume) return;

            _lastVolume = volume;
            var last = _level1.Last;

            CurrentCandlestick.Update(last);
        }

        internal void Start() => _timer.Start();
    }
}
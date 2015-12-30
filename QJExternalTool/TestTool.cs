#region using
using System;
using System.Windows.Forms;
using QJInterface;
#endregion

namespace QJExternalTool
{
	public partial class TestTool : Form
	{
		#region Constants
		public const string OrderTypeLimit = "LIMIT";
		public const string OrderTypeMarket = "MARKET";
		public const string OrderTypeStop = "STOP";
		public const string OrderTypeStopLimit = "STOP LIMIT";
		public const string OrderTypeMarketOnClose = "MARKET ON CLOSE";
		public const string OrderTypeWithOrWithout = "WITH OR WITHOUT";
		public const string OrderTypeLimitOrBetter = "LIMIT OR BETTER";
		public const string OrderTypeLimitWithOrWithout = "LIMIT WITH OR WITHOUT";
		public const string OrderTypeOnBasis = "ON BASIS";
		#endregion

		#region Variables
		public delegate void UpdateLabelDelegate(Label label, string strValue);

	    private readonly IHost _host;

        private int _lastVolume;

	    //Candlestick stuff
        private const string Product = "/ES H6.CME";

        private readonly Timer _timer;
	    private int _frequency;

        private Candlestick _currentCandlestick5;
	    private Candlestick _currentCandlestick15;
	    private Candlestick _currentCandlestick60;

	    private readonly CandlestickChart _candlestickChart;

        //Algo variables
	    private decimal _fast;
	    private decimal _slow;
	    private decimal _buyStop;
	    private decimal _buyLimit;
	    private decimal _sellStop;
	    private decimal _sellLimit;

        //Algo inputs
	    private const int TimeStart = 9;
	    private const int TimeDuration = 14;
	    private const int FastLength = 9;
	    private const int SlowLength = 27;

	    private const decimal Point = 0.25m;

        #endregion

        #region Constructor
        public TestTool(IHost host)
		{
            InitializeComponent();

			_host = host;

            var level1 = _host.GetLevel1(Product);
            level1.Level1Changed += m_level1_Level1Changed;

            _candlestickChart = new CandlestickChart(Product);

            //Set up candlestick
            _lastVolume = level1.Volume;
            _frequency = 0;

            _currentCandlestick5 = new Candlestick(5);
            _currentCandlestick15 = new Candlestick(15);
            _currentCandlestick60 = new Candlestick(60);

            //Set up timer
            _timer = new Timer { Interval = 300000 };
            _timer.Tick += TimerOnTick;


            //Set up algo variables
            _fast = 0;
            _slow = 0;
            _buyStop = 0;
            _buyLimit = 0;
            _sellStop = 0;
            _sellLimit = 0;

		}

	    private void TimerOnTick(object sender, EventArgs eventArgs)
	    {
	        _frequency += 5;

            if (_currentCandlestick5.IsNull)
                _candlestickChart.Candlesticks5.RemoveAt(_candlestickChart.Candlesticks5.Count-1);
	        tbxAll.Text += "\n" + _currentCandlestick5;
            _currentCandlestick5 = new Candlestick(5);
            _candlestickChart.Candlesticks5.Add(_currentCandlestick5);
            _candlestickChart.NewCandlesticks.Add(_currentCandlestick5);

	        if (_frequency%15 == 0)
	        {
                if (_currentCandlestick15.IsNull)
                    _candlestickChart.Candlesticks15.RemoveAt(_candlestickChart.Candlesticks15.Count - 1);
                _currentCandlestick15 = new Candlestick(15);
                _candlestickChart.Candlesticks15.Add(_currentCandlestick15);
                _candlestickChart.NewCandlesticks.Add(_currentCandlestick15);
            }

	        if (_frequency == 60)
	        {
                if (_currentCandlestick60.IsNull)
                    _candlestickChart.Candlesticks60.RemoveAt(_candlestickChart.Candlesticks60.Count - 1);
                _currentCandlestick60 = new Candlestick(60);
                _candlestickChart.Candlesticks60.Add(_currentCandlestick60);
                _candlestickChart.NewCandlesticks.Add(_currentCandlestick60);
                _frequency = 0;
	        }

            Algorithm();

            if (DateTime.Now.Hour < 16) return;
	        _timer.Stop();
	        _candlestickChart.Save();
	    }

	    #endregion

		#region m_level1_Level1Changed()
		// This function is call when the information in the SymbolLevel1 changed.
		void m_level1_Level1Changed(ILevel1 level1)
		{

            if (!_timer.Enabled)
                _timer.Start();

		    var volume = level1.Volume;

            //updating candlestick
		    if (_lastVolume == volume) return;
		    _lastVolume = volume;
		    var last = level1.Last;
		    _currentCandlestick5.Update(last);
		    _currentCandlestick15.Update(last);
		    _currentCandlestick60.Update(last);
		}

	    private void Algorithm()
	    {
	        var high = _candlestickChart.High(CandlestickChart.CandleFrequency.Candles5, 1);
            var close = _candlestickChart.Close(CandlestickChart.CandleFrequency.Candles5, 1);
            var low = _candlestickChart.Low(CandlestickChart.CandleFrequency.Candles5, 1);

            var time = DateTime.Now.Hour;

	        if (time < TimeStart || time >= TimeStart + TimeDuration) return;

	        _fast = _candlestickChart.AverageLast(CandlestickChart.Point.Close, FastLength,
	            CandlestickChart.CandleFrequency.Candles5);
	        _slow = _candlestickChart.AverageLast(CandlestickChart.Point.Close, SlowLength,
	            CandlestickChart.CandleFrequency.Candles5);

	        if (_fast > _slow)
	        {
	            _buyStop = high + Point;
	            _buyLimit = high + 5*Point;
	            if (close < _buyLimit)
	            {
	                //buy;
	                _host.CreateOrder(Product, SideEnum.BUY, 10, _buyLimit, TimeInForceEnum.GTC, OrderTypeMarket);
	            }
	        }

	        else if (_fast < _slow)
	        {
	            _sellStop = low - Point;
	            _sellLimit = low - 5 * Point;
	            if (close > _sellLimit)
	            {
	                //sell;
	                _host.CreateOrder(Product, SideEnum.SELL, 10, _sellLimit, TimeInForceEnum.GTC, OrderTypeMarket);
	            }
	        }
	    }

        #endregion
    }
}

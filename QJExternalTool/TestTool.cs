#region using
using System;
using System.Text;
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
	    private readonly ILevel1 _level1;
	    private IEOrder _order;
	    private readonly IPosition _position;

        private OrderStateEnum _orderState;

        private decimal _lastPrice;

	    private bool _canCheckForTrailingStop;

	    private decimal _highestBid;
	    private decimal _lowestBid;
	    private decimal _highestAsk;
	    private decimal _lowestAsk;

	    private readonly StringBuilder _stringBuilder;

	    private decimal _highAtSignal;
	    private decimal _lowAtSignal;

        private enum Signals
        {
            Buy, Sell, None
        }

	    private Signals _signal;

	    private decimal _fast;
        private decimal _slow;

        private decimal _lastFast;
        private decimal _lastSlow;

        private readonly int _maxDrawdown;
        private readonly int _dollarProfitTarget;
        private readonly int _earned;
        private readonly int _percentDown;

        //Candlestick stuff
        private readonly CandlestickChart _candlestickChart5;

        //Algo inputs
        private const string Product = "/ES H6.CME";
        private const string File = @"Z:\DATA.ES+EC1.1.xlsm";

        private const int TimeStart = 19;
	    private const int TimeDuration = 4;
	    private const int FastLength = 9;
	    private const int SlowLength = 27;

	    private const int TimerInterval = 300000;

	    private const decimal Point = 0.25m;
	    private const decimal DollarPointValue = 12.5m;

        private const int MaxDrawdown = 50;
        private const int DollarProfitTarget = 100;
        private const int Earned = 50;
        private const int PercentDown = 25;

        private const int Lots = 1;

	    private Trade _trade;

	    private string _trend;

        #endregion

        #region Constructor
        public TestTool(IHost host)
        {
            _highestBid = 0;
            _lowestBid = decimal.MaxValue;
            _highestAsk = 0;
            _lowestAsk = decimal.MaxValue;

            #region OUTPUT
            _stringBuilder = new StringBuilder(); //OUTPUT RELATED
            #endregion

            InitializeComponent();

			_host = host;

            _position = _host.GetPosition(Product);
            _level1 = _host.GetLevel1(Product);

            //initialize candlestickCharts here
            //notice the first 4 values are the excel file, sheet, upper right corner of values and lower left corner of values
            //we always need 4 columns: High, Last, Low, Open. If High is column K, then we need the last column to be N
            //the number of the first cell should be the High of the first value you want.
            //the number of the last cell will end up being (the amount of candlesticks you want + the number of the first cell - 1)
            //DON'T FUCK THIS UP 
            _candlestickChart5 = new CandlestickChart(File, "ES", "K3", "N29", TimerInterval, SlowLength, _level1, tbxAll);

            var marketConditionCoefficient = _candlestickChart5.MarketConditionCoefficient;

            _maxDrawdown = (int) Math.Round(MaxDrawdown*marketConditionCoefficient);
            _dollarProfitTarget = (int) Math.Round(DollarProfitTarget*marketConditionCoefficient);
            _earned = (int) Math.Round(Earned*marketConditionCoefficient);
            _percentDown = (int) Math.Round(PercentDown*marketConditionCoefficient);

            #region OUTPUT
            tbxAll.AppendText("\r\nCoefficient: " + marketConditionCoefficient.ToString("F"));
            tbxAll.AppendText("\r\nPoint: " + Point);
            tbxAll.AppendText("\r\nMax drawdown: " + _maxDrawdown);
            tbxAll.AppendText("\r\nDollar profit target: " + _dollarProfitTarget);
            tbxAll.AppendText("\r\nEarned: " + _earned);
            tbxAll.AppendText("\r\nPercent down: " + _percentDown);
            #endregion

            _fast = _candlestickChart5.AverageLast(CandlestickChart.Point.Close, FastLength);
            _slow = _candlestickChart5.AverageLast(CandlestickChart.Point.Close, SlowLength);

            _signal = Signals.None;
            _orderState = OrderStateEnum.FILLED;

            _candlestickChart5.Start();

            _level1.Level1Changed += Level1_Level1Changed;

        }

	    #endregion

		#region Level1_Level1Changed()

		// This function is call when the information in the SymbolLevel1 changed.
	    private void Level1_Level1Changed(ILevel1 level1)
	    {
            #region OUTPUT
            _stringBuilder.Clear();
            _stringBuilder.Append("\r\nVolume: " + level1.Volume + "\r\n");
            #endregion

            var bid = level1.Bid;
	        var ask = level1.Ask;

            if (bid > _highestBid)
                _highestBid = bid;
	        if (bid < _lowestBid)
	            _lowestBid = bid;
	        if (ask > _highestAsk)
	            _highestAsk = ask;
	        if (ask < _lowestAsk)
	            _lowestAsk = ask;

            //Update candlestickCharts here
            _candlestickChart5.Update();

            _fast = _candlestickChart5.AverageLast(CandlestickChart.Point.Close, FastLength);
            _slow = _candlestickChart5.AverageLast(CandlestickChart.Point.Close, SlowLength);

            var crossedUp = _fast > _slow && _lastFast < _lastSlow;
            var crossedDown = _fast <_slow && _lastFast > _lastSlow;

            if (crossedUp || crossedDown)
            {
                _highAtSignal = _candlestickChart5.High(1);
                _lowAtSignal = _candlestickChart5.Low(1);
                _signal = crossedUp ? Signals.Buy : Signals.Sell;

                #region OUTPUT
                _trend = crossedUp ? "Up Trend" : "Down Trend";
                #endregion OUTPUT

            }

            _lastFast = _fast;
            _lastSlow = _slow;

            #region OUTPUT
            var currentTradeOutput = "No current trade";
            #endregion

            if (_trade != null)
	        {
                if (_trade.Position > 0)
	                _trade.Drawdown = (_lastPrice - _lowestBid)*Point*DollarPointValue;
                else if (_trade.Position < 0)
                {
                    _trade.Drawdown = (_highestAsk - _lastPrice)*Point*DollarPointValue;
                }

                #region OUTPUT
                currentTradeOutput = "Current Trade - Position: " + _trade.Position + " Open Price: " + _trade.OpenPrice +
	                                  " @ " + _trade.OpenedAt + " | Drawdown: " + _trade.Drawdown;
                #endregion
            }

            #region OUTPUT
            _stringBuilder.Append(currentTradeOutput);
            #endregion

	        if (_orderState == OrderStateEnum.FILLED)
	        {
                Algorithm();
                if (_position.NetVolume != 0)
	                CheckStops();
	        }

	        #region OUTPUT
            txbAccounts.Text = _stringBuilder.ToString();
            #endregion

        }
        #endregion

        //Algorithm
        private void Algorithm()
	    {
            #region OUTPUT
            _stringBuilder.Append("\r\nAlgo running");
            #endregion

            if (_candlestickChart5.Candlesticks.Count < SlowLength)
	            return;

            var time = DateTime.Now.Hour;

	        if (time < TimeStart || time >= TimeStart + TimeDuration) return;

            #region OUTPUT

            _stringBuilder.Append("\r\nFast: " + _fast.ToString("F"));
            _stringBuilder.Append("\r\nSlow: " + _slow .ToString("F"));

            if (_fast > _slow)
                _stringBuilder.Append("\r\nTrend: Up");
            else if (_fast < _slow)
                _stringBuilder.Append("\r\nTrend: Down");
            else
                _stringBuilder.Append("\r\nTrend: None");

            _stringBuilder.Append("\r\n" + _trend);
            _stringBuilder.Append("\r\nSignal: " + _signal);

            #endregion

            var orderSize = Lots + Lots*(_position.NetVolume == 0 ? 0 : 1);

            switch (_signal)
            {
                case Signals.Buy:

                    var buyStop = _highAtSignal - 4*Point;
                    var buyLimit = _highAtSignal + 5*Point;

                    #region OUTPUT

                    _stringBuilder.Append("\r\nAsk: " + _level1.Ask + " >= " + " Buy Stop: " + buyStop + " && Buy Limit: " + buyLimit);

                    #endregion

                    //buy;
                    if (_position.NetVolume > 0 || _level1.Ask < buyStop || _level1.Ask >= buyLimit) return;

                    //go in new position
                    Buy(orderSize, buyStop, OrderTypeMarket);

                    #region OUTPUT

                    tbxAll.AppendText("\r\nBOT " + orderSize + " at " + _lastPrice + " @ " + DateTime.Now);

                    #endregion

                    break;

                case Signals.Sell:

                    var sellStop = _lowAtSignal + 4*Point;
                    var sellLimit = _lowAtSignal - 5 * Point;

                    #region OUTPUT
                    _stringBuilder.Append("\r\nBid: " + _level1.Bid + " <= " + " Sell Stop: " + sellStop + " && Sell Limit: " + sellLimit);
                    #endregion

                    //sell;
                    if (_position.NetVolume < 0 || _level1.Bid > sellStop || _level1.Bid <= sellLimit) return;

                    Sell(orderSize, sellStop, OrderTypeMarket);

                    #region OUTPUT
                    tbxAll.AppendText("\r\nSLD " + orderSize + " at " + _lastPrice + " @ " + DateTime.Now);
                    #endregion
                    break;

                case Signals.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
	    }

        #region Stops
        private void CheckStops()
	    {
	        CheckStopLoss();
	        CheckPercentTrailing();
	        CheckProfitTarget();
	    }

	    private void CheckProfitTarget()
	    {

	        var longProfitTarget = _lastPrice + _dollarProfitTarget*Point;
	        var shortProfitTarget = _lastPrice - _dollarProfitTarget*Point;

            #region OUTPUT
            _stringBuilder.Append("\r\nWill hit PROFIT TARGET at: " + (_position.NetVolume > 0 ? longProfitTarget : shortProfitTarget));
            #endregion

            if (_position.NetVolume > 0 && _level1.Bid >= longProfitTarget)
	        {
	            Sell(Lots, _level1.Bid, OrderTypeMarket);
                #region OUTPUT
                tbxAll.AppendText("\r\nProfit Target SLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                #endregion
            }
	        else if (_position.NetVolume < 0 && _level1.Ask <= shortProfitTarget)
	        {
                Buy(Lots, _level1.Ask, OrderTypeMarket);
                #region OUTPUT
                tbxAll.AppendText("\r\nProfit Target BOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                #endregion
            }
	    }

        private void CheckPercentTrailing()
        {

            var longEarned = _lastPrice + _earned*Point;
            var shortEarned = _lastPrice - _earned*Point;

            if (_position.NetVolume > 0 && _level1.Bid >= longEarned ||
                             _position.NetVolume < 0 && _level1.Ask <= shortEarned)
                _canCheckForTrailingStop = true;

            if (!_canCheckForTrailingStop)
            {
                #region OUTPUT
                _stringBuilder.Append("\r\nWill activate trailing stop at: " + (_position.NetVolume > 0 ? longEarned : shortEarned));
                #endregion
                return;
            }

            if (_position.NetVolume > 0)
            {
                var stop = _highestBid - _percentDown * Point;

                #region OUTPUT
                _stringBuilder.Append("\r\nWill get out of LONG position at Trailing Stop: " + stop );
                #endregion OUTPUT

                if (_level1.Bid > stop) return;
                Sell(Lots, _level1.Bid, OrderTypeMarket);

                #region OUTPUT
                tbxAll.AppendText("\r\nTrailing Stop SLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                #endregion OUTPUT
            }


            else if (_position.NetVolume < 0)
            {
                var stop = _lowestAsk + _percentDown * Point;
                #region OUTPUT
                _stringBuilder.Append("\r\nWill get out of SHORT position at Trailing Stop: " + stop);
                #endregion OUTPUT
                if (_level1.Ask < stop) return;
                Buy(Lots, _level1.Ask, OrderTypeMarket);
                #region OUTPUT
                tbxAll.AppendText("\r\nTrailing Stop BOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                #endregion
            }

        }

        private void CheckStopLoss()
        {
            var longStop = _lastPrice - _maxDrawdown * Point;
            var shortStop = _lastPrice + _maxDrawdown * Point;

            if (_position.NetVolume > 0)
            {
                #region OUTPUT
                _stringBuilder.Append("\r\nWill get out of LONG position at Stop Loss: " + longStop);
                #endregion

                if (_level1.Bid > longStop) return;
                Sell(Lots, _level1.Bid, OrderTypeMarket);

                #region OUTPUT
                tbxAll.AppendText("\r\nStop Loss SLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                #endregion
            }
            else if (_position.NetVolume < 0)
            {
                #region OUTPUT
                _stringBuilder.Append("\r\nWill get out of SHORT position at Stop Loss: " + shortStop);
                #endregion

                if (_level1.Ask < shortStop) return;
                Buy(Lots, _level1.Ask, OrderTypeMarket);

                #region OUTPUT
                tbxAll.AppendText("\r\nStop Loss BOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                #endregion
            }
        }
        #endregion

        #region Ordering functions
        private void Buy(int orderSize, decimal price, string orderType) => CreateOrder(SideEnum.BUY, orderSize, price, orderType);

	    private void Sell(int orderSize, decimal price, string orderType) => CreateOrder(SideEnum.SELL, orderSize, price, orderType);

	    private void CreateOrder(SideEnum sideEnum, int size, decimal price, string orderType)
	    {
            _orderState = OrderStateEnum.NO_ORDER;
            _canCheckForTrailingStop = false;
            _signal = Signals.None;

	        _order = _host.CreateOrder(Product, sideEnum, size, price, TimeInForceEnum.GTC, orderType);

	        if (_order == null)
	            return;

	        _order.ExecutionReport += Order_ExecutionReport;
	        _order.Anonymous = "Y"; // values = "Y" or "N"
	        _order.Send();
	    }

	    private void Order_ExecutionReport(long toolId, ExecutionReport execReport)
	    {
	        // Informations available in the execReport object.
	        //decimal price = (decimal)execReport.Price; // Price per share.
	        //decimal averagePrice = (decimal)execReport.AvgPrice; // Calculated average price of all fills on this order.
	        var lastPrice = (decimal) execReport.LastPx; // Price of this (last) fill.
	        int orderQuantity = (int)execReport.OrderQty; // Number of shares ordered.
	        //int leavesQty = (int)execReport.LeavesQty; // Amount of shares open for further execution. LeavesQty = OrderQty - CumQty.
	        //int lastQuantity = (int)execReport.LastQty; // Quantity of shares bought/sold on this (last) fill.
	        //int cummulativeQuantity = (int)execReport.CumQty; // Total number of shares filled.
	        //string clientOrderID = execReport.ClOrdID; // This is the long version of the client orderID. exemple: AMP1-39554-12
	        char side = execReport.Side; // '1'=Buy, '2'=Sell, '3'=Buy Minus, '4'=Sell Plus, '5'=Sell Short, '6'=Sell Short Exempt, '7'=Undisclosed, '8'=Cross, '9'=Cross Short.
	        //string text = execReport.Text; // Suplementary information.

	        switch (execReport.OrdStatus)
	        {

	                #region Filled

	            case '2': // Filled
	                // Order completely filled, no remaining quantity.


	                if (_trade != null)
	                {
	                    _trade.ClosePrice = lastPrice;
	                    _trade.ClosedAt = DateTime.Now;

                        textBox1.AppendText("TRADE: Position: " + _trade.Position + " | Open Price: " + _trade.OpenPrice +
                                      " @ " + _trade.OpenedAt + " | Close Price: " + _trade.ClosePrice + " @ " + _trade.ClosedAt + " | Drawdown: " + _trade.Drawdown + "\r\n");

                        _trade = null;

	                    if (orderQuantity > Lots)
	                        _trade = new Trade(lastPrice, Lots, side);
	                }
	                else
	                    _trade = new Trade(lastPrice, Lots, side);

	                _lastPrice = lastPrice;
	                _highestBid = _level1.Bid;
	                _lowestAsk = _level1.Ask;

                    _orderState = OrderStateEnum.FILLED;

                    break;

	                #endregion

	        }
	    }
        #endregion

    }
}

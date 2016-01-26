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

        //Candlestick stuff
        private readonly CandlestickChart _candlestickChart;

        //Algo inputs
        private const string Product = "/ES H6.CME";
        private const string File = @"Z:\DATA.ES+EC1.1.xlsm";

        private const int TimeStart = 9;
	    private const int TimeDuration = 14;
	    private const int FastLength = 9;
	    private const int SlowLength = 27;

	    private const int TimerInterval = 300000;

	    private decimal _point;

        private const int MaxDrawdown = 17;
        private const int DollarProfitTarget = 40;
        private const int Earned = 20;
	    private const int PercentDown = 25;

        private const int Lots = 1;

	    private Trade _trade;

        #endregion

        #region Constructor
        public TestTool(IHost host)
		{

            _stringBuilder = new StringBuilder();

            InitializeComponent();

			_host = host;

            _position = _host.GetPosition(Product);
            _level1 = _host.GetLevel1(Product);

            _point = _level1.Tick;

            _candlestickChart = new CandlestickChart(File, TimerInterval, SlowLength, _level1, tbxAll);

            _fast = _candlestickChart.AverageLast(CandlestickChart.Point.Close, FastLength, CandlestickChart.CandleFrequency.Candles5);
            _slow = _candlestickChart.AverageLast(CandlestickChart.Point.Close, SlowLength, CandlestickChart.CandleFrequency.Candles5);

            _signal = Signals.None;

            _candlestickChart.Start();

            _level1.Level1Changed += Level1_Level1Changed;

        }

	    #endregion

		#region Level1_Level1Changed()

		// This function is call when the information in the SymbolLevel1 changed.
	    private void Level1_Level1Changed(ILevel1 level1)
	    {
            _stringBuilder.Clear();

            _stringBuilder.Append("\r\nVolume:" + level1.Volume + "\r\n");

            var bid = level1.Bid;
	        var ask = level1.Ask;

            if (bid > _highestBid)
                _highestBid = bid;

	        if (ask < _lowestAsk)
	            _lowestAsk = ask;

            _candlestickChart.Update();

            _fast = _candlestickChart.AverageLast(CandlestickChart.Point.Close, FastLength, CandlestickChart.CandleFrequency.Candles5);
            _slow = _candlestickChart.AverageLast(CandlestickChart.Point.Close, SlowLength, CandlestickChart.CandleFrequency.Candles5);

            var crossedUp = _fast > _slow && _lastFast < _lastSlow;
            var crossedDown = _fast <_slow && _lastFast > _lastSlow;

            if (crossedUp || crossedDown)
            {
                _highAtSignal = _candlestickChart.High(CandlestickChart.CandleFrequency.Candles5, 1);
                _lowAtSignal = _candlestickChart.Low(CandlestickChart.CandleFrequency.Candles5, 1);
                _signal = crossedUp ? Signals.Buy : Signals.Sell;

            }

            _lastFast = _fast;
            _lastSlow = _slow;

	        if (_trade != null)
	        {
	            _trade.Drawdown = (_highestBid - _lastPrice)*_point;
	            _stringBuilder.Append("Current Trade - Position: " + _trade.Position + " Open Price: " + _trade.OpenPrice +
	                                  " @ " + _trade.OpenedAt + " | Drawdown: " + _trade.Drawdown);
	        }
	        else
	            _stringBuilder.Append("No current trade");

	        Algorithm();
            CheckStops();

	        txbAccounts.Text = _stringBuilder.ToString();

	    }
        #endregion


        //Algorithm
        private void Algorithm()
	    {
	        _stringBuilder.Append("\r\nAlgo running");
            if (_candlestickChart.Candlesticks5.Count < SlowLength)
	            return;

            var time = DateTime.Now.Hour;

	        if (time < TimeStart || time >= TimeStart + TimeDuration) return;

            _stringBuilder.Append("\r\nFast: " + _fast.ToString("F"));
            _stringBuilder.Append("\r\nSlow: " + _slow .ToString("F"));

            if (_fast > _slow)
                _stringBuilder.Append("\r\nTrend: Up");
            else if (_fast < _slow)
                _stringBuilder.Append("\r\nTrend: Down");
            else
                _stringBuilder.Append("\r\nTrend: None");

            _stringBuilder.Append("\r\nSignal: " + _signal);

            //var orderSize = Lots + Lots*(_position.NetVolume == 0 ? 0 : 1);

            switch (_signal)
            {
                case Signals.Buy:

                    _stringBuilder.Append("\r\nUp Trend");

                    var buyStop = _highAtSignal - 4*_point;
                    var buyLimit = _highAtSignal + 5*_point;

                    _stringBuilder.Append("\r\nAsk: " + _level1.Ask + " >= " + " Buy Stop: " + buyStop + " && Buy Limit: " + buyLimit);

                    //buy;
                    if (_position.NetVolume > 0 || _level1.Ask < buyStop || _level1.Ask >= buyLimit) return;

                    //close existing position if any
                    if (_trade != null)
                        Buy(Lots, buyStop, OrderTypeMarket);

                    //go in new position
                    Buy(Lots, buyStop, OrderTypeMarket);
                    tbxAll.AppendText("\r\nBOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
                    break;

                case Signals.Sell:

                    _stringBuilder.Append("\r\nDown Trend");

                    var sellStop = _lowAtSignal + 4*_point;
                    var sellLimit = _lowAtSignal - 5 * _point;

                    _stringBuilder.Append("\r\nBid: " + _level1.Bid + " <= " + " Sell Stop: " + sellStop + " && Sell Limit: " + sellLimit);

                    //sell;
                    if (_position.NetVolume < 0 || _level1.Bid > sellStop || _level1.Bid <= sellLimit) return;

                    if (_trade != null)
                        Sell(Lots, sellStop, OrderTypeMarket);

                    Sell(Lots, sellStop, OrderTypeMarket);
                    tbxAll.AppendText("\r\nSLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
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
	        if (_position.NetVolume > 0 && _level1.Bid >= _lastPrice + DollarProfitTarget*_point)
	        {
	            Sell(Lots, _level1.Bid, OrderTypeMarket);
                tbxAll.AppendText("\r\nProfit Target SLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now + "Maxdrawdown:" + ((_lastPrice - _lowestAsk) * _point));
            }
	        else if (_position.NetVolume < 0 && _level1.Ask <= _lastPrice - DollarProfitTarget*_point)
	        {
                Buy(Lots, _level1.Ask, OrderTypeMarket);
                tbxAll.AppendText("\r\nProfit Target BOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now + "Maxdrawdown:" + ((_highestBid - _lastPrice) * _point));
            }
	    }

        private void CheckPercentTrailing()
        {
            if (_position.NetVolume > 0 && _level1.Bid >= _lastPrice + Earned * _point ||
                             _position.NetVolume < 0 && _level1.Ask >= _lastPrice - Earned * _point)
                _canCheckForTrailingStop = true;

            if (!_canCheckForTrailingStop) return;


            if (_position.NetVolume > 0)
             

            {
                var stop = _highestBid - PercentDown * _point;
                _stringBuilder.Append("\r\nWill get out of LONG position at Trailing Stop: " + stop );
                if (_level1.Bid > stop) return;
                Sell(Lots, _level1.Bid, OrderTypeMarket);
                tbxAll.AppendText("\r\nTrailing Stop SLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
            }


            else if (_position.NetVolume < 0)
            {
                var stop = _lowestAsk + PercentDown * _point;
                _stringBuilder.Append("\r\nWill get out of SHORT position at Trailing Stop: " + stop);
                if (_level1.Ask < stop) return;
                Buy(Lots, _level1.Ask, OrderTypeMarket);
                tbxAll.AppendText("\r\nTrailing Stop BOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);

            }

        }

        private void CheckStopLoss()
        {
            var longStop = _lastPrice - MaxDrawdown * _point;
            var shortStop = _lastPrice + MaxDrawdown * _point;


            if (_position.NetVolume > 0)
            {

                _stringBuilder.Append("\r\nWill get out of LONG position at Stop Loss: " + longStop);

                if (_level1.Bid > longStop) return;
                Sell(Lots, _level1.Bid, OrderTypeMarket);
                tbxAll.AppendText("\r\nStop Loss SLD " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
            }
            else if (_position.NetVolume < 0)
            {

                _stringBuilder.Append("\r\nWill get out of SHORT position at Stop Loss: " + shortStop);

                if (_level1.Ask < shortStop) return;
                Buy(Lots, _level1.Ask, OrderTypeMarket);
                tbxAll.AppendText("\r\nStop Loss BOT " + Lots + " at " + _lastPrice + " @ " + DateTime.Now);
            }
        }
        #endregion

        #region Ordering functions
        private void Buy(int orderSize, decimal price, string orderType) => CreateOrder(SideEnum.BUY, orderSize, price, orderType);

	    private void Sell(int orderSize, decimal price, string orderType) => CreateOrder(SideEnum.SELL, orderSize, price, orderType);

	    private void CreateOrder(SideEnum sideEnum, int size, decimal price, string orderType)
	    {
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
	        //int orderQuantity = (int)execReport.OrderQty; // Number of shares ordered.
	        //int leavesQty = (int)execReport.LeavesQty; // Amount of shares open for further execution. LeavesQty = OrderQty - CumQty.
	        //int lastQuantity = (int)execReport.LastQty; // Quantity of shares bought/sold on this (last) fill.
	        //int cummulativeQuantity = (int)execReport.CumQty; // Total number of shares filled.
	        //string clientOrderID = execReport.ClOrdID; // This is the long version of the client orderID. exemple: AMP1-39554-12
	        char side = execReport.Side; // '1'=Buy, '2'=Sell, '3'=Buy Minus, '4'=Sell Plus, '5'=Sell Short, '6'=Sell Short Exempt, '7'=Undisclosed, '8'=Cross, '9'=Cross Short.
	        //string text = execReport.Text; // Suplementary information.

	        switch (execReport.OrdStatus)
	        {
	                #region New

	            case '0': // New
	                // Outstanding order with no executions.
	                _orderState = OrderStateEnum.NEW;
	                break;

	                #endregion

	                #region Partially filled

	            case '1': // Partially filled
	                // Outstanding order with executions and remaining quantity.
	                _orderState = OrderStateEnum.PARTIALLY_FILLED;
	                break;

	                #endregion

	                #region Filled

	            case '2': // Filled
	                // Order completely filled, no remaining quantity.
	                _orderState = OrderStateEnum.FILLED;

	                if (_trade != null)
	                {
	                    _trade.ClosePrice = lastPrice;
	                    _trade.ClosedAt = DateTime.Now;
	                    textBox1.AppendText("TRADE: Position: " + _trade.Position + " Open Price: " + _trade.OpenPrice +
                                      " @ " + _trade.OpenedAt + " | Close Price: " + _trade.ClosePrice + " @ " + _trade.ClosedAt + " | Drawdown: " + _trade.Drawdown);
	                    _trade = null;
	                }
	                else
	                    _trade = new Trade(lastPrice, Lots, side);

	                _lastPrice = lastPrice;
	                _highestBid = _level1.Bid;
	                _lowestAsk = _level1.Ask;
	                break;

	                #endregion

	                #region Done for day

	            case '3': // Done for day
	                // Order not, or partially, filled;  no further executions forthcoming for the trading day.
	                _orderState = OrderStateEnum.DONE_FOR_THE_DAY;
	                break;

	                #endregion

	                #region Canceled

	            case '4': // Canceled
	                // Canceled order with or without executions.
	                _orderState = OrderStateEnum.CANCELED;
	                break;

	                #endregion

	                #region Replaced

	            case '5': // Replaced
	                // Replaced order with or without executions.
	                _orderState = OrderStateEnum.REPLACE;
	                break;

	                #endregion

	                #region Pending Cancel

	            case '6': // Pending Cancel
	                // Order with an Order Cancel Request pending, used to confirm receipt of an Order Cancel Request.
	                // DOES NOT INDICATE THAT THE ORDER HAS BEEN CANCELED.
	                _orderState = OrderStateEnum.PENDING_CANCEL;
	                break;

	                #endregion

	                #region Stopped

	            case '7': // Stopped
	                // Order has been stopped at the exchange. Used when guranteeing or protecting a price and quantity.
	                _orderState = OrderStateEnum.STOPPED;
	                break;

	                #endregion

	                #region Rejected

	            case '8': // Rejected
	                // Order has been rejected by broker.
	                // NOTE:  An order can be rejected subsequent to order acknowledgment,
	                // i.e. an order can pass from New to Rejected status.
	                _orderState = OrderStateEnum.REJECTED;
	                break;

	                #endregion

	                #region Suspended

	            case '9': // Suspended
	                // Order has been placed in suspended state at the request of the client.
	                _orderState = OrderStateEnum.SUSPENDED;
	                break;

	                #endregion

	                #region Pending New

	            case 'A': // Pending New
	                // Order has been received by brokers system but not yet accepted for execution.
	                // An execution message with this status will only be sent in response to a Status Request message.
	                _orderState = OrderStateEnum.PENDING_NEW;
	                break;

	                #endregion

	                #region Calculated

	            case 'B': // Calculated
	                // Order has been completed for the day (either filled or done for day).
	                // Commission or currency settlement details have been calculated and reported in this execution message.
	                _orderState = OrderStateEnum.CALCULATED;
	                break;

	                #endregion

	                #region Expired

	            case 'C': // Expired
	                // Order has been canceled in broker's system due to time in force instructions.
	                _orderState = OrderStateEnum.EXPIRED;
	                break;

	                #endregion

	                #region Accepted for bidding

	            case 'D': // Accepted for bidding
	                // Order has been received and is being evaluated for pricing.
	                // It is anticipated that this status will only be  used with the "Disclosed" BidType List Order Trading model.
	                _orderState = OrderStateEnum.ACCEPTED_FOR_BIDDING;
	                break;

	                #endregion

	                #region Pending Replace

	            case 'E': // Pending Replace
	                // Order with an Order Cancel/Replace Request pending, used to confirm receipt of an Order Cancel/Replace Request.
	                // DOES NOT INDICATE THAT THE ORDER HAS BEEN REPLACED
	                _orderState = OrderStateEnum.PENDING_REPLACE;
	                break;

                #endregion
                default:
                    throw new ArgumentOutOfRangeException();

	        }
	    }
        #endregion

    }
}

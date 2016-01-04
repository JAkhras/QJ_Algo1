﻿#region using
using System;
using System.Windows.Forms;
using QJInterface;
using Timer = System.Timers.Timer;

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

        private int _lastVolume;

	    private SideEnum _side;

	    //Candlestick stuff
        private const string Product = "/ES H6.CME";

	    private int _frequency;

        private Candlestick _currentCandlestick5;
	    private Candlestick _currentCandlestick15;
	    private Candlestick _currentCandlestick60;

	    private readonly CandlestickChart _candlestickChart;

        //Algo inputs
	    private const int TimeStart = 18;
	    private const int TimeDuration = 5;
	    private const int FastLength = 9;
	    private const int SlowLength = 27;

	    private const int TimerInterval = 300000;

	    private const decimal Point = 0.25m;

        private const int MaxDrawdown = 17;
        private const int DollarProfitTarget = 40;
        private const int Earned = 8;
	    private const int PercentDown = 16;

        private const int Lots = 1;
        private const TimeInForceEnum TimeInForce = TimeInForceEnum.GTC;

        #endregion

        #region Constructor
        public TestTool(IHost host)
		{
            InitializeComponent();

			_host = host;

            _position = _host.GetPosition(Product);

            _level1 = _host.GetLevel1(Product);
            _level1.Level1Changed += Level1_Level1Changed;

            _candlestickChart = new CandlestickChart("ES_H6_CME");

            //Set up candlestick
            _lastVolume = _level1.Volume;
            _frequency = 0;

            _currentCandlestick5 = new Candlestick(5);
            _currentCandlestick15 = new Candlestick(15);
            _currentCandlestick60 = new Candlestick(60);

            _side = SideEnum.UNDISCLOSED;

            //Set up timer
            var timer = new Timer
            {
                Interval = TimerInterval,
                Enabled = false,
                AutoReset = true
            };
            timer.Elapsed += TimerOnTick;
            timer.Start();

        }

	    private void TimerOnTick(object sender, EventArgs eventArgs)
	    {

            _candlestickChart.Save(tbxAll);

            if (DateTime.Now.Hour >= 16)
	        {
	            _candlestickChart.Save(tbxAll);
	        }

	        _frequency += 5;

	        if (!_currentCandlestick5.IsNull)
	        {
                _candlestickChart.Candlesticks5.Add(_currentCandlestick5);
                _candlestickChart.NewCandlesticks.Add(_currentCandlestick5);
            }
            _currentCandlestick5 = new Candlestick(5);


	        if (_frequency%15 == 0)
	        {
	            if (!_currentCandlestick15.IsNull)
	            {
                    _candlestickChart.Candlesticks15.Add(_currentCandlestick15);
                    _candlestickChart.NewCandlesticks.Add(_currentCandlestick15);
                }
                _currentCandlestick15 = new Candlestick(15);

            }

	        if (_frequency == 60)
	        {
	            if (!_currentCandlestick60.IsNull)
	            {
                    _candlestickChart.Candlesticks60.Add(_currentCandlestick60);
                    _candlestickChart.NewCandlesticks.Add(_currentCandlestick60);
                }
                _currentCandlestick60 = new Candlestick(60);

                _frequency = 0;
	        }

            Algorithm();


	    }

	    #endregion

		#region Level1_Level1Changed()

		// This function is call when the information in the SymbolLevel1 changed.
	    private void Level1_Level1Changed(ILevel1 level1)
		{

		    var volume = level1.Volume;

            //updating candlestick
		    if (_lastVolume == volume) return;
		    _lastVolume = volume;
            var last = level1.Last;
		    _currentCandlestick5.Update(last);
		    _currentCandlestick15.Update(last);
		    _currentCandlestick60.Update(last);

            if (_position.NetVolume != 0)
	            CheckStops(level1);

		}

	    private void CheckStops(ILevel1 level1)
	    {
	        CheckStopLoss(level1);
	        CheckPercentTrailing(level1);
	        CheckProfitTarget(level1);
	    }

	    private void CheckProfitTarget(ILevel1 level1)
	    {
            if (_position.NetVolume > 0 && level1.Bid >= _lastPrice + DollarProfitTarget * Point)
            {
                tbxAll.AppendText("\r\nProfit Target SELL");
                Sell(Lots, level1.Bid, OrderTypeMarket);
            }
            else if (_position.NetVolume < 0 && level1.Ask < _lastPrice - DollarProfitTarget * Point)
            {
                tbxAll.AppendText("\r\nProfit Target BUY");
                Buy(Lots, level1.Ask, OrderTypeMarket);
            }
        }

	    private void CheckPercentTrailing(ILevel1 level1)
	    {
	        if (_position.NetVolume > 0 && level1.Bid >= _lastPrice + Earned*Point)
	        {
	            var stop = _currentCandlestick5.High - PercentDown*Point;
	            if (level1.Bid > stop) return;
                tbxAll.AppendText("\r\nPercent Trailing SELL");
                Sell(Lots, level1.Bid, OrderTypeMarket);
	        }
            else if (_position.NetVolume < 0 && level1.Ask >= _lastPrice - Earned*Point)
            {
                var stop = _currentCandlestick5.Low + PercentDown*Point;
                if (level1.Ask < stop) return;
                tbxAll.AppendText("\r\nPercent Trailing BUY");
                Buy(Lots, level1.Ask, OrderTypeMarket);
            }
	    }

	    private void CheckStopLoss(ILevel1 level1)
	    {
	        if (_position.NetVolume > 0 && level1.Bid <= _lastPrice - MaxDrawdown*Point)
	        {
                tbxAll.AppendText("\r\nStop Loss SELL");
                Sell(Lots, level1.Bid, OrderTypeMarket);
	        }
	        else if (_position.NetVolume < 0 && level1.Ask > _lastPrice + MaxDrawdown*Point)
	        {
                tbxAll.AppendText("\r\nStop Loss BUY");
                Buy(Lots, level1.Ask, OrderTypeMarket);
	        }
	    }

	    private void Algorithm()
	    {

            txbAccounts.Text = "\r\nAlgo running";

            if (_candlestickChart.Candlesticks5.Count < SlowLength)
	            return;

            
	        var high = _candlestickChart.High(CandlestickChart.CandleFrequency.Candles5, 1);
            var low = _candlestickChart.Low(CandlestickChart.CandleFrequency.Candles5, 1);

            var time = DateTime.Now.Hour;

	        //if (time < TimeStart || time >= TimeStart + TimeDuration) return;

	        var fast = _candlestickChart.AverageLast(CandlestickChart.Point.Close, FastLength,
	            CandlestickChart.CandleFrequency.Candles5);
	        var slow = _candlestickChart.AverageLast(CandlestickChart.Point.Close, SlowLength,
	            CandlestickChart.CandleFrequency.Candles5);

            txbAccounts.AppendText("\r\nFast: " + fast );
            txbAccounts.AppendText("\r\nSlow: " + slow );

	        var coefficient = _position.NetVolume == 0 ? 0 : 1;

            var orderSize = (Lots + (Lots*coefficient));

            if (fast > slow)
            {

                if (_side == SideEnum.UNDISCLOSED)
                    _side = SideEnum.SELL;

                txbAccounts.AppendText("\r\nUp Trend");

	            var buyStop = high + Point;
	            var buyLimit = high + 5*Point;

                txbAccounts.AppendText("\r\nL1 Ask: " + _level1.Ask + " == " + " Buy Stop: " + buyStop + " && " + _level1.AskSize + " > " + (orderSize*2));
                txbAccounts.AppendText("\r\nL1 Ask: " + _level1.Ask + " < " + " Buy Limit: " + buyLimit);

                //buy;
                if (_position.NetVolume <= 0 && _level1.Ask == buyStop && _level1.AskSize > orderSize * 2 
                    && _side == SideEnum.BUY)
                {
                    tbxAll.AppendText("\r\nShould BUY at:" + DateTime.Now);
                    Buy(orderSize, buyStop, OrderTypeLimit);
                }

            }

            else if (fast < slow)
	        {

                if (_side == SideEnum.UNDISCLOSED)
                    _side = SideEnum.BUY;

                txbAccounts.AppendText("\r\nDown Trend");

                var sellStop = low - Point;
	            var sellLimit = low - 5 * Point;

                txbAccounts.AppendText("\r\nL1 Bid: " + _level1.Bid + " == " + " Sell Stop: " + sellStop + " && " + _level1.BidSize + " > " + (orderSize * 2));
                txbAccounts.AppendText("\r\nL1 Bid: " + _level1.Bid + " > " + " Sell Limit: " + sellLimit);


                //sell;
                if (_position.NetVolume >= 0 && _level1.Bid == sellStop && _level1.BidSize > orderSize*2 
                    && _side == SideEnum.SELL)
	            {
                    tbxAll.AppendText("\r\nShould SELL at: " + DateTime.Now);
                    Sell(orderSize, sellStop, OrderTypeLimit);
	            }

            }



	    }

	    private void Buy(int orderSize, decimal price, string orderType)
	    {
            CreateOrder(SideEnum.BUY, orderSize, price, orderType);
            _side = SideEnum.SELL;
        } 

	    private void Sell(int orderSize, decimal price, string orderType)
	    {
            CreateOrder(SideEnum.SELL, orderSize, price, orderType);
            _side = SideEnum.SELL;
        } 

	    private void CreateOrder(SideEnum sideEnum, int size, decimal price, string orderType)
	    {

            tbxAll.AppendText("\r\nORDER!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

            _order = _host.CreateOrder(Product, sideEnum, size, price, TimeInForce, orderType);

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
            var lastPrice = (decimal)execReport.LastPx; // Price of this (last) fill.
            //int orderQuantity = (int)execReport.OrderQty; // Number of shares ordered.
            //int leavesQty = (int)execReport.LeavesQty; // Amount of shares open for further execution. LeavesQty = OrderQty - CumQty.
            //int lastQuantity = (int)execReport.LastQty; // Quantity of shares bought/sold on this (last) fill.
            //int cummulativeQuantity = (int)execReport.CumQty; // Total number of shares filled.
            //string clientOrderID = execReport.ClOrdID; // This is the long version of the client orderID. exemple: AMP1-39554-12
            //char side = execReport.Side; // '1'=Buy, '2'=Sell, '3'=Buy Minus, '4'=Sell Plus, '5'=Sell Short, '6'=Sell Short Exempt, '7'=Undisclosed, '8'=Cross, '9'=Cross Short.
            //string text = execReport.Text; // Suplementary information.

            switch (execReport.OrdStatus)
            {
                #region New
                case '0':   // New
                            // Outstanding order with no executions.
                    _orderState = OrderStateEnum.NEW;
                    break;
                #endregion
                #region Partially filled
                case '1':   // Partially filled
                            // Outstanding order with executions and remaining quantity.
                    _orderState = OrderStateEnum.PARTIALLY_FILLED;
                    break;
                #endregion
                #region Filled
                case '2':   // Filled
                            // Order completely filled, no remaining quantity.
                    _orderState = OrderStateEnum.FILLED;
                    _lastPrice = lastPrice;
                    break;
                #endregion
                #region Done for day
                case '3':   // Done for day
                            // Order not, or partially, filled;  no further executions forthcoming for the trading day.
                    _orderState = OrderStateEnum.DONE_FOR_THE_DAY;
                    break;
                #endregion
                #region Canceled
                case '4':   // Canceled
                            // Canceled order with or without executions.
                    _orderState = OrderStateEnum.CANCELED;
                    break;
                #endregion
                #region Replaced
                case '5':   // Replaced
                            // Replaced order with or without executions.
                    _orderState = OrderStateEnum.REPLACE;
                    break;
                #endregion
                #region Pending Cancel
                case '6':   // Pending Cancel
                            // Order with an Order Cancel Request pending, used to confirm receipt of an Order Cancel Request.
                            // DOES NOT INDICATE THAT THE ORDER HAS BEEN CANCELED.
                    _orderState = OrderStateEnum.PENDING_CANCEL;
                    break;
                #endregion
                #region Stopped
                case '7':   // Stopped
                            // Order has been stopped at the exchange. Used when guranteeing or protecting a price and quantity.
                    _orderState = OrderStateEnum.STOPPED;
                    break;
                #endregion
                #region Rejected
                case '8':   // Rejected
                            // Order has been rejected by broker.
                            // NOTE:  An order can be rejected subsequent to order acknowledgment,
                            // i.e. an order can pass from New to Rejected status.
                    _orderState = OrderStateEnum.REJECTED;
                    break;
                #endregion
                #region Suspended
                case '9':   // Suspended
                            // Order has been placed in suspended state at the request of the client.
                    _orderState = OrderStateEnum.SUSPENDED;
                    break;
                #endregion
                #region Pending New
                case 'A':   // Pending New
                            // Order has been received by brokers system but not yet accepted for execution.
                            // An execution message with this status will only be sent in response to a Status Request message.
                    _orderState = OrderStateEnum.PENDING_NEW;
                    break;
                #endregion
                #region Calculated
                case 'B':   // Calculated
                            // Order has been completed for the day (either filled or done for day).
                            // Commission or currency settlement details have been calculated and reported in this execution message.
                    _orderState = OrderStateEnum.CALCULATED;
                    break;
                #endregion
                #region Expired
                case 'C':   // Expired
                            // Order has been canceled in broker's system due to time in force instructions.
                    _orderState = OrderStateEnum.EXPIRED;
                    break;
                #endregion
                #region Accepted for bidding
                case 'D':   // Accepted for bidding
                            // Order has been received and is being evaluated for pricing.
                            // It is anticipated that this status will only be  used with the "Disclosed" BidType List Order Trading model.
                    _orderState = OrderStateEnum.ACCEPTED_FOR_BIDDING;
                    break;
                #endregion
                #region Pending Replace
                case 'E':   // Pending Replace
                            // Order with an Order Cancel/Replace Request pending, used to confirm receipt of an Order Cancel/Replace Request.
                            // DOES NOT INDICATE THAT THE ORDER HAS BEEN REPLACED
                    _orderState = OrderStateEnum.PENDING_REPLACE;
                    break;
                    #endregion
            }
        }

	    #endregion
	}
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace QJExternalTool
{
    public class CandlestickChart
    {

        private const string ConnectionString =
            "Data Source=h98ohmld2f.database.windows.net;Initial Catalog = QJTraderCandlesticks; Integrated Security = False; User ID = JMSXTech; Password=jmsx!2014;Connect Timeout = 60; Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        public enum Point
        {
            Open, Close, High, Low
        }

        public enum CandleFrequency
        {
            Candles5, Candles15, Candles60
        }

        private readonly string _product;

        public List<Candlestick> Candlesticks5 { get; }
        public List<Candlestick> Candlesticks15 { get; }
        public List<Candlestick> Candlesticks60 { get; }

        public List<Candlestick> NewCandlesticks { get; set; }

        public CandlestickChart(string product)
        {
            _product = product;

            var connection = new SqlConnection(ConnectionString);

            var command = new SqlCommand("SELECT O, C, H, L, Frequency, Year FROM " + _product + ";")
            {
                CommandType = CommandType.Text
            };

            connection.Open();

            command.Connection = connection;

            var reader = command.ExecuteReader();

            Candlesticks5 = new List<Candlestick>();
            Candlesticks15 = new List<Candlestick>();
            Candlesticks60 = new List<Candlestick>();
            NewCandlesticks = new List<Candlestick>();

            while (reader.Read())
            {
                var frequency = reader.GetInt32(reader.GetOrdinal("Frequency"));

                var candlestick = new Candlestick(frequency)
                {
                    Frequency = frequency,
                    Open = reader.GetDecimal(reader.GetOrdinal("O")),
                    Close = reader.GetDecimal(reader.GetOrdinal("C")),
                    High = reader.GetDecimal(reader.GetOrdinal("H")),
                    Low = reader.GetDecimal(reader.GetOrdinal("L")),
                    Year = reader.GetInt32(reader.GetOrdinal("Year"))
                };

                switch (frequency)
                {
                    case 5:
                        Candlesticks5.Add(candlestick);
                        break;
                    case 15:
                        Candlesticks15.Add(candlestick);
                        break;
                    default:
                        Candlesticks60.Add(candlestick);
                        break;
                }

            }

            reader.Dispose();
            command.Dispose();
            connection.Dispose();

        }

        public List<Candlestick> Last(int n, CandleFrequency candleFrequency)
        {

            if (n < 1)
                return null;

            var candlesticks = new List<Candlestick>();

            List<Candlestick> candlesticksFrequency;

            switch (candleFrequency)
            {
                case CandleFrequency.Candles5:
                    candlesticksFrequency = Candlesticks5;
                    break;
                case CandleFrequency.Candles15:
                    candlesticksFrequency = Candlesticks15;
                    break;
                case CandleFrequency.Candles60:
                    candlesticksFrequency = Candlesticks60;
                    break;
                default:
                    return null;
            }

            var count = candlesticksFrequency.Count;

            for (var i = count - n; i < candlesticksFrequency.Count; ++i)
                candlesticks.Add(candlesticksFrequency[i]);

            return candlesticks;

        }

        public decimal AverageLast(Point point, int n, CandleFrequency candleFrequency)
        {
            var candlesticks = Last(n, candleFrequency);

            switch (point)
            {
                case Point.Open:
                    return candlesticks.Sum(candlestick => candlestick.Open) / candlesticks.Count;
                case Point.Close:
                    return candlesticks.Sum(candlestick => candlestick.Close) / candlesticks.Count;
                case Point.High:
                    return candlesticks.Sum(candlestick => candlestick.High) / candlesticks.Count;
                case Point.Low:
                    return candlesticks.Sum(candlestick => candlestick.Low) / candlesticks.Count;
                default:
                    return 0;

            }
            

        }

        public void Save(TextBox tbxAll)
        {

            var stringBuilder = new StringBuilder();

            foreach (var candlestick in NewCandlesticks)
                stringBuilder.AppendLine("INSERT INTO " + _product +
                                     " (O, C, H, L, Frequency, Year) VALUES (" + candlestick.Open + ", " + candlestick.Close +
                                     ", " + candlestick.High + ", " + candlestick.Low + ", " + candlestick.Frequency + ", " + candlestick.Year +
                                     ");");


            if (stringBuilder.ToString() == string.Empty)
                return;
            try
            {

                var connection = new SqlConnection(ConnectionString);

                var command = new SqlCommand(stringBuilder.ToString()) {CommandType = CommandType.Text};

                connection.Open();

                command.Connection = connection;

                command.ExecuteNonQuery();

                command.Dispose();
                connection.Dispose();

            }

            catch (Exception e)
            {
                tbxAll.AppendText("\r\n" + e);
            }

            NewCandlesticks.Clear();

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

    }
}
using OptionLib;
using System.ComponentModel.DataAnnotations;
using Table;
using TableCsv;
using TableExtension;

namespace option_console
{
    internal class Program
    {
        static void PrintInstrumentPositionProjection(double currUnderlyingPrice, UnderlyingPortfolio up, double priceFrom, double priceTo, double priceDelta, List<DateTime> dates,
            double volatility, double interest, double dividendYield, Var varType)
        {
            if (dates.Count == 0) { return; }
            var cols = new List<List<OptionProjection>>();
 
            for (int i = 0; i < dates.Count; i++)
            {
                cols.Add(OptionUtil.CreateInstrumentPositionDateList(up, dates[i], priceFrom, priceTo, priceDelta, volatility, interest, dividendYield));
            }

            // Get base for future comparisons
            var baseProj = up.ProjectTo(dates[0], currUnderlyingPrice, volatility, interest, dividendYield);

            int rowCount = cols[0].Count;
            Console.WriteLine($"\n\nProjections of:{up.Underlying.Symbol} {OptionUtil.AsString(up, false)}");
            Console.WriteLine($"===============\n");
            Console.Write("              ");
            for (int i = 0; i < dates.Count; i++)
            {
                Console.Write($"{dates[i]:dd/MMM}    ");
            }
            Console.WriteLine();
            bool priceLinePrinted = false;
            for (int r = 0; r < rowCount; r++)
            {
                if (cols[0][r].UnderlyingPrice > currUnderlyingPrice)
                {
                    if (!priceLinePrinted)
                    {
                        priceLinePrinted = true;
                        Console.WriteLine($"---Curr Price {currUnderlyingPrice, 10:F1}------");
                    }
                }
                Console.Write($"{cols[0][r].UnderlyingPrice,10:F1}");
                for (int i = 0; i < dates.Count; i++)
                {
                    double v = 0.0;
                    if (varType == Var.Price) { v = cols[i][r].Price - baseProj.Price; }
                    if (varType == Var.Point) { v = cols[i][r].Point - baseProj.Point; }
                    if (varType == Var.Value) { v = cols[i][r].Value - baseProj.Value; }
                    Console.Write($"{v, 10:F1}");
                }
                Console.WriteLine();
            }
         }
        static void Print(List<OptionProjection> list)
        {
            bool priceLine = false;
            foreach (var ip in list)
            {
                var sym = ip.Instrument.Underlying.Symbol;
                var typ = ((IOption)ip.Instrument).OptionType;
                var exp = ((IOption)ip.Instrument).ExpiryDate;
                var strike = ((IOption)ip.Instrument).StrikePrice;
                if ((ip.UnderlyingPrice < strike) && !priceLine)
                {
                    Console.WriteLine($"-----------------{ip.UnderlyingPrice:0.00}-------------");
                    priceLine = true;
                }
                Console.WriteLine($"{sym} {typ} {exp:dd/MMM} {strike:0}    Price:{ip.Price:0.00} Delta:{ip.Delta:0.000} Theta:{ip.Theta}");
            }
        }
        static void UpdateBarBegin(UnderlyingPortfolio portfolio, DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature)
        {
            if (portfolio.Count == 0 && portfolio.EquityPosition == 0)
            {
                var optPutSell = new Option((Underlying)portfolio.Underlying, OptionType.PUT, Math.Round(underlyingPrice) - 0.0, dateTime.AddDays(7));
                var ops = optPutSell.ProjectTo(dateTime, underlyingPrice, volatility, interest, dividendYield);
                portfolio.AdjustOptionPosition(optPutSell, dateTime, -1, ops.Price);
            }
            if (portfolio.EquityPosition > 0 && portfolio.Count == 0)
            {
                var optCallSell = new Option((Underlying)portfolio.Underlying, OptionType.CALL, Math.Round(underlyingPrice) + 0.0, dateTime.AddDays(7));
                var ops = optCallSell.ProjectTo(dateTime, underlyingPrice, volatility, interest, dividendYield);
                portfolio.AdjustOptionPosition(optCallSell, dateTime, -1, ops.Price);
            }
            var port_proj = portfolio.ProjectTo(dateTime, underlyingPrice, volatility, interest, dividendYield);
        }
        static void UpdateOptionBar(UnderlyingPortfolio portfolio, OptionProjection optionProjection, int quantity, double optionCashPosition, double temperature)
        {
               
        }
        static void UpdateBarEnd(UnderlyingPortfolio portfolio, DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature)
        {
            var proj = portfolio.ProjectTo(dateTime, underlyingPrice, volatility, interest, dividendYield);

            var optTyp = "";
            foreach (var opt in portfolio.OptionKeys)
            {
                optTyp = opt;
            }

            Console.WriteLine($"{dateTime} {underlyingPrice} Option:{optTyp} {portfolio.OptionCash}");
        }

        static void run_qqq()
        {
            //
            // Load Csv - convert all fields to the correct types
            var attr = new DefaultFieldAttributes();
            attr.SetDefault(TypeCode.Double);
            attr.Add(new Field("Date", TypeCode.DateTime, true));
            IDynamicTable extractTable = new DynamicTable();
            extractTable.LoadCsv(@"d:\predict\index_extract_ta.csv", CsvHeaderType.Header, null, attr);

            //
            // Source field definitions
            var Date = extractTable.AsList<DateTime>("Date");
            var qqq_underlying = extractTable.AsList<double>("close");
            var qqq_volatility = extractTable.AsList<double>("volatility");
            var qqq_interest = extractTable.AsList<double>("interest");
            var qqq_dividend = extractTable.AsList<double>("dividend");

            int start = qqq_underlying.Count - 360;
            var under = new Underlying("CBOE", "QQQ", CurrencyType.USD, 1.0, 100);
            var underlyingPortfolio = new UnderlyingPortfolio(under, Date[start]);

            underlyingPortfolio.UpdateBarBegin += UpdateBarBegin;
            underlyingPortfolio.UpdateOptionBar += UpdateOptionBar;
            underlyingPortfolio.UpdateBarEnd += UpdateBarEnd;

            for (int i = start; i < qqq_underlying.Count; i++)
            {
                var dt = new DateTime(Date[i].Year, Date[i].Month, Date[i].Day, 16, 0, 0);
                underlyingPortfolio.UpdateBar(dt, qqq_underlying[i], /* spx_volatility[i] */ 0.22, qqq_interest[i], qqq_dividend[i], 0.0, true);
            }

        }
        public static double LargestMultipleLessThan(double val, double delta)
        {
            if (delta == 0)
            {
                throw new ArgumentException("Delta must not be zero.", nameof(delta));
            }

            double d = Math.Abs(delta);
            double q = Math.Floor(val / d);
            double m = q * d;

            if (m >= val) { m -= d; }

            return m;
        }
        static void run_spx2()
        {
            //
            // Load Csv - convert all fields to the correct types
            var attr = new DefaultFieldAttributes();
            attr.SetDefault(TypeCode.Double);
            attr.Add(new Field("Date", TypeCode.DateTime, true));
            IDynamicTable extractTable = new DynamicTable();
            extractTable.LoadCsv(@"d:\predict\index_extract_ta.csv", CsvHeaderType.Header, null, attr);

            //
            // Source field definitions
            var date = extractTable.AsList<DateTime>("Date");
            var qqq_open = extractTable.AsList<double>("open");
            var qqq_high = extractTable.AsList<double>("high");
            var qqq_low = extractTable.AsList<double>("low");
            var qqq_close = extractTable.AsList<double>("close");
            var qqq_volatility = extractTable.AsList<double>("volatility");
            var qqq_interest = extractTable.AsList<double>("interest");
            var qqq_dividend = extractTable.AsList<double>("dividend");

            var under = new Underlying("CBOE", "SPX", CurrencyType.USD, 100.0, 1);
            var proj = new OptionProjection();

            double premium = 0.0;
            double totalloss = 0.0;
            double loss = 0.0;
            int countge0 = 0;
            int count = 0;

            for (int i = 0; i < qqq_close.Count; i++)
            {
                if (date[i].Year != 2025) { continue; }
                double underP = LargestMultipleLessThan(qqq_open[i], 5.0);
                var expDateTime = new DateTime(date[i].Year, date[i].Month, date[i].Day, 16, 0, 0);
                var opt = new Option(under, OptionType.PUT, underP, expDateTime);
                var currDateTime = new DateTime(date[i].Year, date[i].Month, date[i].Day, 9, 30, 0);
                proj = opt.ProjectTo(currDateTime, qqq_open[i], 0.20, 0.0275, 0.0, proj);

                premium += proj.Price;
                if (qqq_close[i] < underP) { loss = qqq_close[i] - underP; totalloss += loss; }
                if (qqq_close[i] >= underP) { countge0++; }
                count++;
                Console.WriteLine($"{currDateTime:yy/MM/dd hh:mm} Underlying:{proj.UnderlyingPrice} Price:{proj.Price:0.00} {expDateTime:yy/MM/dd hh:mm} Strike:{opt.StrikePrice:0.00} Close:{qqq_close[i]:0.00} Loss:{loss}");
                loss = 0.0;
            }
            Console.WriteLine($"==================================================================");
            Console.WriteLine($"Total Premium(US$): {premium*100:0.0}");
            Console.WriteLine($"Loss (US$)        : {totalloss*100:0.0}");
            Console.WriteLine($"Count             : {count}");
            Console.WriteLine($"Count >= 0        : {countge0}");
        }
        static void Main(string[] args)
        {
            /*

            var tradingPeriod = new TradingPeriod(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
            //var endOfDay = new DateTime(DateOnly.FromDateTime(DateTime.Now), new TimeOnly(23, 59));
            var endOfDay = new DateTime(2026, 07, 15, 23, 59, 59);
            var currTradingDay = tradingPeriod.NextTradingPeriod(DateTime.Now.AddDays(-1));

            double currUnderlyingPrice = 7564.0;
            double volatility = 0.11;
            double interest = 0.0275;
            double dividend = 0.0;

            var parms = new OptionParm(1.0, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
            var under = new Underlying("CBOE", "SPX", CurrencyType.USD, 100.0, 1);
            //var expiryDate = DateTime.Now.AddDays(7);
            var expiryDate = new DateTime(2026, 7, 22, 16, 0, 0);
            //var expiryDate = new DateTime(2026, 8, 14, 16, 0, 0);
            var listPut = OptionUtil.CreateOptionChainList(under, parms, currTradingDay, currUnderlyingPrice, volatility, interest, dividend, OptionType.PUT, expiryDate, 7420.0, 7580.0);
            var listCall = OptionUtil.CreateOptionChainList(under, parms, currTradingDay, currUnderlyingPrice, volatility, interest, dividend, OptionType.CALL, expiryDate, 7420.0, 7580.0);


            //var optPutBuy = listPut.FindLast(x => x.Delta > -0.25);
            var optPutBuy = new Option(under, OptionType.PUT, 7430.0, expiryDate);
            //var optPutSell = listPut.FindLast(x => x.Delta > -0.50);
            var optPutSell = new Option(under, OptionType.PUT, 7500.0, expiryDate); 

            Console.WriteLine("\n========= Call ===============\n");
            Print(listCall);
            Console.WriteLine("\n========= Put ===============\n");
            Print(listPut);


            //var optComb = new Compound(under);
            //optComb.Add(optPutSell.Instrument, -1);
            //optComb.Add(optCallSell.Instrument, -1);

            var port = new UnderlyingPortfolio(under, currTradingDay);
            port.OpenOptionPosition(optPutSell, currTradingDay, -1, 19.34);
            port.OpenOptionPosition(optPutBuy, currTradingDay, 1, 9.23);
            //port.OpenEquityPosition(currTradingDay, 1, currUnderlyingPrice);

            Console.WriteLine();

            var dt = new List<DateTime>();
            dt.Add(currTradingDay);
            dt.Add(currTradingDay.AddDays(1));
            dt.Add(currTradingDay.AddDays(2));
            dt.Add(currTradingDay.AddDays(3));
            dt.Add(currTradingDay.AddDays(4));
            dt.Add(currTradingDay.AddDays(5));
            dt.Add(currTradingDay.AddDays(6));
            dt.Add(currTradingDay.AddDays(7));
            //dt.Add(currTradingDay.AddDays(8));

            // Print out the table for a specific instrument
            PrintInstrumentPositionProjection(currUnderlyingPrice, port, 7420.0, 7580.0, 1.0, dt, volatility, interest, 0.0, Var.Value);
            */

            run_qqq();
        }
    } 
}

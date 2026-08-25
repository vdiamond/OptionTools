using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;


namespace OptionLib
{
    static public class OptionUtil
    {
 
        static public string AsString(IInstrument instrument, bool includeSymbol = true)
        {
            var opt = instrument as Option;
            if (opt != null)
            {
                string s = includeSymbol ? instrument.Underlying.Symbol + " " : "";
                return $"[{s}{opt.OptionType.ToString()} {opt.ExpiryDate:dd/MMM} {opt.StrikePrice:0.#}]";
            }

            var up = instrument as UnderlyingPortfolio;
            if (up != null)
            {
                var sb = new StringBuilder();

                if (includeSymbol) { sb.Append(instrument.Underlying.Symbol); sb.Append(" "); }
                bool first = true;
                foreach (var ok in up.OptionKeys)
                {
                    string buf = (first) ? "[" : " | ";
                    var ist = up.GetOption(ok);
                    if (up.GetOptionPosition(ok) == 0) { continue; }
                    sb.Append($"{buf}{AsString(ist, includeSymbol)} ({up.GetOptionPosition(ok)})");
                    first = false;
                 }

                if (up.EquityPosition != 0)
                {
                    sb.Append($" | Equity ({up.EquityPosition})");
                }

                sb.Append("]");
                return sb.ToString();

            }
            throw new ArgumentException("Unsupported instrument type");
        }

        public static List<OptionProjection> CreateOptionChainList(IUnderlying underlying, IOptionParm parms,
            DateTime currentDateTime, double underlyingPrice, double volatility, double interest, double dividendYield,
            OptionType optionType, DateTime expiryDate, double strikePriceFrom, double strikePriceTo)
        {
            var ret = new List<OptionProjection>();

            var currentStrike = parms.PrevStrikePrice(strikePriceFrom);
            while (currentStrike < strikePriceTo)
            {
                var opt = new Option((Underlying)underlying, optionType, currentStrike, expiryDate);
                var ip = new OptionProjection();
                ret.Add(opt.ProjectTo(currentDateTime, underlyingPrice, volatility, interest, dividendYield, ip));
                currentStrike = parms.NextStrikePrice(currentStrike);
            }
            return ret;
        }

        public static List<OptionProjection> CreateInstrumentPositionDateList(UnderlyingPortfolio up, DateTime forDateTime, double priceFrom, double priceTo, double priceDelta,
            double volatility, double interest, double dividendYield)
        {
            var ret = new List<OptionProjection>();

            var currentPrice = priceFrom;
            while (currentPrice < priceTo)
            {
                var op = new OptionProjection();
                up.ProjectTo(forDateTime, currentPrice, volatility, interest, dividendYield, op);
                ret.Add(op);
                currentPrice += priceDelta;
            }
            return ret;
        }
    }
}
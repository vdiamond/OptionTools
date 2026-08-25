using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionLib
{
    static public class OptionKeyCCC
    {
        static public string Convert(string symbol, DateOnly expiryDate, OptionType optionType, double strikePrice)
        {
            symbol = symbol.Trim();
            if (symbol.Length > 6) { throw new Exception($"Symbol({symbol}) length must be 6 characters or less"); }
            return $"{symbol}{expiryDate :yyMMdd}{optionType.ToString().Substring(0,1)}{strikePrice * 100 :00000000}";
        }
    }
    public class OptionProjection
    {
        public OptionProjection()
        {
            Reset();
        }
        public void Reset()
        {
            DateTime = DateTime.MinValue;
            UnderlyingPrice = 0.0;
            Interest = 0.0;
            DividendYield = 0.0;
            Delta = 0.0;
            Gamma = 0.0;
            Theta = 0.0;
            Vega = 0.0;
            Rho = 0.0;
            Volatility = 0.0;
            Price = 0.0;
            Point = 0.0;
            Value = 0.0;
            Instrument = null;
        }
        public void Reset(OptionProjection other)
        {
            DateTime = other.DateTime;
            UnderlyingPrice = other.UnderlyingPrice;
            Interest = other.Interest;
            DividendYield = other.DividendYield;
            Delta = other.Delta;
            Gamma = other.Gamma;
            Theta = other.Theta;
            Vega = other.Vega;
            Rho = other.Rho;
            Volatility = other.Volatility;
            Price = other.Price;
            Point = other.Point;
            Value = other.Value;
            Instrument = other.Instrument;  
        }
        public OptionProjection(OptionProjection other)
        {
            DateTime = other.DateTime;
            UnderlyingPrice = other.UnderlyingPrice;
            Interest = other.Interest;
            DividendYield = other.DividendYield;
            Delta = other.Delta;
            Gamma = other.Gamma;
            Theta = other.Theta;
            Vega = other.Vega;
            Rho = other.Rho;
            Volatility = other.Volatility;
            Price = other.Price;
            Point = other.Point;
            Value = other.Value;
            Instrument = other.Instrument;
        }
        public DateTime DateTime { get; set; }
        public double UnderlyingPrice { get; set; }
        public double Interest { get; set; }
        public double DividendYield { get; set; }
        public double Delta { get; set; }
        public double Gamma { get; set; }
        public double Theta { get; set; }
        public double Vega { get; set; }
        public double Rho { get; set; }
        public double Volatility { get; set; }
        public double Price { get; set; }
        public double Point { get; set; }
        public double Value { get; set; }
        public IInstrument Instrument { get; set; }
    }
    public static class OptionProjectionPool
    {
        public static readonly ObjectPool<OptionProjection> Pool =
            new DefaultObjectPoolProvider().Create<OptionProjection>(
            new DefaultPooledObjectPolicy<OptionProjection>());
    }

}

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionLib
{
    public interface IOptionParm
    {
        DateTime NextExpiryDate(DateTime expiryDate);
        DateTime PrevExpiryDate(DateTime expiryDate);
        double NextStrikePrice(double strikePrice);
        double PrevStrikePrice(double strikePrice);
    }
    public class OptionParm : IOptionParm
    {
        public OptionParm(double strikePriceGranularity = 1.0, params DayOfWeek[] expirationDays)
        {
            _strikePriceGranularity = strikePriceGranularity;
            _expirationDays = expirationDays;
        }
        public DateTime NextExpiryDate(DateTime expiryDate)
        {
            var ret = expiryDate.AddDays(1.0);
            while (!_expirationDays.Contains(ret.DayOfWeek)) { ret = ret.AddDays(1.0); }
            ret = new DateTime(ret.Year, ret.Month, ret.Day, 16, 0, 0);
            return ret;
        }
        public DateTime PrevExpiryDate(DateTime expiryDate)
        {
            var ret = expiryDate.AddDays(-1.0);
            while (!_expirationDays.Contains(ret.DayOfWeek)) { ret = ret.AddDays(-1.0); }
            ret = new DateTime(ret.Year, ret.Month, ret.Day, 16, 0, 0);
            return ret;
        }
        public double NextStrikePrice(double strikePrice)
        {
            double granules = strikePrice / _strikePriceGranularity;
            return (Math.Truncate(granules) + 1.0) * _strikePriceGranularity;
        }
        public double PrevStrikePrice(double strikePrice)
        {
            double granules = strikePrice / _strikePriceGranularity;
            if (Math.Truncate(granules) == granules) { return strikePrice - _strikePriceGranularity; }
            return Math.Truncate(granules) * _strikePriceGranularity;
        }
        #region private 
        private double _strikePriceGranularity;
        private DayOfWeek[] _expirationDays;
        #endregion
    }


    public interface ITradingPeriod
    {
        DateTime NextTradingPeriod(DateTime tradingPeriod);
        DateTime PrevTradingPeriod(DateTime tradingPeriod);
    }


    public class TradingPeriod : ITradingPeriod
    {
        public TradingPeriod(params DayOfWeek[] tradingDays)
        {
            _tradingDays = tradingDays; 
        }
        public DateTime NextTradingPeriod(DateTime tradingPeriod)
        {
            var ret = tradingPeriod.AddDays(1.0);
            while (!_tradingDays.Contains(ret.DayOfWeek)) { ret = ret.AddDays(1.0); }
            ret = new DateTime(ret.Year, ret.Month, ret.Day, 16, 0, 0);
            return ret;
        }
        public DateTime PrevTradingPeriod(DateTime tradingPeriod)
        {
            var ret = tradingPeriod.AddDays(-1.0);
            while (!_tradingDays.Contains(ret.DayOfWeek)) { ret = ret.AddDays(-1.0); }
            ret = new DateTime(ret.Year, ret.Month, ret.Day, 16, 0, 0);
            return ret;
        }
        #region private
        private DayOfWeek[] _tradingDays;
        #endregion
    }
}

using OptionLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace OptionLib
{
    // 
    public class Underlying : IUnderlying
    {
        public Underlying(string exchange, string symbol, CurrencyType currencyType, double currencyPerPoint, int contractLot)
        {
            _exchange = exchange.TrimEnd();
            if (symbol.Length > 6) { symbol = symbol.Substring(0, 6); }
            symbol = symbol.TrimEnd();
            _symbol = symbol;
            _currencyType = currencyType;
            _currencyPerPoint = currencyPerPoint;
            _contractLot = contractLot;
        }
        public string Exchange { get { return _exchange; } }
        public string Symbol { get { return _symbol; } }
        public CurrencyType CurrencyType { get { return _currencyType; } }
        public double CurrencyPerPoint { get { return _currencyPerPoint; } }
        public int ContractLot { get { return _contractLot; } }
        public double ConvertVar(Var from, Var to, double value)
        {
            if (from == to) { return value; }
            if (from == Var.Price && to == Var.Point) { return value * _contractLot; }
            if (from == Var.Price && to == Var.Value) { return value * _contractLot * _currencyPerPoint; }
            if (from == Var.Point && to == Var.Price) { return value / _contractLot; }
            if (from == Var.Point && to == Var.Value) { return value * _currencyPerPoint; }
            if (from == Var.Value && to == Var.Price) { return value / (_contractLot * _currencyPerPoint); }
            if (from == Var.Value && to == Var.Point) { return value / _currencyPerPoint; }
            throw new Exception($"Cannot convert from {from} to {to}");
        }
        #region private
        private string _exchange;
        private string _symbol;
        private CurrencyType _currencyType;
        private double _currencyPerPoint;
        private int _contractLot;
        #endregion
    }
    public class Option : IOption
    {
        //
        // Constructor
        public Option(Underlying underlying, OptionType optionType, double strikePrice, DateTime expiryDate, string optionKey = null)
        {
            _underlying = underlying;
            _optionType = optionType;
            _strikePrice = strikePrice;
            expiryDate = new DateTime(expiryDate.Year, expiryDate.Month, expiryDate.Day, 16, 00, 0);
            _expiryDate = expiryDate;
            _optionKey = optionKey;
            if (_optionKey == null)
            {
                _optionKey = OptionKeyCCC.Convert(underlying.Symbol, DateOnly.FromDateTime(expiryDate), optionType, strikePrice);
            }
        }
        //
        // IInstrument
        public IUnderlying Underlying { get { return _underlying; } }
        //
        // Projection
        public OptionProjection ProjectTo(DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, OptionProjection projection = null)
        {
            if (projection == null) { projection = new OptionProjection(); }
            if (dateTime > _expiryDate) { return projection; } // no projection after expiry

            OptionGreeks optionGreeks = new OptionGreeks();

            optionGreeks = OptionCalculator.Calculate((_optionType == OptionType.CALL), underlyingPrice, _strikePrice, OptionCalculator.CalculateTimeToExpiry(_expiryDate, dateTime),
                interest, dividendYield, volatility);

            projection.DateTime = dateTime;
            projection.UnderlyingPrice = underlyingPrice;
            projection.Interest = interest;
            projection.DividendYield = dividendYield;
            projection.Delta = optionGreeks.Delta;
            projection.Gamma = optionGreeks.Gamma;
            projection.Theta = optionGreeks.Theta;
            projection.Vega = optionGreeks.Vega;
            projection.Rho = optionGreeks.Rho;
            projection.Price = optionGreeks.Price;

            projection.Volatility = volatility;

            projection.Point = _underlying.ConvertVar(Var.Price, Var.Point, projection.Price);
            projection.Value = _underlying.ConvertVar(Var.Price, Var.Value, projection.Price);

            projection.Instrument = this;
            return projection;
        }
        public double DeriveVolatility(DateTime dateTime, double price, double underlyingPrice, double interest, double dividendYield)
        {
            return OptionCalculator.Volatility(price, (_optionType == OptionType.CALL), underlyingPrice, _strikePrice, interest, dividendYield,
                OptionCalculator.CalculateTimeToExpiry(_expiryDate, dateTime));
        }
        //
        // IOption
        // Option structure
        public string OptionKey { get { return _optionKey; } }
        public DateTime ExpiryDate { get { return _expiryDate; } }
        public OptionType OptionType { get { return _optionType; } }
        public double StrikePrice { get { return _strikePrice; } }
        #region private
        private Underlying _underlying;
        private OptionType _optionType;
        private double _strikePrice;
        private DateTime _expiryDate;
        private string _optionKey;
        #endregion
    }

}
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace OptionLib
{
    public delegate void UpdateBarBeginHandler(UnderlyingPortfolio portfolio, DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature);
    public delegate void UpdateOptionBarHandler(UnderlyingPortfolio portfolio, OptionProjection optionProjection, int quantity, double optionCashPosition, double temperature);
    public delegate void UpdateBarEndHandler(UnderlyingPortfolio portfolio, DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature);

    public class UnderlyingPortfolio : IInstrument
    {
        public event UpdateBarBeginHandler UpdateBarBegin;
        public event UpdateOptionBarHandler UpdateOptionBar;
        public event UpdateBarEndHandler UpdateBarEnd;

        public UnderlyingPortfolio(IUnderlying underlying, DateTime fromDate, double equityCash = 0.0, double optionCash = 0.0)
        { 
            _account = new UnderlyingAccount(underlying, fromDate, equityCash, optionCash);
            _optionProjection = new OptionProjection();
            _portfolioProjection = new OptionProjection();
            _lastUnderlyingPrice = 0.0;
        }
        public void UpdateBar(DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature, bool assign = false)
        {
            if (_lastUnderlyingPrice == 0.0)
            {
                _lastUnderlyingPrice = underlyingPrice;
            }
            _expireOptions(_account.ToDate, dateTime, _lastUnderlyingPrice, assign);
            OnUpdateBarBegin(dateTime, underlyingPrice, volatility, interest, dividendYield, temperature);   
            foreach (var optKey in _account.OptionKeys)
            {
                var option = _account.GetOption(optKey);
                var quantity = _account.GetOptionPosition(optKey);
                var optionCashPosition = _account.GetOptionCashPosition(optKey); 
                option.ProjectTo(dateTime, underlyingPrice, volatility, interest, dividendYield, _optionProjection);
                OnUpdateOptionBar(_optionProjection, quantity, optionCashPosition, temperature);
            }
            OnUpdateBarEnd(dateTime, underlyingPrice, volatility, interest, dividendYield, temperature);
            _account.RemoveZeroOptionPositions();
            _lastUnderlyingPrice = underlyingPrice;
        }
        #region Services to be called by derived classes
        public void AdjustOptionPosition(IOption option, DateTime dateTime, int quantity, double price)
        {
            _account.AdjustOptionPosition(option, dateTime, quantity, price);
        }
        public void CloseOptionPosition(IOption option, DateTime dateTime, double price)
        {
            int quantity = -_account.GetOptionPosition(option.OptionKey);
            AdjustOptionPosition(option, dateTime, quantity, price);
        }
        public void AdjustEquityPosition(DateTime dateTime, int quantity, double underlyingPrice)
        {
            _account.AdjustEquityPosition(dateTime, quantity, underlyingPrice);
        }
        public void CloseEquityPosition(DateTime dateTime, double underlyingPrice)
        {
            int quantity = -_account.EquityPosition;
            AdjustEquityPosition(dateTime, quantity, underlyingPrice);
        }
        public void ExerciseOption(IOption option, DateTime dateTime)
        {
             _account.ExerciseOption(option, dateTime);
        }
        public void AssignOption(IOption option, DateTime dateTime, double underlyingPrice)
        {
            _account.AssignOption(option, dateTime, underlyingPrice);
        }
        public OptionProjection ProjectTo(DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, OptionProjection projection = null)
        {
            if (projection == null) { projection = new OptionProjection(); }
            projection.Reset();
            foreach (var optKey in _account.OptionKeys)
            {
                var option = _account.GetOption(optKey);
                var quantity = _account.GetOptionPosition(optKey);
                var optionCashPosition = _account.GetOptionCashPosition(optKey);
                option.ProjectTo(dateTime, underlyingPrice, volatility, interest, dividendYield, _optionProjection);
                _combineOption(projection, _optionProjection, quantity);
            }
            _combineEquity(projection, underlyingPrice, _account.EquityPosition);
            return projection;
        }
        public IUnderlying Underlying {  get { return _account.Underlying; } }
        public int Count {  get { return _account.Count; } }
        public IEnumerable<string> OptionKeys { get { return _account.OptionKeys; }  }
        public double OptionCash {  get { return _account.OptionCash; } } 
        public IOption GetOption(string optionKey) {  return _account.GetOption(optionKey); }
        public int GetOptionPosition(string optionKey) {  return _account.GetOptionPosition(optionKey); }
        public double GetOptionCashPosition(string optionKey) {  return _account.GetOptionCashPosition(optionKey);  }
        public double EquityCash { get { return _account.EquityCash; }  }
        public int EquityPosition {  get {  return _account.EquityPosition;  }  } 
        #endregion
        #region Virtual calls to be implemented by derived classes
        protected virtual void OnUpdateBarBegin(DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature)
        {
            if (UpdateBarBegin  != null) { UpdateBarBegin(this, dateTime, underlyingPrice, volatility, interest, dividendYield, temperature); return; }
        }
        protected virtual void OnUpdateOptionBar(OptionProjection optionProjection, int quantity, double optionCashPosition, double temperature) 
        {
            if (UpdateOptionBar != null) { UpdateOptionBar(this, optionProjection, quantity, optionCashPosition, temperature); return; }
        }
        protected virtual void OnUpdateBarEnd(DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, double temperature)
        {
            if (UpdateBarEnd != null) { UpdateBarEnd(this, dateTime, underlyingPrice, volatility, interest, dividendYield, temperature); return; };
        }
        #endregion
        #region private
        private void _expireOptions(DateTime prevDateTime, DateTime currentDateTime, double underlyingPrice, bool assign = false)
        {
            foreach (var optKey in _account.OptionKeys)
            {
                var option = _account.GetOption(optKey);
                var position = _account.GetOptionPosition(optKey);
                if (prevDateTime <= option.ExpiryDate && option.ExpiryDate < currentDateTime)
                {
                    _account.ExpireOption(option, currentDateTime, underlyingPrice, assign);
                }
            }
        }
        private void _combineOption(OptionProjection portfolioProjection, OptionProjection optionProjection, int quantity)
        {
            portfolioProjection.Delta += optionProjection.Delta * quantity;
            portfolioProjection.Gamma += optionProjection.Gamma * quantity;
            portfolioProjection.Theta += optionProjection.Theta * quantity;
            portfolioProjection.Vega += optionProjection.Vega * quantity;
            portfolioProjection.Rho += optionProjection.Rho * quantity;
            portfolioProjection.Price += optionProjection.Price * quantity;
            portfolioProjection.Point += optionProjection.Point * quantity;
            portfolioProjection.Value += optionProjection.Value * quantity;
        }
        private void _combineEquity(OptionProjection portfolioProjection, double underlyingPrice, int quantity)
        {
            portfolioProjection.Delta += quantity;
            portfolioProjection.Gamma += 0.0;
            portfolioProjection.Theta += 0.0;
            portfolioProjection.Vega += 0.0;
            portfolioProjection.Rho += 0.0;
            portfolioProjection.Price += underlyingPrice * quantity;
            portfolioProjection.Point += _account.Underlying.ConvertVar(Var.Price, Var.Point, underlyingPrice) * quantity;
            portfolioProjection.Value += _account.Underlying.ConvertVar(Var.Price, Var.Value, underlyingPrice) * quantity;
            portfolioProjection.UnderlyingPrice = underlyingPrice;
        }
        private UnderlyingAccount _account;
        private OptionProjection _optionProjection;
        private OptionProjection _portfolioProjection;
        private double _lastUnderlyingPrice;
        #endregion
    }
}

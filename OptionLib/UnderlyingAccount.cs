using System;
using System.Collections.Generic;
using System.Text;

namespace OptionLib
{
   
    public class UnderlyingAccount 
    {
        public UnderlyingAccount(IUnderlying underlying, DateTime fromDate, double equityCash = 0.0, double optionCash = 0.0)
        {
            _underlying = underlying;
            _optionPositions = new Dictionary<string, OptionPosition>();
            _equityPosition = 0;
            _equityCash = equityCash;
            _optionCash = optionCash;
            _fromDate = fromDate;
            _toDate = fromDate;
        }
        public void Reset(DateTime fromDate, double equityCash = 0.0, double optionCash = 0.0)
        {
            _optionPositions.Clear();
            _equityPosition = 0;
            _equityCash = equityCash;
            _optionCash = optionCash;
            _fromDate = fromDate;
            _toDate = fromDate;
        }
        public IUnderlying Underlying { get { return _underlying; } }
        public int Count { get { return _optionPositions.Count; } }
        public IEnumerable<string> OptionKeys { get { return _optionPositions.Keys; } }
        public int GetOptionPosition(string optionKey)
        {
            OptionPosition op;
            if (_optionPositions.TryGetValue(optionKey, out op))
            {
                return op.Quantity;
            }
            return 0;
        }
        public double GetOptionCashPosition(string optionKey)
        {
            OptionPosition op;
            if (_optionPositions.TryGetValue(optionKey, out op))
            {
                return op.OptionCash;
            }
            return 0;
        }
        public IOption GetOption(string optionKey)
        {
            OptionPosition op;
            if (_optionPositions.TryGetValue(optionKey, out op))
            {
                return op.Option;
            }
            throw new ArgumentException($"Option with key {optionKey} not found.");
        }
        public int EquityPosition { get { return _equityPosition; } }
        public double EquityCash { get { return _equityCash; } }
        public double OptionCash { get { return _optionCash; } }
        public DateTime ToDate { get { return _toDate; } }

        public void AdjustEquityPosition(DateTime dateTime, int quantity, double underlyingPrice)
        {
            // Ensure date time sequence is hounored
            _checkDate(dateTime);

            // quantity cannot be 0
            if (quantity == 0) { throw new Exception("Quantity cannot be 0."); }

            // price must be greater than 0
            if (underlyingPrice <= 0.0) { throw new Exception("Underlying price cannot be 0 or negative."); }

             // Update the quantity of the equity position
            _equityPosition += quantity;

            // Update the cash adjustment
            double cashAdjustment = -_underlying.ConvertVar(Var.Price, Var.Value, underlyingPrice) * quantity;
            _equityCash += cashAdjustment;
        }
        public void CloseEquityPosition(DateTime dateTime, double underlyingPrice)
        {
            AdjustEquityPosition(dateTime, -_equityPosition, underlyingPrice);
        }
        public void AdjustOptionPosition(IOption option, DateTime dateTime, int quantity, double price)
        {
            // cannot open option position after expiry date
            if (dateTime >= option.ExpiryDate) { throw new Exception("Cannot open option position after expiry date."); }
            _adjustOptionPosition(option, dateTime, quantity, price);
        }
        public void CloseOptionPosition(IOption option, DateTime dateTime, double price)
        {
            // See if we can get the option to close
            OptionPosition op;
            if (!_optionPositions.TryGetValue(option.OptionKey, out op))
            {
                return;
            }
            AdjustOptionPosition(option, dateTime, -op.Quantity, price);
        }
        public void ExerciseOption(IOption option, DateTime dateTime)
        {
            // cannot exercise option after expiry date
            if (dateTime >= option.ExpiryDate) { throw new Exception("Cannot exercise option after expiry date."); }
            // Get Quantity
            OptionPosition op;
            if (!_optionPositions.TryGetValue(option.OptionKey, out op))
            {
                throw new Exception($"Attempting to exercise option ({option.OptionKey}) which does not have a position");
            }
            var quantity = op.Quantity;

            _adjustOptionPosition(option, dateTime, -quantity, 0.0);

            // Adjust equity position
            if (option.OptionType == OptionType.CALL)
            {
                AdjustEquityPosition(dateTime, quantity, option.StrikePrice);
            }
            if (option.OptionType == OptionType.PUT)
            {
                AdjustEquityPosition(dateTime, -quantity, option.StrikePrice);
            }
        }
        public void AssignOption(IOption option, DateTime dateTime, double underlyingPrice)
        {
            // cannot assign option before expiry date
            if (dateTime < option.ExpiryDate) { throw new Exception("Cannot assign option before expiry date."); }

            // Get option data
            OptionPosition op;
            if (!_optionPositions.TryGetValue(option.OptionKey, out op))
            {
                throw new Exception($"Attempting to assign option ({option.OptionKey}) which does not have a position");
            }
            var quantity = op.Quantity;

            // quantity cannot be 0
            if (quantity >= 0) { throw new Exception("Quantity cannot be >= 0. Can only assign a short option position"); }

            // Can only assign an option if its ITM
            if (option.OptionType == OptionType.CALL)
            {
                if (underlyingPrice <= option.StrikePrice) { throw new Exception("Cannot assign a call option that is not ITM."); }
            }
            if (option.OptionType == OptionType.PUT)
            {
                if (underlyingPrice >= option.StrikePrice) { throw new Exception("Cannot assign a put option that is not ITM."); }
            }

            // Adjustoption position
            _adjustOptionPosition(option, dateTime, -quantity, 0.0);

            // Adjust equity position
            if (option.OptionType == OptionType.CALL)
            {
                AdjustEquityPosition(dateTime, quantity, option.StrikePrice);
            }
            else if (option.OptionType == OptionType.PUT)
            {
                AdjustEquityPosition(dateTime, -quantity, option.StrikePrice);
            }
        }
        public bool ExpireOption(IOption option, DateTime dateTime, double underlyingPrice, bool assign = false)
        {
            // cannot expire option before expiry date
            if (dateTime < option.ExpiryDate) { return false; }

            // Check if option exists
            OptionPosition op;
            if (!_optionPositions.TryGetValue(option.OptionKey, out op))
            {
                throw new Exception($"Attempting to expire an option ({option.OptionKey}) that does not exist");
            }

            // Check if we are ITM
            bool ITM = false;
            if (option.OptionType == OptionType.CALL)
            {
                if (underlyingPrice > option.StrikePrice) { ITM = true; }
            }
            if (option.OptionType == OptionType.PUT)
            {
                if (underlyingPrice < option.StrikePrice) { ITM = true; }
            }

            // If we are not ITM, then we can just expire the option and return true
            if (!ITM)
            {
                _adjustOptionPosition(option, dateTime, -op.Quantity, 0.0);
                return true;
            }

            // If we are, short and need to assign, then assign the option and return true
            if (assign && op.Quantity < 0)
            {
                AssignOption(option, dateTime, underlyingPrice);
                return true;
            }

            // We need to adjust the cash
            double cashAdjustment = 0.0;
            if (option.OptionType == OptionType.CALL)
            {
                cashAdjustment = option.Underlying.ConvertVar(Var.Price, Var.Value, underlyingPrice - option.StrikePrice) * op.Quantity;
            }
            else
            {
                // Its a put
                cashAdjustment = option.Underlying.ConvertVar(Var.Price, Var.Value, option.StrikePrice - underlyingPrice) * op.Quantity;
            }


            op.Quantity = 0;
            op.OptionCash += cashAdjustment;
            _optionPositions[option.OptionKey] = op;

            _optionCash += cashAdjustment;

            return true;
        }
        public void RemoveZeroOptionPositions()
        {
            foreach (var op in _optionPositions.Values)
            {
                if (op.Quantity == 0)
                {
                    _optionPositions.Remove(op.Option.OptionKey);
                }
            }
        }
 
        #region private
        private void _checkDate(DateTime dateTime)
        {
            if (dateTime < _toDate)
            {
                throw new Exception($"Date mismatch. Account is at {_toDate}, but transaction is at {dateTime}.");
            }
            _toDate = dateTime;
        }
        private struct OptionPosition
        {
            public IOption Option;
            public int Quantity;
            public double OptionCash;
        }
        private void _adjustOptionPosition(IOption option, DateTime dateTime, int quantity, double price)
        {
            // Ensure correct sequence of dates
            _checkDate(dateTime);

            // quantity cannot be 0
            if (quantity == 0) { throw new Exception("Quantity cannot be 0."); }

            // price must be greater than 0
            if (price < 0.0) { throw new Exception("Price cannot be negative."); }

            // Validate we are working on the same underlying
            if (option.Underlying.Symbol != _underlying.Symbol)
            {
                throw new Exception($"Option underlying symbol({option.Underlying.Symbol}) does not match account underlying symbol({_underlying.Symbol}).");
            }

            // Update the cash adjustment
            double cashAdjustment = -option.Underlying.ConvertVar(Var.Price, Var.Value, price) * quantity;

            // Update the quantity of the option position and the cash adjustment
            OptionPosition op;
            if (!_optionPositions.TryGetValue(option.OptionKey, out op))
            {
                op = new OptionPosition { Option = option, Quantity = 0, OptionCash = 0 };
                _optionPositions[option.OptionKey] = op;
            }
            op.OptionCash += cashAdjustment;
            op.Quantity += quantity;
            _optionPositions[option.OptionKey] = op;
            _optionCash += cashAdjustment;
        }
        private IUnderlying _underlying;
        private int _equityPosition;
        private double _equityCash;
        private double _optionCash;
        private Dictionary<string, OptionPosition> _optionPositions;
        private DateTime _toDate;
        private DateTime _fromDate;
        #endregion
    }
}

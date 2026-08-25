using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionLib
{
     public enum Resolution { Year, Month, Week, Day, Hour, Minute, Second }

    // =============================================================================
    //
    // Price - price of single instrument e.g. SPX 4.5 (PUT)
    // Point - Price X ContractLot e.g. SPX 4.5 X 1 = 4.5
    // Value - Point X CurrencyPerPoint e.g. SPX 4.5 X 100 = $450
    //
    // Note: Will always transact in contract lots. If I have a Qty of 1 QQQ, it means I hold 1 contract lot of QQQ.
    // If I have a Qty of 2 QQQ, it means I hold 2 contract lots of QQQ.
    // The value of the position is Qty X ContractLot X Price X CurrencyPerPoint
    public enum Var { Price, Point, Value }
    public enum CurrencyType { USD, CAD }
    public interface IUnderlying
    {
        string Exchange { get; }
        string Symbol { get; }
        CurrencyType CurrencyType { get; }
        double CurrencyPerPoint { get; }
        int ContractLot { get; }
        double ConvertVar(Var from, Var to, double value);
    }

    // =============================================================================

    public interface IInstrument
    {
        IUnderlying Underlying { get; }
        OptionProjection ProjectTo(DateTime dateTime, double underlyingPrice, double volatility, double interest, double dividendYield, OptionProjection projection = null);
    }

    // =============================================================================
    public enum OptionType { CALL, PUT }
    public interface IOption : IInstrument
    {
         //
        // Option structure
        string OptionKey { get; }
        DateTime ExpiryDate { get; }
        OptionType OptionType { get; }
        double StrikePrice { get; }
        double DeriveVolatility(DateTime dateTime, double price, double underlyingPrice, double interest, double dividendYield);
    }



}

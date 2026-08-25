using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionLib
{
    /// <summary>
    /// Represents the Greeks of an option, including price and sensitivity measures like delta, gamma, vega, theta, and
    /// rho. It provides constructors for initializing these values.
    /// </summary>
    public struct OptionGreeks
    {
        // Price of the option
        public double Price { get; set; }
        // Sensitivity of option price to changes in underlying asset price.
        // Given a movement of 1 point in the underlying, how much does the option price change.
        public double Delta { get; set; }
        // Sensitivity of delta to changes in underlying asset price.
        // Given a movement of 1 point in the underlying, how much does the delta change.
        public double Gamma { get; set; }
        // Sensitivity of option price to changes in volatility.
        // Given a movement of 1% in volatility, how much does the option price change.
        public double Vega { get; set; }
        // Sensitivity of option price to passage of time.
        // Given a passage of 1 day, how much does the option price change.
        public double Theta { get; set; }
        // Sensitivity of option price to changes in interest rate.
        // Given a movement of 1% in interest rate, how much does the option price change.
        public double Rho { get; set; }
    }
    public static class OptionCalculator
    {
        /// <summary>
        /// Calculates option price and Greeks for European options using Black-Scholes
        /// </summary>
        /// <param name="isCall">True for call, false for put</param>
        /// <param name="S">Underlying price</param>
        /// <param name="K">Strike price</param>
        /// <param name="T">Time to expiry in years</param>
        /// <param name="r">Risk-free interest rate</param>
        /// <param name="q">Dividend yield</param>
        /// <param name="v">Volatility</param>
        /// <param name="g">Output: Greeks</param>
        public static OptionGreeks Calculate(bool isCall, double S, double K, double T, double r, double q, double v)
        {
            var g = new OptionGreeks(); 

            // Input validation
            if (S <= 0 || K <= 0 || T < MinTimeToExpiry || v < 0 || q < 0 || double.IsNaN(S + K + T + r + q + v))
            {
                g.Price = g.Delta = g.Gamma = g.Delta = g.Delta = g.Gamma = 0;
                return g;
            }

            // Pre-calculate common terms
            double sqrtT = Math.Sqrt(T);
            double vSqrtT = v * sqrtT;
            double d1 = (Math.Log(S / K) + (r - q + v * v * 0.5) * T) / vSqrtT;
            double d2 = d1 - vSqrtT;

            // Check for numerical stability
            if (double.IsInfinity(d1) || double.IsNaN(d1))
            {
                g.Price = g.Delta = g.Gamma = g.Vega = g.Theta = g.Rho = 0;
                return g;
            }

            // Calculate cumulative normal distribution
            double nd1 = NormCdf(d1);
            double nd2 = NormCdf(d2);
            double nMinusD1 = NormCdf(-d1);
            double nMinusD2 = NormCdf(-d2);

            // Cache expensive calculations
            double eMinusRt = Math.Exp(-r * T);
            double eMinusQt = Math.Exp(-q * T);
            double svSqrtT = S * vSqrtT;

            // Calculate option price
            if (isCall)
            {
                g.Price = S * eMinusQt * nd1 - K * eMinusRt * nd2;
                g.Delta = eMinusQt * nd1;
            }
            else
            {
                g.Price = K * eMinusRt * nMinusD2 - S * eMinusQt * nMinusD1;
                g.Delta = eMinusQt * (nd1 - 1.0);
            }

            // Calculate gamma (same for call and put)
            double nPrimeD1 = NormPdf(d1);
            g.Gamma = nPrimeD1 * eMinusQt / svSqrtT;

            // Calculate vega (same for call and put, scaled to per 1% change)
            g.Vega = S * eMinusQt * nPrimeD1 * sqrtT * 0.01;

            // Calculate theta
            double commonTheta = -svSqrtT * eMinusQt * nPrimeD1 / (2 * T);
            if (isCall)
            {
                g.Theta = commonTheta - q * S * eMinusQt * nd1 - r * K * eMinusRt * nd2;
            }
            else
            {
                g.Theta = commonTheta + q * S * eMinusQt * nMinusD1 + r * K * eMinusRt * nMinusD2;
            }
            g.Theta /= 365.0; // Convert to daily theta

            // Calculate rho (scaled to per 1% change)
            if (isCall)
            {
                g.Rho = K * T * eMinusRt * nd2 * 0.01;
            }
            else
            {
                g.Rho = -K * T * eMinusRt * nMinusD2 * 0.01;
            }

            return g;
        }

        /// <summary>
        /// Helper method to calculate time to expiry from datetime
        /// </summary>
        public static double CalculateTimeToExpiry(DateTime expiry, DateTime current)
        {
            if (expiry <= current)
                return MinTimeToExpiry;
            return Math.Max((expiry - current).TotalDays / 365.0, MinTimeToExpiry);
        }

        /// <summary>
        /// Calculates the implied volatility based on the market price of an option and other parameters using an
        /// iterative bisection method.
        /// </summary>
        /// <param name="price">Represents the market price of the option being analyzed.</param>
        /// <param name="isCall">Indicates whether the option is a call or a put.</param>
        /// <param name="S">Denotes the current price of the underlying asset.</param>
        /// <param name="K">Specifies the price at which the option can be exercised.</param>
        /// <param name="r">Refers to the risk-free interest rate used in the calculation.</param>
        /// <param name="q">Refers to the dividend yield</param>
        /// <param name="T">Indicates the time to expiry in years.</param>
        /// <returns>Returns the calculated volatility that matches the given market price.</returns>
        public static double Volatility(double price, bool isCall, double S, double K, double r, double q, double T)
        {
            //
            // Very difficult to calculate directly from BlackScholes. We do a iterative bisection of the volatility to close in on the price. 
            //
            double volHi = 5.0;
            double volLo = 0.0;
            double tolerance = 0.001;
            double currPrice = 0.0;
            double volatility = 0.0;
            int maxTries = 100;
            var op = new OptionGreeks();

            int i = 0;
            do
            {
                volatility = (volHi + volLo) / 2.0;
                op = Calculate(isCall, S, K, T, r, q, volatility);
                currPrice = op.Price;

                if (currPrice > price) { volHi = volatility; }
                else { volLo = volatility; }

                i++;
                if (i > maxTries) { break; }

            } while (Math.Abs(currPrice - price) > tolerance);

            return volatility;
        }

        /// <summary>
        /// Given the current price and volatility, what is the probability of the price being less than the limitPrice in the given period.
        /// </summary>
        /// <param name="beginDateTime">Begin of period</param>
        /// <param name="endDateTime">End of period</param>
        /// <param name="v">annual volatility</param>
        /// <param name="x">current price</param>
        /// <param name="lp">limit price</param>
        /// <returns>probability of price being less than limit price at the end of the period</returns>
        public static double ProbabilityOfLessThan(DateTime beginDateTime, DateTime endDateTime, double v, double x, double lp)
        {
            var stdDevs = Normalize(lp, x, VolatilityFor(v, beginDateTime, endDateTime) * x);
            return NormCdf(stdDevs);
        }

        /// <summary>
        /// Given the current price and volatility, what is the probability of the price being greater than the limitPrice in the given period.
        /// </summary>
        /// <param name="beginDateTime">Begin of period</param>
        /// <param name="endDateTime">End of period</param>
        /// <param name="v">annual volatility</param>
        /// <param name="x">current price</param>
        /// <param name="lp">limit price</param>
        /// <returns>probability of price being greater than limit price at the end of the period</returns>
        public static double ProbabilityOfGreaterThan(DateTime beginDateTime, DateTime endDateTime, double v, double x, double lp)
        {
             return 1.0 - ProbabilityOfLessThan(beginDateTime, endDateTime, v, x, lp);
        }

        /// <summary>
        /// Calculates the probability of a price falling between two thresholds over a specified time period.
        /// </summary>
        /// <param name="beginDateTime">Specifies the start of the time period for the probability calculation.</param>
        /// <param name="endDateTime">Specifies the end of the time period for the probability calculation.</param>
        /// <param name="v">Represents the volatility used in the probability calculations.</param>
        /// <param name="x">Indicates the current price for which the probability is being assessed.</param>
        /// <param name="lp">Defines the lower threshold for the price range in the probability assessment.</param>
        /// <param name="hp">Defines the upper threshold for the price range in the probability assessment.</param>
        /// <returns>Returns the probability of the price being between the specified thresholds during the given period.</returns>
        public static double ProbabilityOfBetween(DateTime beginDateTime, DateTime endDateTime, double v, double x, double lp, double hp)
        {
            var plt = ProbabilityOfLessThan(beginDateTime, endDateTime, v, x, lp);
            var pgt = ProbabilityOfGreaterThan(beginDateTime, endDateTime, v, x, hp);
            return 1.0 - plt - pgt;
        }

        #region private
        /// <summary>
        /// Approximates the cumulative distribution function of standard normal
        /// using Abramowitz and Stegun approximation
        /// </summary>
        private static double NormCdf(double x)
        {
            if (double.IsInfinity(x) || double.IsNaN(x))
                return x > 0 ? 1.0 : 0.0;

            double absX = Math.Abs(x);
            double t = 1.0 / (1.0 + 0.2316419 * absX);
            double y = ((((1.330274429 * t - 1.821255978) * t + 1.781477937) * t - 0.356563782) * t + 0.319381530) * t;
            y *= Math.Exp(-absX * absX * 0.5) / Math.Sqrt(2.0 * Math.PI);
            return x >= 0 ? 1.0 - y : y;
        }

        /// <summary>
        /// Calculates the probability density function of standard normal
        /// </summary>
        private static double NormPdf(double x)
        {
            if (double.IsInfinity(x) || double.IsNaN(x))
                return 0.0;
            return Math.Exp(-x * x * 0.5) / Math.Sqrt(2.0 * Math.PI);
        }

        /// </summary>
        /// <param name="x"></param>
        /// <param name="m"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private static double Normalize(double x, double m, double v)
        {
            return (x - m) / v;
        }

        /// <summary>
        /// Translate annualized sigma to another period. Used to calculate probability of a specific movement in a non-annual period.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="beginDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        private static double VolatilityFor(double v, DateTime beginDateTime, DateTime endDateTime)
        {
            return v * Math.Sqrt(365.0 / endDateTime.Subtract(beginDateTime).TotalDays);
        }

        // Constants for numerical stability
        private const double TwoPi = 6.283185307179586;
        private const double MinTimeToExpiry = 1e-6; // Minimum T to avoid division by zero
        #endregion
    }

}

using OptionLib;
namespace optionlib_test
{
    [TestClass]
    public sealed class TestXXX
    {
        // Data for Tests
        public struct _test_data
        {
            public OptionType Type { get; set; }
            public double UnderlyingPrice { get; set; }
            public double StrikePrice { get; set; }
            public double Interest { get; set; }
            public double Days { get; set; }
            public double Volatility { get; set; }
            public double ExpectedPrice { get; set; }
            public double ExpectedDelta { get; set; }
            public double ExpectedGamma { get; set; }
            public double ExpectedTheta { get; set; }
            public double ExpectedVega { get; set; }
            public double ExpectedRho { get; set; }
        }
        public _test_data[] PriceData = new _test_data[]
        {
            new _test_data { Type = OptionType.PUT, UnderlyingPrice = 100.0, StrikePrice = 98.0, Interest = 0.0275, Days = 7.0, Volatility = 0.17, 
                ExpectedPrice = 0.24, ExpectedDelta = -0.19, ExpectedGamma = 0.114, ExpectedTheta = -0.044, ExpectedVega = 0.037, ExpectedRho = -0.004 },
            new _test_data { Type = OptionType.CALL, UnderlyingPrice = 100.0, StrikePrice = 98.0, Interest = 0.0275, Days = 7.0, Volatility = 0.17,
                ExpectedPrice = 2.29, ExpectedDelta = 0.81, ExpectedGamma = 0.114, ExpectedTheta = -0.051, ExpectedVega = 0.037, ExpectedRho = 0.015 },

            new _test_data { Type = OptionType.PUT, UnderlyingPrice = 98.0, StrikePrice = 100.0, Interest = 0.0275, Days = 7.0, Volatility = 0.17,
                ExpectedPrice = 2.21, ExpectedDelta = -0.8, ExpectedGamma = 0.123, ExpectedTheta = -0.041, ExpectedVega = 0.039, ExpectedRho = -0.015 },
            new _test_data { Type = OptionType.CALL, UnderlyingPrice = 98.0, StrikePrice = 100.0, Interest = 0.0275, Days = 7.0, Volatility = 0.17,
                ExpectedPrice = 0.26, ExpectedDelta = 0.2, ExpectedGamma = 0.123, ExpectedTheta = -0.048, ExpectedVega = 0.039, ExpectedRho = 0.004 },

            new _test_data { Type = OptionType.PUT, UnderlyingPrice = 6004.0, StrikePrice = 6140.0, Interest = 0.0275, Days = 24.0, Volatility = 0.11,
                ExpectedPrice = 148.14, ExpectedDelta = -0.76, ExpectedGamma = 0.002, ExpectedTheta = -1.076, ExpectedVega = 4.754, ExpectedRho = -3.109 },
            new _test_data { Type = OptionType.CALL, UnderlyingPrice = 6004.0, StrikePrice = 6140.0, Interest = 0.0275, Days = 24.0, Volatility = 0.11,
                ExpectedPrice = 23.23, ExpectedDelta = 0.24, ExpectedGamma = 0.002, ExpectedTheta = -1.086, ExpectedVega = 4.754, ExpectedRho = 0.92 },

           new _test_data { Type = OptionType.PUT, UnderlyingPrice = 6004.0, StrikePrice = 5865.0, Interest = 0.0275, Days = 24.0, Volatility = 0.15,
                ExpectedPrice = 35.08, ExpectedDelta = -0.25, ExpectedGamma = 0.001, ExpectedTheta = -1.52, ExpectedVega = 4.89, ExpectedRho = -1.009 },
            new _test_data { Type = OptionType.CALL, UnderlyingPrice = 6004.0, StrikePrice = 5865.0, Interest = 0.0275, Days = 24.0, Volatility = 0.15,
                ExpectedPrice = 184.67, ExpectedDelta = 0.75, ExpectedGamma = 0.001, ExpectedTheta = -1.51, ExpectedVega = 4.89, ExpectedRho = 2.84 },

         // Add more test cases as needed
        };

        [TestMethod]
        public void TestPrice()
        {

        }
    }
}

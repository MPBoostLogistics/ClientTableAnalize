using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizerTest
{
    /// <summary>
    /// Static data for many tests (singletone). 
    /// </summary>
    public sealed class TestData
    {
        #region Variables and constants
        public const int RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT = 61138;
        public const int RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT = 1299;
        #endregion

        #region Properties

        public string TestFilesPath { get; set; }
        public SourceReportFileForSKU? TestReportFilesRansomsWB { get; set; }
        public SourceReportFileForSKU? TestReportFilesRedemptionWB { get; set; }
        public List<RangeSourceReportRows>? _testRansomsRangeSourceReportRows;
        public static Dictionary<int, List<SKURansomsBalanceDataUnit>>? SkuRansomsBalanceUnits { get; set; }
        public static Dictionary<int, SKU>? Skus { get; set; }
        #endregion

        #region Constructors
        private TestData() { }

        private static readonly Lazy<TestData> lazy = new(() => new TestData());

        public static TestData Source { get { return lazy.Value; } }
        #endregion
    }
}
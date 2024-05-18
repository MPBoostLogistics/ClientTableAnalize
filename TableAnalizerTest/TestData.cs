using ClientTableAnalizer.Objects.Managers;
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

        public List<RangeSourceReportRows>? _testRansomsRangeSourceReportRows;

        public List<RangeSourceReportRows>? _testRemainingWBRangeSourceReportRows =
        [
            new(ExcelManager.RemainingSourceCells01, ExcelManager.FIRST_ROOM_NAME),
            new(ExcelManager.RemainingSourceCells02, ExcelManager.SECOND_ROOM_NAME,(ExcelManager.LAST_MARKER_KEY, ExcelManager.LAST_MARKER_KEY_OFFSET))
        ];

        public List<RangeSourceReportRows> _testShortagesRangeSourceReportRows = 
        [
            new(ExcelManager.ShortagesSourceCells)
        ];

        public List<RangeSourceReportRows> _testResortRangeSourceReportRows = 
        [
            new(ExcelManager.ResortSourceCells)
        ];

        public List<RangeSourceReportRows> _testDummiesRangeSourceReportRows = 
        [
            new(ExcelManager.DummiesSourceCells)
        ];

        public List<RangeSourceReportRows> _testShippedRangeSourceReportRows = [new(ExcelManager.ShippedSourceCells)];

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
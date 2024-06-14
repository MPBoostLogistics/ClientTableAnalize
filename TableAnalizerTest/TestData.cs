using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizerTest
{
    /// <summary>
    /// Static data for many tests (singletone). 
    /// </summary>
    public sealed class TestData
    {
        #region Variables and constants
        public const int RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT = 74181;
        public const int RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT = 1785;
        public const int RESORT_RANGE_SOURCE_REPORT_ROWS_COUNT = 105;
        #endregion

        #region Properties

        public string TestFilesPath { get; set; }

        public List<RangeSourceReportRows>? _testRansomsRangeSourceReportRows;

        public List<RangeSourceReportRows>? _testRemainingWBRangeSourceReportRows =
        [
            new(ExcelManager.RemainingFirstRoomSourceCells, ExcelManager.FIRST_ROOM_NAME),
            new(ExcelManager.RemainingSecondRoomSourceCells, ExcelManager.SECOND_ROOM_NAME),
            new(ExcelManager.RemainingThirdRoomSourceCells, ExcelManager.THIRD_ROOM_NAME, (ExcelManager.LAST_MARKER_KEY, ExcelManager.LAST_MARKER_KEY_OFFSET))
        ];

        public List<RangeSourceReportRows>? _testRemainingOZRangeSourceReportRows =
        [
            new(ExcelManager.RemainingFirstRoomSourceCells, ExcelManager.FIRST_ROOM_NAME),
            new(ExcelManager.RemainingSecondRoomSourceCells, ExcelManager.SECOND_ROOM_NAME),
            new(ExcelManager.RemainingThirdRoomSourceCells, ExcelManager.THIRD_ROOM_NAME, (ExcelManager.LAST_MARKER_KEY, ExcelManager.LAST_MARKER_KEY_OFFSET))
        ];

        public List<RangeSourceReportRows> _testShortagesRangeSourceReportRows = 
        [
            new(ExcelManager.CompensationSourceCells)
        ];

        public List<RangeSourceReportRows> _testResortRangeSourceReportRows = 
        [
            new(ExcelManager.ResortSourceCells)
        ];

        public List<RangeSourceReportRows> _testWriteOffRangeSourceReportRows = 
        [
            new(ExcelManager.WriteOffShortagesSourceCells)
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
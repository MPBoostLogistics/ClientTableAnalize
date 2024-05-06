using ClientTableAnalizer.Enums;
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
        public const string FIRST_ROOM_NAME = "1 - Помещение приемки, сортировки и упаковки";
        public const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения и отгрузки";
        public const string LAST_MARKER_KEY = "Итого товаров в единицах хранения";
        public const int LAST_MARKER_KEY_OFFSET = 2;
        #endregion

        #region Properties

        public string TestFilesPath { get; set; }
        public SourceReportFileForSKU? TestReportFilesRansomsWB { get; set; }
        public SourceReportFileForSKU? TestReportFilesRedemptionWB { get; set; }

        public static List<SourceCell> RemainingWBSourceCells01 { get; } =
        [
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "Название товара", ParsingTypeCode.StringTypeCode, true),
            new(15, "Приход", ParsingTypeCode.IntTypeCode, false),
            new(17, "Остаток", ParsingTypeCode.IntTypeCode, false)
        ];

        public static List<SourceCell> RemainingWBSourceCells02 { get; } =
        [
            new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
            new(6, "Название товара", ParsingTypeCode.StringTypeCode, true), 
            new(17, "Остаток", ParsingTypeCode.IntTypeCode, false)
        ];

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
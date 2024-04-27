using System.Reflection.Metadata;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizerTest
{
    /// <summary>
    /// Static data for many tests. 
    /// </summary>
    public static class TestData
    {
        public static string? TestFilesPath { get; set; }
        public static SourceReportFileForSKU? TestReportFilesRansomsWB { get; set;}
        public static SourceReportFileForSKU? TestReportFilesRedemptionWB { get;set;}
        public static List<RangeSourceReportRows>? testRansomsRangeSourceReportRows;
        public static Dictionary<int, List<SKURansomsBalanceDataUnit>>? SkuRansomsBalanceUnits { get;set; }

        public static Dictionary<int, SKU>? Skus { get; set; }
        
        public const int RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT = 61138;
        public const int RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT = 1299;
    }
}
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;
using NUnit.Framework.Internal;
using OfficeOpenXml;

namespace TableAnalizerTest.Managers
{
    [TestFixture, Order(2)]
    public class T02_ExcelReportManagerTest : BaseTests
    {
        #region Constants && variables
        private const string Ransoms_Report_List_Name = "Выкупы";
        private List<SourceCell> _remainingWBSourceCells01;
        private List<SourceCell> _remainingWBSourceCells02;
        private ExcelWorksheet? _ransomsWBWorksheet;
        private ExcelWorksheet? _remainingWBWorksheet;
        private List<RangeSourceReportRows> _remainingWBRangeSourceReportRows;
        private const string FIRST_ROOM_NAME = "1 - Помещение приемки, сортировки и упаковки";
        private const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения и отгрузки";
        private const string LAST_MARKER_KEY = "Итого товаров в единицах хранения";
        private const int LAST_MARKER_KEY_OFFSET = 2;
        private const int REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_01 = 1222;
        private const int REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_02 = 1190;
        #endregion

        #region Functionality
        protected override void StartTest()
        {
            base.StartTest();

            _remainingWBSourceCells01 =
            [
                new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
                new(6, "Название товара", ParsingTypeCode.StringTypeCode, true), // TODO: Разделить на бренд и номенклатуру
                new(15, "Приход", ParsingTypeCode.IntTypeCode, false),
                new(17, "Остаток", ParsingTypeCode.IntTypeCode, false)
            ];

            _remainingWBSourceCells02 =
            [
                new(1, "SKUNumber", ParsingTypeCode.IntTypeCode, true),
                new(6, "Название товара", ParsingTypeCode.StringTypeCode, true), // TODO: Разделить на бренд и номенклатуру
                new(17, "Остаток", ParsingTypeCode.IntTypeCode, false)
            ];

            _remainingWBRangeSourceReportRows = [new(_remainingWBSourceCells01, FIRST_ROOM_NAME), new(_remainingWBSourceCells02, SECOND_ROOM_NAME, (LAST_MARKER_KEY, LAST_MARKER_KEY_OFFSET))];
        }

        [Test, Order(1)]
        public void CreateExcelPackages_Test()
        {
            // input assert
            Assert.Multiple(() =>
            {
                Assert.That(TestData.TestReportFilesRedemptionWB, Is.Not.Null);
                Assert.That(TestData.TestReportFilesRansomsWB, Is.Not.Null);
            });

            // arrange
            // ... 

            // act
            _testRansomsWBPackage = new(TestData.TestReportFilesRedemptionWB.FileInfo);
            _testRemainingWBPackage = new(TestData.TestReportFilesRansomsWB.FileInfo);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_testRansomsWBPackage, Is.Not.Null);
                Assert.That(_testRemainingWBPackage, Is.Not.Null);
            });
        }

        [Test, Order(2)]
        public void GetRangesSourceReportRows_OneRange_Test()
        {
            // input test assert
            Assert.Multiple(() =>
            {
                Assert.That(_testRansomsWBPackage, Is.Not.EqualTo(null));
                Assert.That(TestData.testRansomsRangeSourceReportRows, Is.Not.Null);
                Assert.That(TestData.TestReportFilesRedemptionWB, Is.Not.EqualTo(null));
                Assert.That(TestData.TestReportFilesRedemptionWB!.ReportKey, Is.EqualTo(SourceSKUReportFileKey.RansomsWB));


            });

            // arrange

            ExcelReportManager.GetExcelWorksheetBy(Ransoms_Report_List_Name, in _testRansomsWBPackage, out _ransomsWBWorksheet);
            ExcelReportManager.GetRangeSourceReportRows(in _ransomsWBWorksheet!, ref TestData.testRansomsRangeSourceReportRows);

            // act
            var testingRange = Enumerable.Range(2, 148706 - 1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_ransomsWBWorksheet, Is.Not.EqualTo(null));
                Assert.That(TestData.testRansomsRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingRange));
            });
        }

        [Test, Order(3)]
        public void GetRowIndexByStringKey_Test()
        {
            // input test assert
            Assert.That(_testRemainingWBPackage, Is.Not.EqualTo(null));

            // arrange
            var testFirstIndex = 14;
            var testSecondIndex = 1237;
            var testLastIndex = 2430;

            // act 

            ExcelReportManager.GetExcelWorksheetBy("TDSheet", in _testRemainingWBPackage, out _remainingWBWorksheet);

            // assert 01
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            var firstIndexInTest = ExcelReportManager.GetRowIndexBy(FIRST_ROOM_NAME, in _remainingWBWorksheet);
            var secondIndexInTest = ExcelReportManager.GetRowIndexBy(SECOND_ROOM_NAME, in _remainingWBWorksheet);
            var lastIndexInTest = ExcelReportManager.GetRowIndexBy(LAST_MARKER_KEY, in _remainingWBWorksheet);

            // assert 02
            Assert.Multiple(() =>
            {
                Assert.That(firstIndexInTest, Is.EqualTo(testFirstIndex));
                Assert.That(secondIndexInTest, Is.EqualTo(testSecondIndex));
                Assert.That(lastIndexInTest, Is.EqualTo(testLastIndex));
            });
        }

        [Test, Order(4)]
        public void GetRangesSourceReportRows_ManyRange_Test()
        {
            // input assert 01
            Assert.That(_testRemainingWBPackage, Is.Not.EqualTo(null));

            // act 
            ExcelReportManager.GetExcelWorksheetBy("TDSheet", in _testRemainingWBPackage, out _remainingWBWorksheet);

            // input test assert
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            // arrange
            var testingFirstRangeFirstIndex = 14 + 1;
            var testingFirstRange = Enumerable.Range(testingFirstRangeFirstIndex, 1237 - testingFirstRangeFirstIndex);     // 15, 1236 = 1222

            var testingSecondRangeFirstIndex = 1237 + 1;
            var testingSecondRange = Enumerable.Range(testingSecondRangeFirstIndex, 2428 - testingSecondRangeFirstIndex);  // 1238, 2428 = 1190

            // act 
            ExcelReportManager.GetRangeSourceReportRows(in _remainingWBWorksheet, ref _remainingWBRangeSourceReportRows);

            // assert 02 
            Assert.Multiple(() =>
            {
                Assert.That(testingFirstRange.Count(), Is.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_01));
                Assert.That(testingSecondRange.Count(), Is.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_02));

                Assert.That(_remainingWBRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingFirstRange));
                Assert.That(_remainingWBRangeSourceReportRows[0].RowsIndexRange!.Count, Is.EqualTo(testingFirstRange.Count()));

                Assert.That(_remainingWBRangeSourceReportRows[1].RowsIndexRange, Is.EqualTo(testingSecondRange));
                Assert.That(_remainingWBRangeSourceReportRows[1].RowsIndexRange!.Count, Is.EqualTo(testingSecondRange.Count()));
            });
        }

        [Test, Order(5)]
        public void GetRawDataFromExcelRows_SimpleRange_Test()
        {
            // input assert
            Assert.Multiple(() =>
            {
                Assert.That(_ransomsWBWorksheet, Is.Not.Null);
                Assert.That(TestData.testRansomsRangeSourceReportRows, Is.Not.Null);
            });

            // arrange
            // ...

            // act
            ExcelReportManager.GetRawDataFromExcelRows(in _ransomsWBWorksheet, ref TestData.testRansomsRangeSourceReportRows);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(TestData.testRansomsRangeSourceReportRows, Has.Count.EqualTo(1));
                Assert.That(TestData.testRansomsRangeSourceReportRows[0].RawRowItems, Has.Count.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT));
            });
        }

        [Test, Order(6)]
        public void GetRawDataFromExcelRows_ManyRange_Test()
        {
            // input assert
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            // arrange
            // ...

            // act
            ExcelReportManager.GetRawDataFromExcelRows(in _remainingWBWorksheet, ref _remainingWBRangeSourceReportRows);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_remainingWBRangeSourceReportRows, Has.Count.EqualTo(2));
                Assert.That(_remainingWBRangeSourceReportRows[0]!.RawRowItems!, Has.Count.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_01));
                Assert.That(_remainingWBRangeSourceReportRows[1]!.RawRowItems!, Has.Count.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_02));
            });
        }

        [Test, Order(7)]
        public void SaveSKURansomsBalanceUnits_Test()
        {
            // input assert
            Assert.That(TestData.testRansomsRangeSourceReportRows, Is.Not.Null);
            Assert.That(TestData.testRansomsRangeSourceReportRows, Has.Count.EqualTo(1));
            Assert.That(TestData.testRansomsRangeSourceReportRows[0].RawRowItems, Has.Count.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT));

            // arrange
            //...

            // act
            var keyCount = SKUManager.SaveSKURansomsBalanceUnits(TestData.testRansomsRangeSourceReportRows[0].RawRowItems);

            // assert
            Assert.That(keyCount, Is.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));

            TestData.SkuRansomsBalanceUnits = SKUManager.SkuRansomsBalanceUnits;
        }

        [Test, Order(8)]
        public void MakeSKUs_Test() 
        {
            // input assert
            Assert.That(TestData.SkuRansomsBalanceUnits, Is.Not.Null);
            Assert.That(TestData.SkuRansomsBalanceUnits, Has.Count.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));

            // arrange
            SKUManager.SkuRansomsBalanceUnits = TestData.SkuRansomsBalanceUnits;

            // act
            _skuManager.MakeSKUs();

            // assert
            Assert.That(_skuManager.SKUsCount, Is.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));

            TestData.Skus = _skuManager.SKUs;
        }

        [Test, Order(9)]
        public void SKURansomsBalances_Test() 
        {
             // input assert
            Assert.That(TestData.Skus, Is.Not.Null);
            Assert.That(TestData.Skus, Has.Count.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));

            // arrange
            Dictionary<int,SKURansomsBalance> testSKURansomsBalances = new() 
            {
                {15320659, new SKURansomsBalance(onWay: 0, canceled: 0, delivered: 0, withDrawn: 10)},
                {216237114, new SKURansomsBalance(onWay: 2, canceled: 0, delivered: 1, withDrawn: 0)},
                {214569047, new SKURansomsBalance(onWay: 3, canceled: 0, delivered: 1, withDrawn: 3)}
            };

            // act
            var sku15320659RansomsBalance = TestData.Skus[15320659]?.SKURansomsBalance;
            var sku216237114RansomsBalance = TestData.Skus[216237114]?.SKURansomsBalance;
            var sku214569047RansomsBalance = TestData.Skus[214569047]?.SKURansomsBalance;

            // assert  
            Assert.Multiple(() =>
            {
                Assert.That(sku15320659RansomsBalance, Is.Not.Null);
                Assert.That(testSKURansomsBalances[15320659], Is.EqualTo(sku15320659RansomsBalance));

                Assert.That(sku216237114RansomsBalance, Is.Not.Null);
                Assert.That(testSKURansomsBalances[216237114], Is.EqualTo(sku216237114RansomsBalance));

                Assert.That(sku214569047RansomsBalance, Is.Not.Null);
                Assert.That(testSKURansomsBalances[214569047], Is.EqualTo(sku214569047RansomsBalance));
            });
        }

        protected override void EndTest()
        {
            base.EndTest();

            _testRansomsWBPackage?.Dispose();
            _testRemainingWBPackage?.Dispose();
            
        }
        #endregion

        // arrange
        // ... 

        // act
        // ... 

        // assert
        // ...

        // Trace.WriteLine("...");
    }
}
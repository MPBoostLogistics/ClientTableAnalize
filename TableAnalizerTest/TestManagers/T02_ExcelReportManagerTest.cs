using System.Diagnostics;
using System.Globalization;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;
using NUnit.Framework.Internal;
using OfficeOpenXml;

namespace TableAnalizerTest.Managers
{
    [TestFixture, Order(2)]
    public class T02_ExcelReportManagerTest : BaseTests
    {
        #region Constants && variables
        private ExcelWorksheet? _ransomsWBWorksheet;
        private ExcelWorksheet? _remainingWBWorksheet;
        private ExcelWorksheet? _shippedWorksheet;
        private const int REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_01 = 1650;
        private const int REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_02 = 10;
         private const int REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_03 = 1625;
        private const int SHIPPED_RANGE_SOURCEREPORT_ROWSCOUNT = 5188;
        private const int SHIPPED_RANGE_SOURCEREPORT_SKUSCOUNT = 1228; 
        #endregion

        #region Functionality

        [Test, Order(1)]
        public void CreateExcelPackages_Test()
        {
            var testRansomsWBFilePath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (WB).xlsx";
            var testRemainingWBilePath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов приходы и остатки 150324 (WB).xlsx";
            var testShippedFilePath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов расходы 22032024.xlsx";

            // act
            ExcelManager.CreateExcelPackage(SourceSKUReportFileKey.RansomsWB, new FileInfo(testRansomsWBFilePath));
            ExcelManager.CreateExcelPackage(SourceSKUReportFileKey.RemainingWB, new FileInfo(testRemainingWBilePath));
            ExcelManager.CreateExcelPackage(SourceSKUReportFileKey.Shipped, new FileInfo(testShippedFilePath));

            ExcelManager.GetExcelPackageByKey(SourceSKUReportFileKey.RansomsWB, out ExcelPackage? testRansomsWBPackage);
            ExcelManager.GetExcelPackageByKey(SourceSKUReportFileKey.RemainingWB, out ExcelPackage? testRemainingWBPackage);
            ExcelManager.GetExcelPackageByKey(SourceSKUReportFileKey.Shipped, out ExcelPackage? testShippedPackage);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(testRansomsWBPackage, Is.Not.Null);
                Assert.That(testRemainingWBPackage, Is.Not.Null);
                Assert.That(testShippedPackage, Is.Not.Null);
            });
        }

        [Test, Order(2)]
        public void GetRansomsRangesSourceReportRows_OneRange_Test()
        {
            // input assert
            Assert.That(TestData._testRansomsRangeSourceReportRows, Is.Not.Null);

            // arrange
            ExcelManager.GetExcelWorksheetByName(ExcelManager.RANSOMS_WORKSHEET_NAME, SourceSKUReportFileKey.RansomsWB, out _ransomsWBWorksheet);
            ExcelManager.GetRangeSourceReportRows(in _ransomsWBWorksheet!, ref TestData._testRansomsRangeSourceReportRows);

            // act
            var testingRange = Enumerable.Range(2, 148706 - 1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_ransomsWBWorksheet, Is.Not.EqualTo(null));
                Assert.That(TestData._testRansomsRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingRange));
            });
        }

        [Test, Order(3)]
        public void GetShippedRangesSourceReportRows_OneRange_Test() 
        {
            // input test assert
            Assert.That(TestData._testShippedRangeSourceReportRows, Is.Not.Null);

            // arrage
            ExcelManager.GetExcelWorksheetByName(ExcelManager.REMAINING_OR_SHIPPED_WORKSHEET_NAME, SourceSKUReportFileKey.Shipped, out _shippedWorksheet);
            ExcelManager.GetRangeSourceReportRows(in _shippedWorksheet!, ref TestData._testShippedRangeSourceReportRows);

            IEnumerable<int>? testingRange = Enumerable.Range(2, 6448 - 1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_shippedWorksheet, Is.Not.EqualTo(null));
                Assert.That(TestData._testShippedRangeSourceReportRows, Is.Not.Null);
                Assert.That(TestData._testShippedRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingRange));
            });
        }

        [Test, Order(4)]
        public void GetRowIndexByStringKey_Test()
        {
            // input test assert
            ExcelManager.GetExcelPackageByKey(SourceSKUReportFileKey.RemainingWB, out ExcelPackage? testRemainingWBPackage);
            Assert.That(testRemainingWBPackage, Is.Not.EqualTo(null));

            // arrange
            var testFirstIndex = 14;
            var testSecondIndex = 1665;
            var testThirdIndex = 1676;
            var testLastIndex = 3304;

            // act 

            ExcelManager.GetExcelWorksheetByName(ExcelManager.REMAINING_OR_SHIPPED_WORKSHEET_NAME, SourceSKUReportFileKey.RemainingWB, out _remainingWBWorksheet);

            // assert 01
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            ExcelManager.GetRowIndexBy(ExcelManager.FIRST_ROOM_NAME, in _remainingWBWorksheet, out int? firstIndexInTest);
            ExcelManager.GetRowIndexBy(ExcelManager.SECOND_ROOM_NAME, in _remainingWBWorksheet, out int? secondIndexInTest);
            ExcelManager.GetRowIndexBy(ExcelManager.THIRD_ROOM_NAME, in _remainingWBWorksheet, out int? testThirdIndexInTest);
            ExcelManager.GetRowIndexBy(ExcelManager.LAST_MARKER_KEY, in _remainingWBWorksheet, out int? lastIndexInTest);

            // assert 02
            Assert.Multiple(() =>
            {
                Assert.That(firstIndexInTest, Is.EqualTo(testFirstIndex));
                Assert.That(secondIndexInTest, Is.EqualTo(testSecondIndex));
                Assert.That(testThirdIndexInTest, Is.EqualTo(testThirdIndex));
                Assert.That(lastIndexInTest, Is.EqualTo(testLastIndex));
            });
        }

        [Test, Order(5)]
        public void GetRangesSourceReportRows_ManyRange_Test()
        {
            // input assert 01
            ExcelManager.GetExcelPackageByKey(SourceSKUReportFileKey.RemainingWB, out ExcelPackage? testRemainingWBPackage);
            Assert.That(testRemainingWBPackage, Is.Not.EqualTo(null));

            // act 
            ExcelManager.GetExcelWorksheetByName("TDSheet", SourceSKUReportFileKey.RemainingWB, out _remainingWBWorksheet);

            // input test assert
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            // arrange
            var testingFirstRangeFirstIndex = 14 + 1;
            var testingFirstRange = Enumerable.Range(testingFirstRangeFirstIndex, 1665 - testingFirstRangeFirstIndex);  // 1650  

            var testingSecondRangeFirstIndex = 1665 + 1;
            var testingSecondRange = Enumerable.Range(testingSecondRangeFirstIndex, 1676 - testingSecondRangeFirstIndex);  // 10 

            var testingThirdRangeFirstIndex = 1676 + 1;
            var testingThirdRange = Enumerable.Range(testingThirdRangeFirstIndex, 3302 - testingThirdRangeFirstIndex);      // 1625

            // act 
            ExcelManager.GetRangeSourceReportRows(in _remainingWBWorksheet, ref TestData._testRemainingWBRangeSourceReportRows!);

            // assert 02 
            //Assert.Multiple(() =>
            //{
                Assert.That(testingFirstRange.Count(), Is.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_01));
                Assert.That(testingSecondRange.Count(), Is.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_02));
                Assert.That(testingThirdRange.Count(), Is.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_03));

                Assert.That(TestData._testRemainingWBRangeSourceReportRows, Is.Not.Null);
                Assert.That(TestData._testRemainingWBRangeSourceReportRows![0], Is.Not.Null);
                Assert.That(TestData._testRemainingWBRangeSourceReportRows![1], Is.Not.Null);
                Assert.That(TestData._testRemainingWBRangeSourceReportRows![2], Is.Not.Null);

                Assert.That(TestData._testRemainingWBRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingFirstRange));
                Assert.That(TestData._testRemainingWBRangeSourceReportRows[0].RowsIndexRange!.Count, Is.EqualTo(testingFirstRange.Count()));

                Assert.That(TestData._testRemainingWBRangeSourceReportRows[1].RowsIndexRange, Is.EqualTo(testingSecondRange));
                Assert.That(TestData._testRemainingWBRangeSourceReportRows[1].RowsIndexRange!.Count, Is.EqualTo(testingSecondRange.Count()));

                Assert.That(TestData._testRemainingWBRangeSourceReportRows[2].RowsIndexRange, Is.EqualTo(testingThirdRange));
                Assert.That(TestData._testRemainingWBRangeSourceReportRows[2].RowsIndexRange!.Count, Is.EqualTo(testingThirdRange.Count()));
            //});
        }

        [Test, Order(6)]
        public void GetRawDataFromExcelRows_SimpleRange_Test()
        {
            // input assert
            Assert.Multiple(() =>
            {
                Assert.That(_ransomsWBWorksheet, Is.Not.Null);
                Assert.That(TestData._testRansomsRangeSourceReportRows, Is.Not.Null);
                Assert.That(_shippedWorksheet, Is.Not.Null);
                Assert.That(TestData._testShippedRangeSourceReportRows, Is.Not.Null);
            });

            // arrange
            // ...

            // act
            ExcelManager.GetRawDataFromExcelRows(in _ransomsWBWorksheet, ref TestData._testRansomsRangeSourceReportRows);
            ExcelManager.GetRawDataFromExcelRows(in _shippedWorksheet, ref TestData._testShippedRangeSourceReportRows);

            foreach (var item in TestData._testShippedRangeSourceReportRows[0].RowsWithRawData)
            {
                Trace.WriteLine(item);
            }
            // assert
           // Assert.Multiple(() =>
           // {
                Assert.That(TestData._testRansomsRangeSourceReportRows, Has.Count.EqualTo(1));
                Assert.That(TestData._testRansomsRangeSourceReportRows[0].RowsWithRawData, Has.Count.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT));
                Assert.That(TestData._testShippedRangeSourceReportRows, Has.Count.EqualTo(1));
                Assert.That(TestData._testShippedRangeSourceReportRows[0].RowsWithRawData, Has.Count.EqualTo(SHIPPED_RANGE_SOURCEREPORT_ROWSCOUNT));
            //});
        }

        [Test, Order(7)]
        public void GetRawDataFromExcelRows_ManyRange_Test()
        {
            // input assert
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            // arrange
            // ...

            // act
            ExcelManager.GetRawDataFromExcelRows(in _remainingWBWorksheet, ref TestData._testRemainingWBRangeSourceReportRows!);

            // assert
            Assert.Multiple(() =>
            {
                //TestData._testRemainingWBRangeSourceReportRows[0]
                Assert.That(TestData._testRemainingWBRangeSourceReportRows, Has.Count.EqualTo(3));
                Assert.That(TestData._testRemainingWBRangeSourceReportRows![0]!.RowsWithRawData!, Has.Count.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_01));
                Assert.That(TestData._testRemainingWBRangeSourceReportRows![1]!.RowsWithRawData!, Has.Count.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_02));
                Assert.That(TestData._testRemainingWBRangeSourceReportRows![2]!.RowsWithRawData!, Has.Count.EqualTo(REMAINING_WB_RANGE_SOURCEREPORT_ROWSCOUNT_03));
            });
        }

        [Test, Order(8)]
        public void SaveSKUBalanceUnits_Test()
        {
            // input assert
            Assert.That(TestData._testRansomsRangeSourceReportRows, Is.Not.Null);
            Assert.That(TestData._testRansomsRangeSourceReportRows, Has.Count.EqualTo(1));
            Assert.That(TestData._testRansomsRangeSourceReportRows[0].RowsWithRawData, Has.Count.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_ROWS_COUNT));

            // arrange
            //...

            // act
            var keyCount = _skuManager.SaveSKUBalanceUnits(SourceSKUReportFileKey.RansomsWB, ExcelManager.RANSOMS_WORKSHEET_NAME, TestData._testRansomsRangeSourceReportRows[0].RowsWithRawData);

            // assert
            Assert.That(keyCount, Is.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));
        }

        [Test, Order(9)]
        public async Task MakeSKUs_Test()
        {
            // input assert
            Assert.That(SKUManager.Equals, Is.Not.Null);
            Assert.That(_skuManager.SkuRansomsBalanceUnits.Count, Is.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));

            // arrange
            // ... 

            // act
            var task = _skuManager.UpdateSKUsFromBalanceUnits(SourceSKUReportFileKey.RansomsWB, ExcelManager.RANSOMS_WORKSHEET_NAME);
            await Task.Run(async () => await task);
            task.Wait();

            // assert
            Assert.That(_skuManager.SKUsCount, Is.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));
        }

        [Test, Order(10)]
        public void SKURansomsBalances_Test()
        {
            // input assert
            Assert.That(_skuManager.SKUs, Is.Not.Null);
            Assert.That(_skuManager.SKUs.Count, Is.EqualTo(TestData.RANSOMS_WB_RANGE_SOURCE_REPORT_SKUNUMBERS_COUNT));

            // arrange
            Dictionary<int, SKURansomsBalance> testSKURansomsBalances = new()
            {
                {15320659, new SKURansomsBalance(onWay: 0, canceled: 0, delivered: 0, withDrawn: 10)},
                {216237114, new SKURansomsBalance(onWay: 2, canceled: 0, delivered: 1, withDrawn: 0)},
                {214569047, new SKURansomsBalance(onWay: 3, canceled: 0, delivered: 1, withDrawn: 3)}
            };

            // act
            _skuManager.SKUs.GetValue(15320659, out SKU? sku1);
            var sku15320659RansomsBalance = sku1?.SKURansomsBalance;
            _skuManager.SKUs.GetValue(216237114, out SKU? sku2);
            var sku216237114RansomsBalance = sku2?.SKURansomsBalance;
            _skuManager.SKUs.GetValue(214569047, out SKU? sku3);
            var sku214569047RansomsBalance = sku3?.SKURansomsBalance;

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

        [Test, Order(11)]
        public void SKUMaxRansomsDate_Test()
        {
            // arrange
            var gregorian = new GregorianCalendar();
            DateOnly maxSku15320659RansomsDate = new(2023, 12, 29, gregorian);
            DateOnly maxSku216237114RansomsDate = new(2024, 03, 22, gregorian);
            DateOnly maxSku214569047RansomsDate = new(2024, 03, 21, gregorian);

            // act
            _skuManager.SKUs.GetValue(15320659, out SKU? sku15320659);
            _skuManager.SKUs.GetValue(216237114, out SKU? sku216237114);
            _skuManager.SKUs.GetValue(214569047, out SKU? sku214569047);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_skuManager.SKUs, Is.Not.Null);
                Assert.That(maxSku15320659RansomsDate, Is.EqualTo(sku15320659?.MaxRansomsDate));
                Assert.That(maxSku216237114RansomsDate, Is.EqualTo(sku216237114?.MaxRansomsDate));
                Assert.That(maxSku214569047RansomsDate, Is.EqualTo(sku214569047?.MaxRansomsDate));
            });

        }

        [Test, Order(12)]
        public void GetPropertyValue_Test() 
        {
            // https://code-maze.com/csharp-get-a-value-of-a-property-by-using-its-name/

            // arrange
            var testSku = _skuManager.SKUs.GetValuesList().OrderBy(s => s.value.MaxRansomsDate).First().value;
            int testSku157270554_Number = 157270554;
            Marketplace testSku157270554_Marketplace = Marketplace.WB; 
            string testSku157270554_Name = "No name";
            string testSku157270554_Brand = "No brand";
            double testSku157270554_Average = 2000.00;
            DateOnly testSku157270554_Date = new(2023, 06, 23);


            // act
            var numberSuccess = _skuManager.TryGetPropertyValue<int,SKU>(testSku,"SKUNumber", out var number);
            var marketplaceSuccess = _skuManager.TryGetPropertyValue<Marketplace,SKU>(testSku,"Marketplace", out var marketplace);
            var nameSuccess = _skuManager.TryGetPropertyValue<string,SKU>(testSku,"SKUName", out var name);
            var brandSuccess = _skuManager.TryGetPropertyValue<string,SKU>(testSku,"SKUBrand", out var brand);
            var averageSuccess = _skuManager.TryGetPropertyValue<double,SKU>(testSku,"AveragePrice", out var average);
            var dateSuccess = _skuManager.TryGetPropertyValue<DateOnly,SKU>(testSku,"MaxRansomsDate", out var date);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(testSku157270554_Number, Is.EqualTo(number));
                Assert.That(testSku157270554_Marketplace, Is.EqualTo(marketplace));
                Assert.That(testSku157270554_Name, Is.EqualTo(name));
                Assert.That(testSku157270554_Brand, Is.EqualTo(brand));
                Assert.That(testSku157270554_Average, Is.EqualTo(average));
                Assert.That(testSku157270554_Date, Is.EqualTo(date));
            });
        }

        [Test, Order(13)]
        public void GetCompensationSKU_Test() 
        {
            
        }

        [Test, Order(14)]
        public void GetResortSKU_Test() 
        {
            // input assert / arrage
            Assert.That(TestData._testResortRangeSourceReportRows, Is.Not.Null);

            // Create ExcelPackage
            var additionalReportsPath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов дополнительный 220224.xlsx";
            var additionalReportsPackage = ExcelManager.CreateExcelPackage(SourceSKUReportFileKey.Additional, new FileInfo(additionalReportsPath));
            ExcelManager.GetExcelWorksheetByName(ExcelManager.RESORTS_REPORT_WORKSHEET_NAME, SourceSKUReportFileKey.Additional, out var _resortWorksheet);
            ExcelManager.GetRangeSourceReportRows(in _resortWorksheet!, ref TestData._testResortRangeSourceReportRows);
            ExcelManager.GetRawDataFromExcelRows(in _resortWorksheet!, ref TestData._testResortRangeSourceReportRows);

            var testingResortRange = Enumerable.Range(2, 105);

            Assert.Multiple(() =>
            {
                Assert.That(additionalReportsPackage, Is.Not.Null);
                Assert.That(TestData._testResortRangeSourceReportRows, Has.Count.EqualTo(1));
            });

            Assert.Multiple(() =>
            {
                Assert.That(TestData._testResortRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingResortRange));
                Assert.That(TestData._testResortRangeSourceReportRows[0].RowsWithRawData, Has.Count.EqualTo(TestData.RESORT_RANGE_SOURCE_REPORT_ROWS_COUNT));
            });


            additionalReportsPackage?.Dispose();
        }

        [Test, Order(15)]
        public void GetDummiesSKU_Test() 
        {
            
        }

        [Test, Order(16)]
        public void CreateOutputReport() 
        {
            // input assert 01  
            Assert.That(FileManager.OutputClientReportFilePath, Is.Not.Null);

            // act 01
            var outputPackage = new ExcelPackage(new FileInfo(FileManager.OutputClientReportFilePath));

            // assert 02
            Assert.That(outputPackage, Is.Not.Null);

            // act 02
            var reportSheetName = ExcelManager.OUTPUT_REPORT_WORKSHEET01_NAME;
            ExcelManager.CreateExcelWorkSheet(reportSheetName, in outputPackage, out var sheet, _excelManager.Colors[SKUSellmentStatus.FullSet]);

             // assert 03
            Assert.That(sheet, Is.Not.Null);

            // act 03
            var outputSkus = _skuManager.SKUs.GetValuesList().Select(s => s.value.GetSKUForTargetReports).ToList();
            FileManager.DeleteExistingFile(FileManager.OutputClientReportFilePath);
            _excelManager.FillingСellsWithData<SKUForTargetReports>(outputPackage, ref sheet, in outputSkus, _excelManager.mainReportSKUInfoCells, FileManager.OutputClientReportFilePath); 

            // ... 
            outputPackage.Dispose();
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


/*
    Указание листа
    Указание соответствия SorceCells и TargetCells
    Проход по книге и заполнение 
    Настройки хидера и тп
    Сохранение
*/

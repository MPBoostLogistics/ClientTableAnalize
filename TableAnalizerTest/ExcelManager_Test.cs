using System.Diagnostics;
using OfficeOpenXml;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.Parsing;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizerTest
{
    [Order(3)]
    public class ExcelManager_Test : Base_Test
    {
        #region Fields
        private SourceReportFileForSKU? sourceReportFileForSKURansoms;
        private SourceSKUReportFileKey ransomsKey;

        #endregion

        [Test, Order(2)]
        public async Task GetSourceReport_Test() 
        {
            var getSourceReportFilesTask = reportFileManager.GetSourceReportFiles(testSingleClientFileInfos);
            await Task.Run(() => getSourceReportFilesTask);
            getSourceReportFilesTask.Wait();

            // arrange
            Task<SourceReportFileForSKU?> getSourceReportTask = reportFileManager.GetSourceReport(SourceSKUReportFileKey.Ransoms);
    
            // act
            await Task.Run(() => getSourceReportTask);
            getSourceReportTask.Wait();
            sourceReportFileForSKURansoms = getSourceReportTask.Result;

            // assert 
            Assert.That(sourceReportFileForSKURansoms, Is.Not.Null);
        }

        [Test, Order(3)]
        public void CreateExcelPackageForRansoms_Test() 
        {
            // arrange
            Assert.Multiple(() =>
            {
                Assert.That(sourceReportFileForSKURansoms, Is.Not.Null);
                Assert.That(sourceReportFileForSKURansoms!.ReportKey, Is.Not.Null);
                Assert.That(sourceReportFileForSKURansoms!.FileInfo, Is.Not.Null);
            });

            // act
            ransomsKey = (SourceSKUReportFileKey)sourceReportFileForSKURansoms!.ReportKey;
            using var ransomsPackage = excelManager.CreateExcelPackage(ransomsKey, sourceReportFileForSKURansoms!.FileInfo);
            excelManager.GetExcelPackageByKey(ransomsKey, out ExcelPackage? package);
            
            // assert 
            Assert.That(package, Is.Not.Null);
        }

        [Test, Order(4)]
        public void GetExcelWorksheetByName_Test() 
        {
            // arrange
            // ...

            // act
            //excelManager.GetExcelWorksheetByName(ExcelManager.)

            // assert 
            // ... 
        }

        [Test, Order(5)]
        public void GetExcelWorksheetStringNamesByKey_Test() 
        {
            // arrange
            // ...

            // act
            ExcelManager.GetExcelWorksheetStringNamesByKey(ransomsKey, out List<string>? workSheetNames);

            // assert 
            Assert.That(workSheetNames, Is.Not.Null);
            Assert.That(workSheetNames, Has.Count.EqualTo(1));
            Assert.That(workSheetNames.First(), Is.EqualTo(ExcelManager.RANSOMS_WORKSHEET_NAME));
        }

        [Test, Order(6)]
        public void MakeRangeSourceReportRowsByKey_Test() 
        {
            // arrange
            // ...

            // act
            ExcelManager.MakeRangeSourceReportRowsByKey(ransomsKey, ExcelManager.RANSOMS_WORKSHEET_NAME, out List<RangeSourceReportRows>? ransomsRangeSourceReportRows);

            // assert 
            Assert.That(ransomsRangeSourceReportRows, Has.Count.EqualTo(1));
            Assert.That(ransomsRangeSourceReportRows[0].SourceCells, Is.EqualTo(ExcelManager.RansomsSourceCells));

        }

        [Test, Order(7)]
        public async Task GetRangeSourceReportRows_Test() 
        {
            var excelPackage = new ExcelPackage (sourceReportFileForSKURansoms!.FileInfo);

            var currentWorksheetTask = excelManager?.GetExcelWorksheetByName(ExcelManager.RANSOMS_WORKSHEET_NAME, ransomsKey, excelPackage!) ;
            await Task.Run(() => currentWorksheetTask);
            currentWorksheetTask?.Wait();
            var ransomsWorkSheet = currentWorksheetTask?.Result;

            ExcelManager.MakeRangeSourceReportRowsByKey(ransomsKey, ExcelManager.RANSOMS_WORKSHEET_NAME, out List<RangeSourceReportRows>? ransomsRangeSourceReportRows);

            // arrange
            Assert.Multiple(() =>
            {
                Assert.That(ransomsWorkSheet, Is.Not.Null);
                Assert.That(ransomsRangeSourceReportRows, Is.Not.Null);
            });

            // act
            var getRangeSourceReportRowsTask = ExcelManager.GetRangeSourceReportRows(in ransomsWorkSheet, ref ransomsRangeSourceReportRows);
            await Task.Run(() => getRangeSourceReportRowsTask);
            getRangeSourceReportRowsTask.Wait();

            // assert 
            Assert.That(ransomsRangeSourceReportRows[0].RangeRowIndexesСollection, Is.Not.Null);
            Assert.That(ransomsRangeSourceReportRows[0].RangeRowIndexesСollection.ToList(), Has.Count.EqualTo(108));

            excelPackage?.Dispose();
        }

        [Test, Order(8)]
        public async Task GetRawDataFromExcelRows_Test() 
        {
            // arrange
            using var excelPackage = new ExcelPackage (sourceReportFileForSKURansoms!.FileInfo);

            ExcelManager.GetExcelWorksheetStringNamesByKey(ransomsKey, out List<string>? workSheetNames);

            Assert.That(workSheetNames, Is.Not.Null);
            Assert.That(workSheetNames, Has.Count.EqualTo(1));
            Assert.That(workSheetNames[0], Is.EqualTo(ExcelManager.RANSOMS_WORKSHEET_NAME));

            var currentWorksheetTask = excelManager?.GetExcelWorksheetByName(workSheetNames[0], ransomsKey, excelPackage) ;
            await Task.Run(() => currentWorksheetTask);
            currentWorksheetTask?.Wait();
            var currentWorksheet = currentWorksheetTask?.Result;

            Assert.That(currentWorksheet, Is.Not.Null);
            Assert.That(currentWorksheet.Name, Is.EqualTo(ExcelManager.RANSOMS_WORKSHEET_NAME));

            ExcelManager.MakeRangeSourceReportRowsByKey(ransomsKey, workSheetNames[0], out List<RangeSourceReportRows> rangeSourceReportRows);

            Assert.That(rangeSourceReportRows, Is.Not.Null);
            Assert.That(rangeSourceReportRows, Has.Count.EqualTo(1));

            var getRangeSourceReportRowsTask = ExcelManager.GetRangeSourceReportRows(in currentWorksheet, ref rangeSourceReportRows);
            await Task.Run(() => getRangeSourceReportRowsTask);
            getRangeSourceReportRowsTask.Wait();

            Assert.That(rangeSourceReportRows[0].SourceCells, Is.EqualTo(ExcelManager.RansomsSourceCells));
            Assert.That(rangeSourceReportRows[0].RangeRowIndexesСollection, Is.Not.Null);
            Assert.That(rangeSourceReportRows[0].RangeRowIndexesСollection.ToList(), Has.Count.EqualTo(108));

            // act 
            var getRawDataFromExcelRowsTask = ExcelManager.GetRawDataFromExcelRows(in currentWorksheet, ref rangeSourceReportRows);
            await Task.Run(() => getRawDataFromExcelRowsTask);
            getRawDataFromExcelRowsTask.Wait();

            Assert.That(rangeSourceReportRows[0].DataOnReadRowsCollection, Is.Not.Null);
            Assert.That(rangeSourceReportRows[0].DataOnReadRowsCollection, Has.Count.EqualTo(108));
        }

    }
}

// arrange
// ...

// act
// ...

// assert 
// ... 
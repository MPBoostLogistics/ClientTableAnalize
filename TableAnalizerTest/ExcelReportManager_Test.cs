using System.Diagnostics;
using System.Runtime.InteropServices;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using OfficeOpenXml;

namespace TableAnalizerTest.Managers
{
    public class ExcelReportManager_Test : BaseTests
    {
        #region Constants && variables
        private ExcelReportManager _excelManager;
        private string _testFilesPath;
        private readonly Dictionary<SourceReportFileKey, SourceReportFileForSKU> testReportFilesForSKU = [];
        private const string Redemtion_Report_List_Name = "Выкупы";
        private List<SourceCell> _redemtionWBSourceCells;
        private List<SourceCell> _remainingWBSourceCells01;
        private List<SourceCell> _remainingWBSourceCells02;
        private List<RangeSourceReportRows> _redemtionWBRangeSourceReportRows;
        private SourceReportFileForSKU? _testReportFilesRemainingWB;
        private SourceReportFileForSKU? _testReportFilesRedemptionWB;
        private ExcelWorksheet? _redemtionWBWorksheet;
        private ExcelWorksheet? _remainingWBWorksheet;
        private List<RangeSourceReportRows> _remainingWBRangeSourceReportRows;
        private ExcelPackage? _testRemainingWBPackage;
        private ExcelPackage? _testRedemptionWBPackage;
        private const string SPACE_SEPARATOR = "\u0020";
        private const string FIRST_ROOM_NAME = "1 - Помещение приемки, сортировки и упаковки";
        private const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения и отгрузки";
        private const string LAST_MARKER_KEY = "Итого товаров в единицах хранения";
        private const int LAST_MARKER_KEY_OFFSET = 2;
        #endregion

        #region Functionality
        protected override void StartTest()
        {
            base.StartTest();
            _excelManager = new();
            _testFilesPath = string.Concat(_excelManager.ProjectDirectory, "/testFiles");

            //_redemtionFileWBFileInfo = new FileInfo(Redemtion_File_WB_Path);

            _redemtionWBSourceCells =
            [
                new(1, "Артикул", typeof(int), true),
                new(9, "Статус доставки", typeof(string), true),
                new(10, "Логистика", typeof(string), true, "Да"),
                new(14, "Стоимость товара",  typeof(string), true) // 1137 руб.
            ];

            _redemtionWBRangeSourceReportRows = [new(_redemtionWBSourceCells)];

            _remainingWBSourceCells01 =
            [
                new(1, "Артикул", typeof(int), true),
                new(6, "Название товара", typeof(string), true), // TODO: Разделить на бренд и номенклатуру
                new(15, "Приход", typeof(int), false),
                new(17, "Остаток", typeof(int), false)
            ];

            _remainingWBSourceCells02 =
            [
                new(1, "Артикул", typeof(int), true),
                new(6, "Название товара", typeof(string), true), // TODO: Разделить на бренд и номенклатуру
                new(17, "Остаток", typeof(int), true)
            ];

            _remainingWBRangeSourceReportRows = [new(_remainingWBSourceCells01, FIRST_ROOM_NAME), new(_remainingWBSourceCells02, SECOND_ROOM_NAME, (LAST_MARKER_KEY, LAST_MARKER_KEY_OFFSET))];

            
        }

        [Test, Order(1)]
        public void FilesPath_Test()
        {
            // arrange
            // ... 

            // act 1
            var dirInfo = new DirectoryInfo(_testFilesPath);

            // assert 1
            Assert.That(dirInfo, Is.Not.EqualTo(null));

            // act 2
            var fileInfos = from fileInfo in dirInfo.GetFiles()
                            where fileInfo.Name.EndsWith(".xlsx")
                            select fileInfo;

            // assert 2
            Assert.That(fileInfos, Is.Not.EqualTo(null));
            Assert.That(fileInfos.Count(), Is.EqualTo(8));
        }

        [Test, Order(2)]
        public void SplitStringIntoSubstrings_Test()
        {
            // arrange
            var testPath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (Ozon).xlsx";
            var testFileInfo = new FileInfo(testPath);

            // act
            var succsess = SourceReportFileForSKU.SplitStringIntoSubstrings(Path.GetFileName(testFileInfo.FullName), [SPACE_SEPARATOR], out string[]? subStrings);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(succsess, Is.EqualTo(true));
                Assert.That(subStrings, Has.Length.EqualTo(5));
            });
        }

        [Test, Order(3)]
        public void GetReportFiles_Test()
        {
            // arrange
            // ...

            // act
            var getReportFilesTask = _excelManager.GetReportFiles(_testFilesPath);
            getReportFilesTask.Wait();

            //Trace.WriteLine($"{_excelManager.ReportsPaths.Count}");

            var succsess = _excelManager.IsRequiredFilesProvided(out List<SourceReportFileKey> lostKeys);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_excelManager.ReportsPaths, Has.Count.EqualTo(8));
                Assert.That(succsess, Is.EqualTo(true));
                Assert.That(_excelManager.ClientLogin, Is.EqualTo("79164372997"));
                Assert.That(_excelManager.GetClientOutputPath, Is.Not.EqualTo(null));
            });

            // Set redemtionWB file path
            testReportFilesForSKU.Add(SourceReportFileKey.RedemtionWB, _excelManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceReportFileKey.RedemtionWB).Value);
            testReportFilesForSKU.Add(SourceReportFileKey.RemainingWB, _excelManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceReportFileKey.RemainingWB).Value);


            _testReportFilesRemainingWB = testReportFilesForSKU[SourceReportFileKey.RemainingWB];
            _testReportFilesRedemptionWB = testReportFilesForSKU[SourceReportFileKey.RedemtionWB];

            _testRemainingWBPackage = new(_testReportFilesRemainingWB.FileInfo);
            _testRedemptionWBPackage = new(_testReportFilesRedemptionWB.FileInfo);
        }

        [Test, Order(4)]
        public void GetReportFile_Test()
        {
            // inpet test assert
            Assert.That(_testReportFilesRedemptionWB, Is.Not.EqualTo(null));

            // act 1
            var getReportFilesTask = _excelManager.GetReportFiles(_testFilesPath);
            getReportFilesTask.Wait();

            getReportFilesTask.ContinueWith(t =>
            {
                // act 2
                var getReportRedemtionFileWB = _excelManager.GetReportFile(SourceReportFileKey.RedemtionWB, true);

                // assert
                Assert.Multiple(() =>
                {
                    Assert.That(testReportFilesForSKU[SourceReportFileKey.RedemtionWB], Is.Not.EqualTo(null));
                    Assert.That(_testReportFilesRedemptionWB, Is.Not.EqualTo(null));
                    Assert.That(getReportRedemtionFileWB, Is.EqualTo(_testReportFilesRedemptionWB));
                });
            });
        }

        [Test, Order(5)]
        public void GetRangesSourceReportRows_OneRange_Test()
        {
            // input test assert
            Assert.Multiple(() =>
            {
                Assert.That(_testRedemptionWBPackage, Is.Not.EqualTo(null));
                Assert.That(_testReportFilesRedemptionWB, Is.Not.EqualTo(null));
                Assert.That(_testReportFilesRedemptionWB!.ReportKey, Is.EqualTo(SourceReportFileKey.RedemtionWB));
            });

            // arrange

            ExcelReportManager.GetExcelWorksheetBy(Redemtion_Report_List_Name, in _testRedemptionWBPackage, out _redemtionWBWorksheet);
            ExcelReportManager.GetRangeSourceReportRows(in _redemtionWBWorksheet!, ref _redemtionWBRangeSourceReportRows);

            // act
            var testingRange = Enumerable.Range(2, 148706 - 1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_redemtionWBWorksheet, Is.Not.EqualTo(null));
                Assert.That(_redemtionWBRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingRange));
            });
        }

        [Test, Order(6)]
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

        [Test, Order(7)]
        public void GetRangesSourceReportRows_ManyRange_Test()
        {
             // input assert 01
            Assert.That(_testRemainingWBPackage, Is.Not.EqualTo(null));
            
            // act 
            ExcelReportManager.GetExcelWorksheetBy("TDSheet", in _testRemainingWBPackage, out _remainingWBWorksheet);

            // input test assert
            Assert.That(_remainingWBWorksheet, Is.Not.EqualTo(null));

            // arrange
            var testingFirstRange = Enumerable.Range(14 + 1, 1237 - 1);     // 15, 1236
            var testingSecondRange = Enumerable.Range(1237 + 1, 2430 - 2);  // 1238, 2428

            // act 
            ExcelReportManager.GetRangeSourceReportRows(in _remainingWBWorksheet, ref _remainingWBRangeSourceReportRows);

            // assert 02 
            Assert.Multiple(() =>
            {
                Assert.That(_remainingWBRangeSourceReportRows[0].RowsIndexRange, Is.EqualTo(testingFirstRange));
                Assert.That(_remainingWBRangeSourceReportRows[1].RowsIndexRange, Is.EqualTo(testingSecondRange));
            });

        }

        [Test, Order(8)]
        public void GetRawDataFromExcelRows_SimpleRange_Test() 
        {
            // input assert
            Assert.That(_redemtionWBWorksheet, Is.Not.EqualTo(null));

            // arrange
            int redemtionWBRangeSourceReportRowsCount = 61138;

            // act
            ExcelReportManager.GetRawDataFromExcelRows(in _redemtionWBWorksheet, ref _redemtionWBRangeSourceReportRows); 

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_redemtionWBRangeSourceReportRows, Has.Count.EqualTo(1));
                Assert.That(_redemtionWBRangeSourceReportRows[0].Values, Has.Count.EqualTo(redemtionWBRangeSourceReportRowsCount));
            });
        }

        protected override void EndTest()
        {
            base.EndTest();
            _testRemainingWBPackage?.Dispose();
            _testRedemptionWBPackage?.Dispose();
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
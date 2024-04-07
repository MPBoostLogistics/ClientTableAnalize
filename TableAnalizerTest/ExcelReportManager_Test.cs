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
        private List<RangeSourceReportRows> _remainingWBRangeSourceReportRows;
        private const string SPACE_SEPARATOR = "\u0020";
        private const string FIRST_ROOM_NAME = "1 - Помещение приемки, сортировки и упаковки";
        private const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения и отгрузки";
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
                new(2, "Артикул", typeof(int)),
                new(9, "Статус доставки", typeof(string)),
                new(10, "Логистика", typeof(string), "Да" ),
                new(14, "Стоимость товара", typeof(string)) // 1137 руб.
            ];

            _redemtionWBRangeSourceReportRows = [new(_redemtionWBSourceCells)];

            _remainingWBSourceCells01 =
            [
                new(1, "Артикул", typeof(int)),
                new(6, "Название товара", typeof(string)), // TODO: Разделить на бренд и номенклатуру
                new(15, "Приход", typeof(int)),
                new(17, "Остаток", typeof(int))
            ];

            _remainingWBSourceCells02 =
            [
                new(1, "Артикул", typeof(int)),
                new(6, "Название товара", typeof(string)), // TODO: Разделить на бренд и номенклатуру
                new(17, "Остаток", typeof(int))
            ];

            _remainingWBRangeSourceReportRows = [new(_remainingWBSourceCells01, FIRST_ROOM_NAME), new(_remainingWBSourceCells02, SECOND_ROOM_NAME)];
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

        }

        [Test, Order(4)]
        public void GetReportFile_Test()
        {
            // inpet test assert
            Assert.That(testReportFilesForSKU[SourceReportFileKey.RedemtionWB], Is.Not.EqualTo(null));

            // arrange
            var _redemtionFileWB = new SourceReportFileForSKU(testReportFilesForSKU[SourceReportFileKey.RedemtionWB].FileInfo);

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
                    Assert.That(_redemtionFileWB, Is.Not.EqualTo(null));
                    Assert.That(getReportRedemtionFileWB, Is.EqualTo(_redemtionFileWB));
                });
            });
        }

        [Test, Order(5)]
        public void GetRangesSourceReportRows_OneRange_Test()
        {
            // input test assert
            Assert.Multiple(() =>
            {
                Assert.That(testReportFilesForSKU[SourceReportFileKey.RedemtionWB], Is.Not.EqualTo(null));
                Assert.That(testReportFilesForSKU[SourceReportFileKey.RedemtionWB].ReportKey, Is.EqualTo(SourceReportFileKey.RedemtionWB));
            });

            // arrange
            using ExcelPackage package = new(testReportFilesForSKU[SourceReportFileKey.RedemtionWB].FileInfo);
            ExcelReportManager.GetExcelWorksheetBy(Redemtion_Report_List_Name, in package, out ExcelWorksheet? worksheet);
            ExcelReportManager.GetRangeSourceReportRows(in worksheet!, ref _redemtionWBRangeSourceReportRows);

            // act
            var testingRange = Enumerable.Range(2, 148706 - 1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(worksheet, Is.Not.EqualTo(null));
                Assert.That(_redemtionWBRangeSourceReportRows[0].RowsRange, Is.EqualTo(testingRange));
            });

            package.Dispose();
        }

        [Test, Order(6)]
        public void GetRowIndexByStringKey_Test() 
        {
            // input test assert
            var remainingWBKey = SourceReportFileKey.RemainingWB;
            var testReportFilesRemainingWB = testReportFilesForSKU[remainingWBKey];
            Assert.Multiple(() =>
            {
                Assert.That(testReportFilesRemainingWB, Is.Not.EqualTo(null));
                Assert.That(testReportFilesRemainingWB.ReportKey, Is.EqualTo(remainingWBKey));
            });

            // arrange
            var testFirstIndex = 14;
            var testSecondIndex = 1237;

            // act 
            using ExcelPackage package = new(testReportFilesRemainingWB.FileInfo);
            ExcelReportManager.GetExcelWorksheetBy("TDSheet", in package, out ExcelWorksheet? worksheet);

            // assert 01
            Assert.That(worksheet, Is.Not.EqualTo(null));

            var firstIndexInTest = ExcelReportManager.GetRowIndexBy(FIRST_ROOM_NAME, in worksheet);
            var secondIndexInTest = ExcelReportManager.GetRowIndexBy(SECOND_ROOM_NAME, in worksheet);

            // assert 02
            Assert.Multiple(() =>
            {
                Assert.That(firstIndexInTest, Is.EqualTo(testFirstIndex));
                Assert.That(secondIndexInTest, Is.EqualTo(testSecondIndex));
            });


            package.Dispose();
        }

        [Test, Order(7)]
        public void GetRangesSourceReportRows_ManyRange_Test()
        {
            // input test assert
            var remainingWBKey = SourceReportFileKey.RemainingWB;
            var testReportFilesRemainingWB = testReportFilesForSKU[remainingWBKey];
            Assert.Multiple(() =>
            {
                Assert.That(testReportFilesRemainingWB, Is.Not.EqualTo(null));
                Assert.That(testReportFilesRemainingWB.ReportKey, Is.EqualTo(remainingWBKey));
            });

            // arrange
            using ExcelPackage package = new(testReportFilesRemainingWB.FileInfo);
            ExcelReportManager.GetExcelWorksheetBy("TDSheet", in package, out ExcelWorksheet? worksheet);
            ExcelReportManager.GetRangeSourceReportRows(in worksheet!, ref _remainingWBRangeSourceReportRows);

            package.Dispose();
        }

        protected override void EndTest()
        {
            base.EndTest();
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
using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using OfficeOpenXml;

namespace TableAnalizerTest.Managers
{
    public class ExcelReportManager_Test : BaseTests
    {
        #region Variables
        private ExcelReportManager _excelManager;
        private string _testFilesPath;
        private readonly Dictionary<SourceReportFileKey,FileInfo> testFileInfos = [];
        private const string Redemtion_Report_List_Name = "Выкупы";
        private List<SourceCell> _redemtionWBsourceCells;
        private List<RangeSourceReportRows> _redemtionWBrangeSourceReportRows;
        private SourceReportFileForSKU? _redemtionFileWB;

        private const string SPACE_SEPARATOR = "\u0020";
        #endregion

        #region Functionality
        protected override void StartTest()
        {
            base.StartTest();
            _excelManager = new();
            _testFilesPath = string.Concat(_excelManager.ProjectDirectory, "/testFiles"); 

            //_redemtionFileWBFileInfo = new FileInfo(Redemtion_File_WB_Path);

            _redemtionWBsourceCells = 
            [
                new(2, "Артикул", typeof(int)), 
                new(9, "Статус доставки", typeof(string)), 
                new(10, "Логистика", typeof(string), "Да" ),
                new(14, "Стоимость товара", typeof(string)) // 1137 руб.
            ];

            _redemtionWBrangeSourceReportRows = [new (_redemtionWBsourceCells)];
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
            testFileInfos.Add(SourceReportFileKey.RedemtionWB, _excelManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceReportFileKey.RedemtionWB).Value.FileInfo);
            testFileInfos.Add(SourceReportFileKey.RemainingWB, _excelManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceReportFileKey.RemainingWB).Value.FileInfo);

        }

        [Test, Order(4)]
        public void GetReportFile_Test()
        {
            // inpet test assert
            Assert.That(testFileInfos[SourceReportFileKey.RedemtionWB], Is.Not.EqualTo(null));

            // arrange
            _redemtionFileWB = new SourceReportFileForSKU(testFileInfos[SourceReportFileKey.RedemtionWB]);

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
                    Assert.That(testFileInfos[SourceReportFileKey.RedemtionWB], Is.Not.EqualTo(null));
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
                Assert.That(testFileInfos[SourceReportFileKey.RedemtionWB], Is.Not.EqualTo(null));
                Assert.That(_redemtionFileWB, Is.Not.EqualTo(null));
            });

            Assert.That(_redemtionFileWB.ReportKey, Is.Not.EqualTo(null));

            Assert.Multiple(() =>
            {
                Assert.That(_redemtionFileWB.ReportKey, Is.EqualTo(SourceReportFileKey.RedemtionWB));
                Assert.That(_redemtionFileWB, Is.Not.EqualTo(null));
            });

            // arrange
            // .. 

            // act
            using ExcelPackage package = new(testFileInfos[SourceReportFileKey.RedemtionWB]);
            _excelManager.GetExcelWorksheetBy(Redemtion_Report_List_Name, in package, out ExcelWorksheet? worksheet);
            
            ExcelReportManager.GetRangeSourceReportRows(in worksheet!, ref _redemtionWBrangeSourceReportRows);

            var testingRange = Enumerable.Range(2,148706-1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(worksheet, Is.Not.EqualTo(null));
                Assert.That(_redemtionWBrangeSourceReportRows[0].RowsRange, Is.EqualTo(testingRange));
            });

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
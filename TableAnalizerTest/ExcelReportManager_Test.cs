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

        private const string Redemtion_Report_List_Name = "Выкупы";

        private SourceReportFileForSKU? _redemtionFileWB;
        #endregion

        #region Functionality
        protected override void StartTest()
        {
            base.StartTest();
            _excelManager = new();
            _testFilesPath = string.Concat(_excelManager.ProjectDirectory, "/testFiles"); ;
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
            var spaceSeparator = "\u0020";
            var testPath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (Ozon).xlsx";
            var testFileInfo = new FileInfo(testPath);

            // act
            var succsess = SourceReportFileForSKU.SplitStringIntoSubstrings(Path.GetFileName(testFileInfo.FullName), [spaceSeparator], out string[]? subStrings);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(succsess, Is.EqualTo(true));
                Assert.That(subStrings, Is.Not.EqualTo(null));
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
        }

        [Test, Order(4)]
        public void GetReportFile_Test()
        {
            // arrange
            var redemtionFileWBPath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (WB).xlsx";
            var redemtionFileWBFileInfo = new FileInfo(redemtionFileWBPath);
            _redemtionFileWB = new SourceReportFileForSKU(redemtionFileWBFileInfo);

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
                    Assert.That(redemtionFileWBFileInfo, Is.Not.EqualTo(null));
                    Assert.That(_redemtionFileWB, Is.Not.EqualTo(null));
                    Assert.That(getReportRedemtionFileWB, Is.EqualTo(_redemtionFileWB));
                });
            });
        }

        [Test, Order(5)]
        public void GetRangesSourceReportRows_Test()
        {
            // assert
            Assert.That(_redemtionFileWB, Is.Not.EqualTo(null));
            Assert.That(_redemtionFileWB.ReportKey, Is.Not.EqualTo(null));
            Assert.Multiple(() =>
            {
                Assert.That(_redemtionFileWB.ReportKey, Is.EqualTo(SourceReportFileKey.RedemtionWB));
                Assert.That(_redemtionFileWB, Is.Not.EqualTo(null));
            });

            // arrange
            List<SourceCell> redemtionWBsourceCells = 
            [
                new(2, "Артикул", typeof(int)), 
                new(9, "Статус доставки", typeof(string)), 
                new(10, "Логистика", typeof(string), "Да" ),
                new(14, "Стоимость товара", typeof(string)) // 1137 руб.
            ];

            List<RangeSourceReportRows> redemtionWBrangeSourceReportRows = [new (redemtionWBsourceCells)];

            // act
            var redemtionFileWBPath = "/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (WB).xlsx";
            var redemtionFileWBFileInfo = new FileInfo(redemtionFileWBPath);
            
            using ExcelPackage package = new(redemtionFileWBFileInfo);
            _excelManager.GetExcelWorksheetBy(Redemtion_Report_List_Name, in package, out ExcelWorksheet? worksheet);
            
            ExcelReportManager.GetRangeSourceReportRows(in worksheet!, ref redemtionWBrangeSourceReportRows);

            // 2
            // 148706

            var testingRange = Enumerable.Range(2,148706-1);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(worksheet, Is.Not.EqualTo(null));
                Assert.That(redemtionWBrangeSourceReportRows[0].RowsRange, Is.EqualTo(testingRange));

            });

            //  2
            // 148707


            // Trace.WriteLine("...");

            // 2
            // 148706 

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
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.SKUItems;

namespace TableAnalizerTest.Managers
{
    public class ExcelReportManager_Test : BaseTests
    {
        #region Variables
        private ExcelReportManager _excelManager;
        private string _testFilesPath;
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
            var succsess = SourceReportFileForSKU.SplitStringIntoSubstrings(Path.GetFileName(testFileInfo.FullName), [spaceSeparator], out string[] subStrings);

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

            //var aa = _excelManager.GetReportFiles(_testFilesPath);
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

        // [Test, Order(4)]
        // public void SaveСlientLogin_Test() 
        // {
        //     // arrange
        //     // ..

        //     // act 1 
        //     // ...

        //     // assert 1 
        //     //ExcelReportManager.SaveСlientLogin();

        // }

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
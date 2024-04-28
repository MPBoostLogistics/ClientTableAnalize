
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.SKUItems;

namespace TableAnalizerTest.Managers
{
    [TestFixture, Order(1)]
    public class T01_FileManagerTest : BaseTests
    {
        #region Functionality
        [Test, Order(1)]
        public void FilesPath_Test()
        {
            // input assert 
            Assert.That(TestData.TestFilesPath, Is.Not.EqualTo(null));

            // arrange
            FileManager.InputReportsFilePath = TestData.TestFilesPath;

            // act 1
            var dirInfo = new DirectoryInfo(FileManager.InputReportsFilePath);

            // assert 1
            Assert.That(dirInfo, Is.Not.EqualTo(null));

            // act 2
            var success = FileManager.GetFileInfosFromDirInfoByExtensions(dirInfo, out var testFileInfos, ".xlsx");

            // assert 2
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(testFileInfos, Is.Not.EqualTo(null));
                Assert.That(testFileInfos!.Count(), Is.EqualTo(TEST_FILEINFOS_COUNT));
            });
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
            // input assert 
            Assert.That(TestData.TestFilesPath, Is.Not.EqualTo(null));

            // arrange
            // ...

            // act
            var getReportFilesTask = _fileManager.GetReportFiles(TestData.TestFilesPath);
            getReportFilesTask.Wait();

            var succsess = _fileManager.IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys);

            // assert
            Assert.Multiple(() =>
            {
                Assert.That(_fileManager.ReportsPaths, Has.Count.EqualTo(8));
                Assert.That(succsess, Is.EqualTo(true));
                Assert.That(_fileManager.ClientLogin, Is.EqualTo("79164372997"));
                Assert.That(_fileManager.DefineDirInfoForClientTargetReports, Is.Not.EqualTo(null));
            });

            // Set redemtionWB file path
            testReportFilesForSKU.Add(SourceSKUReportFileKey.RansomsWB, _fileManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceSKUReportFileKey.RansomsWB).Value);
            testReportFilesForSKU.Add(SourceSKUReportFileKey.RemainingWB, _fileManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceSKUReportFileKey.RemainingWB).Value);

            TestData.TestReportFilesRansomsWB = testReportFilesForSKU[SourceSKUReportFileKey.RemainingWB];
            TestData.TestReportFilesRedemptionWB = testReportFilesForSKU[SourceSKUReportFileKey.RansomsWB];
        }

        protected override void EndTest()
        {
            base.EndTest();
            _testRansomsWBPackage?.Dispose();
            _testRemainingWBPackage?.Dispose();
        }

        #endregion
    }
}


using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.SKUItems;

namespace TableAnalizerTest.TestManagers
{
    [TestFixture, Order(1)]
    public class Test01_FileManagerTest : BaseTests
    {
        #region Functionality
        [Test, Order(1)]
        public void FilesPath_Test()
        {
            // input assert 
            Assert.That(TestData.TestFilesPath, Is.Not.EqualTo(null));

            // arrange
            // ... 

            // act 1
            var dirInfo = new DirectoryInfo(TestData.TestFilesPath);

            // assert 1
            Assert.That(dirInfo, Is.Not.EqualTo(null));

            // act 2
            testFileInfos = from fileInfo in dirInfo.GetFiles()
                            where fileInfo.Name.EndsWith(".xlsx")
                            select fileInfo;

            // assert 2
            Assert.That(testFileInfos, Is.Not.EqualTo(null));
            Assert.That(testFileInfos.Count(), Is.EqualTo(TEST_FILEINFOS_COUNT));
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
                Assert.That(_fileManager.GetClientOutputPath, Is.Not.EqualTo(null));
            });

            // Set redemtionWB file path
            testReportFilesForSKU.Add(SourceSKUReportFileKey.RansomsWB, _fileManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceSKUReportFileKey.RansomsWB).Value);
            testReportFilesForSKU.Add(SourceSKUReportFileKey.RemainingWB, _fileManager.ReportsPaths
                .FirstOrDefault(rp => rp.Value.ReportKey == SourceSKUReportFileKey.RemainingWB).Value);

            TestData._testReportFilesRansomsWB = testReportFilesForSKU[SourceSKUReportFileKey.RemainingWB];
            TestData._testReportFilesRedemptionWB = testReportFilesForSKU[SourceSKUReportFileKey.RansomsWB];
        }

        // [Test, Order(4)]
        // public void GetReportFile_Test()
        // {
        //     // inpet test assert
        //     Assert.Multiple(() =>
        //     {
        //         Assert.That(_testFilesPath, Is.Not.Null);
        //         Assert.That(_testReportFilesRedemptionWB, Is.Not.EqualTo(null));
        //     });

        //     // act 1
        //     var getReportFilesTask = _fileManager.GetReportFiles(_testFilesPath);
        //     getReportFilesTask.Wait();

        //     getReportFilesTask.ContinueWith(t =>
        //     {
        //         // act 2
        //         var getReportRedemtionFileWB = _fileManager.GetReportFile(SourceSKUReportFileKey.RedemtionWB, true);

        //         // assert
        //         Assert.Multiple(() =>
        //         {
        //             Assert.That(testReportFilesForSKU[SourceSKUReportFileKey.RedemtionWB], Is.Not.EqualTo(null));
        //             Assert.That(_testReportFilesRedemptionWB, Is.Not.EqualTo(null));
        //             Assert.That(getReportRedemtionFileWB, Is.EqualTo(_testReportFilesRedemptionWB));
        //         });
        //     });
        // }

        protected override void EndTest()
        {
            base.EndTest();
            _testRansomsWBPackage?.Dispose();
            _testRemainingWBPackage?.Dispose();
        }

        #endregion
    }
}

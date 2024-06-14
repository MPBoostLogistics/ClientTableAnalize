
using System.Diagnostics;
using TableAnalizer.Enums;
using TableAnalizer.Objects.Managers;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizerTest.TestManagers
{
    [TestFixture, Order(1)]
    public class T01_FileManagerTest : BaseTests
    {
        #region Variables and constants
        private readonly Dictionary<SourceSKUReportFileKey, (string path, FileInfo? fileInfo)> testFilesCollection = new() 
        {
            {SourceSKUReportFileKey.RansomsWB, ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (WB).xlsx", null)},
            {SourceSKUReportFileKey.RansomsOZ, ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Отчет по клиенту +79164372997 (Ozon).xlsx", null)},
            {SourceSKUReportFileKey.RemainingWB, ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов приходы и остатки 220524 (WB).xlsx", null)},
            {SourceSKUReportFileKey.RemainingOZ,  ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов приходы и остатки 220524 (Ozon).xlsx", null)},
            {SourceSKUReportFileKey.Shipped, ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов расходы 22052024.xlsx", null)},
            {SourceSKUReportFileKey.WriteOffShortages, ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Общее списание 23.01.24.xlsx", null)},
            {SourceSKUReportFileKey.Additional, ("/Users/dmitrijsamarcev/Documents/Programming/CSharp/TableAnalizer/TableAnalizerTest/testFiles/Юсупов дополнительный 220224.xlsx", null)}
        }; 
        #endregion

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
                Assert.That(testFileInfos!.Count(), Is.EqualTo(testFilesCollection.Count));
            });
        }

        [Test, Order(2)]
        public void SplitStringIntoSubstrings_Test()
        {
            // arrage
            // ... 

            // act  
            foreach (var testFile in testFilesCollection)
            {
                var currentItem = testFilesCollection[testFile.Key];
                currentItem.fileInfo = new FileInfo(testFile.Value.path);
                testFilesCollection[testFile.Key] = currentItem;

                var success = SourceReportFileForSKU.SplitStringIntoSubstrings(Path.GetFileName(testFilesCollection[testFile.Key].fileInfo!.FullName), [SourceReportFileForSKU.SPACE_SEPARATOR], out string[]? subStrings);

                // assert 01
                Assert.Multiple(() =>
                {
                    
                    Assert.That(success, Is.EqualTo(true));
                    Assert.That(subStrings, Is.Not.Null);
                });

                 // assert 02
                switch (testFile.Key)
                {
                    case SourceSKUReportFileKey.RansomsWB: case SourceSKUReportFileKey.RansomsOZ:
                        Assert.That(subStrings, Has.Length.EqualTo(5));
                        break;
                    case SourceSKUReportFileKey.RemainingWB: case SourceSKUReportFileKey.RemainingOZ:
                        Assert.That(subStrings, Has.Length.EqualTo(6));
                        break;
                    case SourceSKUReportFileKey.WriteOffShortages: case SourceSKUReportFileKey.Additional: case SourceSKUReportFileKey.Shipped:
                        Assert.That(subStrings, Has.Length.EqualTo(3));
                        break;
                }
            }
        }

        [Test, Order(3)]
        public void MakeSourceReportFileForSKUs_Test()
        {
            // arrange
            _fileManager.ReportsPaths.Clear();
            IEnumerable<FileInfo>? fileInfos = FileManager.GetFilesFromDirectory(TestData.TestFilesPath, BaseManager.EXCEL_FILE_EXTENSION);

            Assert.That(fileInfos, Is.Not.Null);
            Assert.That(fileInfos.Count, Is.EqualTo(TEST_FILEINFOS_COUNT));

            foreach (var fileInfo in fileInfos)
            {
                try
                {
                    var sorceReportFile = new SourceReportFileForSKU(fileInfo);
                    var key = sorceReportFile.ReportKey;

                    // asserts 
                    Assert.Multiple(() =>
                    {
                        Assert.That(sorceReportFile, Is.Not.Null);
                        Assert.That(key.HasValue, Is.True);
                    });

                    _fileManager.ReportsPaths[key.Value] = sorceReportFile;

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                };

            }

            // assert
            Assert.That(_fileManager.ReportsPaths, Has.Count.EqualTo(TEST_FILEINFOS_COUNT));
            



            // // input 
            // Assert.That(TestData.TestFilesPath, Is.Not.EqualTo(null));

            // 
            // // ...

            // // act
            // var getReportFilesTask = _fileManager.GetReportFiles(TestData.TestFilesPath);
            // getReportFilesTask.Wait();

            // var succsess = _fileManager.IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out List<SourceSKUReportFileKey> existKeys);

            // // assert
            // Assert.Multiple(() =>
            // {
            //     Assert.That(_fileManager.ReportsPaths, Has.Count.EqualTo(7));
            //     Assert.That(succsess, Is.EqualTo(true));
            //     Assert.That(_fileManager.ClientLogin, Is.EqualTo("79164372997"));
            //     Assert.That(_fileManager.DefineDirInfoForClientTargetReports, Is.Not.EqualTo(null));
            // });

            // // Set redemtionWB file path
            // testReportFilesForSKU.Add(SourceSKUReportFileKey.RansomsWB, _fileManager.ReportsPaths
            //     .FirstOrDefault(rp => rp.Value.ReportKey == SourceSKUReportFileKey.RansomsWB).Value);
            // testReportFilesForSKU.Add(SourceSKUReportFileKey.RemainingWB, _fileManager.ReportsPaths
            //     .FirstOrDefault(rp => rp.Value.ReportKey == SourceSKUReportFileKey.RemainingWB).Value);
        }

        [Test, Order(4)]
        public void GetReportFiles_Test() 
        {
            // arrage 
            var fileCount = _fileManager.GetReportFiles(TestData.TestFilesPath);

            // assert 
            Assert.That(fileCount.Result, Is.EqualTo(TEST_FILEINFOS_COUNT));
        }


        // protected override void EndTest()
        // {
        //     base.EndTest();
        // }

        #endregion
    }
}

using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.SKUItems;
using OfficeOpenXml;

namespace TableAnalizerTest.Managers
{
    [TestFixture, Order(0)]
    public class BaseTests
    {
        #region Variables and constants

        protected static TestData TestData {get; set;}
        protected static ExcelManager _excelManager;
        protected static SKUManager _skuManager;
        protected FileManager _fileManager;
        protected const string SPACE_SEPARATOR = "\u0020";
        
        protected const int TEST_FILEINFOS_COUNT = 8;
        protected IEnumerable<FileInfo>? testFileInfos;
        protected readonly Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> testReportFilesForSKU = [];

        #endregion

        #region Functionality
        [OneTimeSetUp]
        protected virtual void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Create managers and init values 
            TestData ??= TestData.Source;
            _fileManager = new();
            TestData.TestFilesPath = string.Concat(_fileManager.ProjectDirectory, "/testFiles");

            _excelManager = new();
            _skuManager = new(_excelManager);

            TestData._testRansomsRangeSourceReportRows = [new(ExcelManager.RansomsSourceCells)];
        }

        [OneTimeTearDown]
        protected virtual void EndTest()
        {
            Trace.Flush();
        }
        #endregion

        // arrange
        // act
        // assert
    }
}
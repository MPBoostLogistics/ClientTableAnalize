using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using OfficeOpenXml;

namespace TableAnalizerTest.Managers
{
    [TestFixture, Order(0)]
    public class BaseTests
    {
        #region Variables and constants
        protected static ExcelReportManager _excelManager;
        protected static SKUManager _skuManager;
        protected const string SPACE_SEPARATOR = "\u0020";
        
        protected FileManager _fileManager;
        protected const int TEST_FILEINFOS_COUNT = 8;
        protected IEnumerable<FileInfo>? testFileInfos;
        protected readonly Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> testReportFilesForSKU = [];
        protected ExcelPackage? _testRansomsWBPackage;
        protected ExcelPackage? _testRemainingWBPackage;

        #endregion

        #region Functionality

        [OneTimeSetUp]
        protected virtual void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            // Create managers and init values 
            _fileManager = new();
            TestData.TestFilesPath = string.Concat(_fileManager.ProjectDirectory, "/testFiles");

            _excelManager = new();
            _skuManager = new(_excelManager);

            TestData.testRansomsRangeSourceReportRows = [new(ExcelReportManager.RansomsSourceCells)];
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
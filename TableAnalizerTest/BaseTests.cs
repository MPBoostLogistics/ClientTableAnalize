using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.Parsing;
using ClientTableAnalizer.Objects.SKUItems;
using OfficeOpenXml;

namespace TableAnalizerTest.TestManagers 
{
    [TestFixture, Order(0)]
    public class BaseTests
    {
        #region Variables and constants
        protected const string SPACE_SEPARATOR = "\u0020";
        protected ExcelReportManager _excelManager;
        protected FileManager _fileManager;
        protected const int TEST_FILEINFOS_COUNT = 8;
        protected IEnumerable<FileInfo>? testFileInfos;
        protected readonly Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> testReportFilesForSKU = [];
        protected ExcelPackage? _testRansomsWBPackage;
        protected ExcelPackage? _testRemainingWBPackage;
        protected List<SourceCell> _testRansomsSourceCells;
        protected List<RangeSourceReportRows> _testRansomsRangeSourceReportRows;

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

            _testRansomsSourceCells =
            [
                new(1, "SKUNumber", typeof(int), true),
                new(9, "SKURansomsStatus", typeof(string), true),
                new(10, "Логистика", typeof(string), true, "Да"),
                new(14, "SKUCost",  typeof(string), true) // 1137 руб.
            ];

            _testRansomsRangeSourceReportRows = [new(_testRansomsSourceCells)];
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
using OfficeOpenXml;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    [Order(3)]
    public class ExcelManager_Test : Base_Test
    {
        [Test, Order(1)]
        public void CreateExcelPackage_Test() 
        {
            // arrange
            List<SourceSKUReportFileKey> keys = [];

            // act
            foreach (var reportsPath in reportFileManager.ReportsPathsSingle)
            {
                keys.Add(reportsPath.Key);
                excelManager.CreateExcelPackage(reportsPath.Key, reportsPath.Value.FileInfo);
            }

            // assert 
            foreach (var key in keys)
            {
                excelManager.GetExcelPackageByKey(key, out ExcelPackage? package);
                Assert.That(package, Is.Not.Null);
            }
        }
    }
}

// arrange
// ...

// act
// ...

// assert 
// ... 
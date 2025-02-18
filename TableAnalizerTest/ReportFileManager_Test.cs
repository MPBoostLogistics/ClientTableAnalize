using System.Diagnostics;
using TableAnalizer.Objects;
using TableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    [Order(2)]
    public class ReportFileManager_Test : Base_Test
    {

        public override void Start()
        {
            base.Start();
        }

        [Test, Order(1)]
        public void GetReportSourceFilesForSingleClient_Test() 
        {
            // assert 
            Assert.That(reportFileManager.ReportsPathsSingle, Has.Count.EqualTo(reportSourceFilesForSingleClientSum));
        }

        [Order(2)]
        [TestCase(SourceSKUReportFileKey.RansomsWB, true)]
        [TestCase(SourceSKUReportFileKey.RansomsOZ, true)]
        [TestCase(SourceSKUReportFileKey.RemainingWB, true)]
        [TestCase(SourceSKUReportFileKey.RemainingOZ, true)]
        [TestCase(SourceSKUReportFileKey.Shipped, true)]
        [TestCase(SourceSKUReportFileKey.WriteOffShortages, true)]
        [TestCase(SourceSKUReportFileKey.Additional, true)]
        [TestCase(SourceSKUReportFileKey.Client, false)]
        [TestCase(SourceSKUReportFileKey.Transfered, false)]
        public void GetSourceReportForSingleClient_Test(SourceSKUReportFileKey key, bool isExist) 
        {
            //assert 
            Assert.That(reportFileManager.ReportsPathsSingle.ContainsKey(key), Is.EqualTo(isExist));
        }
    }
}

            // arrange
            // ...

            // act
            // ...

            // assert 
            // ... 
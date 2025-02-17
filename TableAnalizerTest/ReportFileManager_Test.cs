using TableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    [Order(2)]
    public class ReportFileManager_Test : Base_Test
    {
        #region Valiables and constants
        private ReportFileManager reportFileManager;   
        private string[] validFileExtensions = []; 
        #endregion

        public override void Start()
        {
            base.Start();
            reportFileManager = new();
            validFileExtensions = reportFileManager.validFileExtensions;
        }

        [Test, Order(1)]
        public async Task GetReportSourceFilesForSingleClient_Test() 
        {
            // arrange
            var reportSourceFilesForSingleClientSum = 7;

            // act
            Task<int> getReportFilesTask = 
                reportFileManager.GetReportFiles(testSingleClientSourceDirectory!.FullName, false, validFileExtensions);
            await Task.Run(() => getReportFilesTask);
            getReportFilesTask.Wait();

            // assert 
            Assert.Multiple(() =>
            {
                Assert.That(getReportFilesTask.Result, Is.EqualTo(reportSourceFilesForSingleClientSum));
            });
        }
    }
            // arrange
            // ...

            // act
            // ...

            // assert 
            // ... 
}


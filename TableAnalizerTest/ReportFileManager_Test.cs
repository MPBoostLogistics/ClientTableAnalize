using TableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    [Order(2)]
    public class ReportFileManager_Test : Base_Test
    {
        #region Valiables and constants
        private ReportFileManager reportFileManager;    
        #endregion

        public override void Start()
        {
            base.Start();
            reportFileManager = new();
        }

        [Test, Order(1)]
        public async Task GetReportFiles() 
        {
            // arrange
            // ...

            // act
            Task<(int count, string message, bool error)> getReportFilesTask = 
                reportFileManager.GetReportFiles(testSingleClientSourceDirectory!.FullName, false);
            await Task.Run(() => getReportFilesTask);
            getReportFilesTask.Wait();
            var (count, message, error) = getReportFilesTask.Result;

            // assert 
            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(7));
                Assert.That(error, Is.False);
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


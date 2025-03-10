using TableAnalizer.Objects;

namespace TableAnalizerTest
{
    [Order(1)]
    public class ReportFileManager_Test : Base_Test
    {
        public override void Start()
        {
            base.Start();
        }

        [Order(1)]
        [TestCase(SourceSKUReportFileKey.Ransoms, true)]
        [TestCase(SourceSKUReportFileKey.Balance, true)]
        [TestCase(SourceSKUReportFileKey.OutgoingProducts, true)]
        [TestCase(SourceSKUReportFileKey.WriteOffShortages, true)]
        [TestCase(SourceSKUReportFileKey.Additional, false)]
        [TestCase(SourceSKUReportFileKey.Client, false)]
        [TestCase(SourceSKUReportFileKey.Transfered, false)]
        public async Task ChecksAvailabilityNecessaryFiles_Test(SourceSKUReportFileKey key, bool isExisting) 
        {
            // arrange
            // ...

            // act
            var checksAvailabilityTask = reportFileManager.ChecksAvailabilityNecessaryFiles(testSingleClientSourceDirectory!.FullName);
            await Task.Run(() => checksAvailabilityTask);
            checksAvailabilityTask.Wait();
            List<SourceSKUReportFileKey>? existingKeys = checksAvailabilityTask.Result;

            // assert 
            Assert.That(existingKeys, Is.Not.Null);
            Assert.That(existingKeys.Contains(key), Is.EqualTo(isExisting));
        }

        [Test, Order(3)]
        public void CheckingClientsLogin_Test() 
        {
            // assert 
            
            Assert.Multiple(() =>
            {
                Assert.That(testSingleClientLogin, Is.Not.Null);
                Assert.That(testSingleClientLogin, Is.EqualTo("447849626195"));
                Assert.That(reportFileManager.ClientLogins, Is.Not.Null);
                Assert.That(reportFileManager.ClientLogins, Has.Count.EqualTo(1));
                Assert.That(reportFileManager.ClientLogins.First(), Is.EqualTo(testSingleClientLogin));
            });

        
        }
    }
}

            // arrange
            // ...

            // act
            // ...

            // assert 
            // ... 
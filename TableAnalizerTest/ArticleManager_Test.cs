using ClientTableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    public class ArticleManager_Test
    {
        private ArticleManager _articleManager;


        [OneTimeSetUp]
        public void Setup()
        {
            _articleManager = new();
        }

        [Test]
        public void SetDeadlineDays_Test()
        {
            // arrange
            var deadLineDays = 14;

            // act 
            _articleManager.SetDeadlineDays(deadLineDays);

            // assert
            Assert.That(deadLineDays, Is.EqualTo(_articleManager.DeadlineDays));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            
        }

        // arrange
        // act
        // assert
    }

}

// articleManager.SetDeadlineDays(deadLineDays);
// var saveTime = articleManager.SaveArticleRowsInReport(rowInReports, marketplace);
// if(articleManager.GetArticleByObjectValue(worksheet.Cells[row, articleManager.StorageTableIndexes.articleIndex].Value, out Article? existingArticle) && 
// articleManager.AnalyzeArticles();
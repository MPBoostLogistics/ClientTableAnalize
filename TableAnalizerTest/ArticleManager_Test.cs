using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.ArticleItems;
using ClientTableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    public class ArticleManager_Test
    {
        private ArticleManager _articleManager;
        private List<ArticleRowInReport> _testArticleRowInReports; 
        private Marketplace _marketplace;

        int[] _testCorrectArticleNumbers;
        int[] _testIncorrectArticleNumbers;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _articleManager = new();
            _testCorrectArticleNumbers = [10010001, 10010002, 10010003];
            _testIncorrectArticleNumbers = [10010004, 10010005, 10010006];

            _testArticleRowInReports =
            [
                new(_testCorrectArticleNumbers[0], "18.03.2024 12:30", "Доставлен на ПВЗ", "319 руб."),
                new(_testCorrectArticleNumbers[0], "11.03.2024 11:45", "Забран с ПВЗ", "318 руб."),
                new(_testCorrectArticleNumbers[0], "21.03.2024 00:44", "В пути", "324 руб."),
                new(_testCorrectArticleNumbers[0], "14.03.2024 17:21", "Отменен", "311 руб."),
                
                new(_testCorrectArticleNumbers[1], "17.03.2024 12:30", "Доставлен на ПВЗ", "319 руб."),
                new(_testCorrectArticleNumbers[1], "15.03.2024 09:15", "Доставлен на ПВЗ", "311 руб."),
                new(_testCorrectArticleNumbers[1], "20.03.2024 12:30", "В пути", "333 руб."),
                new(_testCorrectArticleNumbers[1], "14.03.2024 12:30", "Отменен", "311 руб."),
                
                new(_testCorrectArticleNumbers[2], "19.03.2024 03:15", "В пути", "325 руб."),
                new(_testCorrectArticleNumbers[2], "20.03.2024 04:09", "В пути", "324 руб.")
            ];
        }

        [SetUp]
        public void SetUp() 
        {
            _articleManager.SaveArticleRowsInReport(_testArticleRowInReports, _marketplace);
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

        [Test]
        public void SaveArticleRowsInReport_Test() 
        {
            // arrange
            _marketplace = Marketplace.WB; 
            var rowGroups = from row in _testArticleRowInReports
                group row by row.ArticleNumber into rowGroup
                select rowGroup;

            // act
            // ... 

            // assert
            Assert.That(_articleManager.ArticlesCount, Is.EqualTo(rowGroups.Count()));
        }

        [Test]
        public void GetArticleByNumber_Test() 
        {
            // arrange 
            // ... 

            // act && assert
            foreach (var articleNumber in _testCorrectArticleNumbers)
            {
                _articleManager.GetArticleByNumber(articleNumber, out Article? article); 
                Assert.That(article, Is.Not.EqualTo(null));
            }

            foreach (var articleNumber in _testIncorrectArticleNumbers)
            {
                _articleManager.GetArticleByNumber(articleNumber, out Article? article); 
                Assert.That(article, Is.EqualTo(null));
            }
        }

        [Test]
        public void AnalyzeArticles_Test() 
        {
            // arrange
            int first_delivered = 1;
            int first_withDrawn = 1;
            int first_onWay = 1;
            int first_canceled = 1;
            int first_sum = first_delivered + first_withDrawn + first_onWay + first_canceled;

            int second_delivered = 2;
            int second_onWay = 1;
            int second_canceled = 1;
            int second_sum = second_delivered + second_onWay + second_canceled;

            int third_onWay = 2;
            int third_sum = third_onWay;

            // act 1
            _articleManager.AnalyzeArticles();
            _articleManager.GetArticleByNumber(_testCorrectArticleNumbers[0], out Article? firstArticle); 
            _articleManager.GetArticleByNumber(_testCorrectArticleNumbers[1], out Article? secondArticle); 
            _articleManager.GetArticleByNumber(_testCorrectArticleNumbers[2], out Article? thirdrticle);

            // assert 1
            Assert.Multiple(() =>
            {
                
                Assert.That(firstArticle, Is.Not.EqualTo(null));
                Assert.That(secondArticle, Is.Not.EqualTo(null));
                Assert.That(thirdrticle, Is.Not.EqualTo(null));
            });

            // act 2
            var first_balance = firstArticle.AnalizeArticleRedemtionBalance();
            var second_balance = firstArticle.AnalizeArticleRedemtionBalance();
            var third_balance = firstArticle.AnalizeArticleRedemtionBalance();

             // assert 2
            Assert.That(first_balance!.Value.sum, Is.EqualTo(first_sum));
            Assert.That(first_balance!.Value.delivered, Is.EqualTo(first_delivered));
            Assert.That(first_balance!.Value.onWay, Is.EqualTo(first_onWay));
            Assert.That(first_balance!.Value.canceled, Is.EqualTo(first_canceled));
            Assert.That(second_balance!.Value.sum, Is.EqualTo(second_sum));
            Assert.That(second_balance!.Value.delivered, Is.EqualTo(second_delivered));
            Assert.That(second_balance!.Value.onWay, Is.EqualTo(second_onWay));
            Assert.That(second_balance!.Value.canceled, Is.EqualTo(second_canceled));
            Assert.That(third_balance!.Value.sum, Is.EqualTo(third_sum));
            Assert.That(second_balance!.Value.onWay, Is.EqualTo(second_canceled));

            // act 2
            //_articleManager.ClearCollection();
            //ß_articleManager.AnalyzeArticles();
            // проверить исключение
        }



        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            
        }

        [TearDown]
        public void TearDown() 
        {
            _testArticleRowInReports.Clear();
        
        }

        // arrange
        // act
        // assert
    }

}

// articleManager.AnalyzeArticles();
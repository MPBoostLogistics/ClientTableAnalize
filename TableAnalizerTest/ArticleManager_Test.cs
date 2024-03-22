using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.ArticleItems;
using ClientTableAnalizer.Objects.Managers;

namespace TableAnalizerTest
{
    public class ArticleManager_Test
    {
        #region Variables 
        private readonly int _firstArticleNumberIndex = 0;
        private readonly int _secondArticleNumberIndex = 1;
        private readonly int _thirdArticleNumberIndex = 2;

        private readonly string onWayStatusString = "В пути";
        private readonly string canceledStatusString = "Отменен";
        private readonly string deliveredStatusString = "Доставлен на ПВЗ";
        private readonly string withDrawnStatusString = "Забран с ПВЗ";

        private ArticleManager _articleManager;
        private List<ArticleRowInReport> _testArticleRowInReports; 
        private Marketplace _marketplace;

        int[] _testCorrectArticleNumbers;
        int[] _testIncorrectArticleNumbers;
        #endregion

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _articleManager = new();
            _testCorrectArticleNumbers = [10010001, 10010002, 10010003];
            _testIncorrectArticleNumbers = [10010004, 10010005, 10010006];

            _testArticleRowInReports =
            [
                new(_testCorrectArticleNumbers[_firstArticleNumberIndex], "18.03.2024 12:30", deliveredStatusString, "319 руб."),
                new(_testCorrectArticleNumbers[_firstArticleNumberIndex], "11.03.2024 11:45", withDrawnStatusString, "318 руб."),
                new(_testCorrectArticleNumbers[_firstArticleNumberIndex], "21.03.2024 00:44", onWayStatusString, "324 руб."),
                new(_testCorrectArticleNumbers[_firstArticleNumberIndex], "14.03.2024 17:21", canceledStatusString, "311 руб."),
                
                new(_testCorrectArticleNumbers[_secondArticleNumberIndex], "17.03.2024 12:30", deliveredStatusString, "319 руб."),
                new(_testCorrectArticleNumbers[_secondArticleNumberIndex], "15.03.2024 09:15", deliveredStatusString, "311 руб."),
                new(_testCorrectArticleNumbers[_secondArticleNumberIndex], "20.03.2024 12:30", onWayStatusString, "333 руб."),
                new(_testCorrectArticleNumbers[_secondArticleNumberIndex], "14.03.2024 12:30", canceledStatusString, "311 руб."),
                
                new(_testCorrectArticleNumbers[_thirdArticleNumberIndex], "19.03.2024 03:15", onWayStatusString, "325 руб."),
                new(_testCorrectArticleNumbers[_thirdArticleNumberIndex], "20.03.2024 04:09", onWayStatusString, "324 руб.")
            ];
        }

        [SetUp]
        public void SetUp() 
        {
           // _articleManager.SaveArticleRowsInReport(_testArticleRowInReports, _marketplace);
        }

        [Test,  Order(1)]
        public void SetDeadlineDays_Test()
        {
            // arrange
            var deadLineDays = 14;

            // act 
            _articleManager.SetDeadlineDays(deadLineDays);

            // assert
            Assert.That(deadLineDays, Is.EqualTo(_articleManager.DeadlineDays));
        }

        [Test,  Order(2)]
        public void SaveArticleRowsInReport_Test() 
        {
            // arrange
            _marketplace = Marketplace.WB; 
            var rowGroups = from row in _testArticleRowInReports
                group row by row.ArticleNumber into rowGroup
                select rowGroup;

            // act
            _articleManager.SaveArticleRowsInReport(_testArticleRowInReports, _marketplace);

            // assert
            Assert.That(_articleManager.ArticlesCount, Is.EqualTo(rowGroups.Count()));
        }

        [Test,  Order(3)]
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

        [Test,  Order(4)]
        public void AnalyzeArticles_Test() 
        {
            // arrange
            int first_delivered = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_firstArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, deliveredStatusString));
            int first_withDrawn = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_firstArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, withDrawnStatusString));
            int first_onWay = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_firstArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, onWayStatusString));
            int first_canceled = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_firstArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, canceledStatusString));
            int first_sum = first_delivered + first_withDrawn + first_onWay + first_canceled;

            int second_delivered = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_secondArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, deliveredStatusString));
            int second_onWay = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_secondArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, onWayStatusString));
            int second_canceled = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_secondArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, canceledStatusString));
            int second_sum = second_delivered + second_onWay + second_canceled;

            int third_onWay = _testArticleRowInReports
                .Count(arir => arir.ArticleNumber == _testCorrectArticleNumbers[_thirdArticleNumberIndex] && 
                    string.Equals(arir.ItemStatus, onWayStatusString));
            int third_sum = third_onWay;

            // act 1
            _articleManager.AnalyzeArticles();
            _articleManager.GetArticleByNumber(_testCorrectArticleNumbers[_firstArticleNumberIndex], out Article? firstArticle); 
            _articleManager.GetArticleByNumber(_testCorrectArticleNumbers[_secondArticleNumberIndex], out Article? secondArticle); 
            _articleManager.GetArticleByNumber(_testCorrectArticleNumbers[_thirdArticleNumberIndex], out Article? thirdrticle);

            // assert 1
            Assert.Multiple(() =>
            {
                Assert.That(firstArticle, Is.Not.EqualTo(null));
                Assert.That(secondArticle, Is.Not.EqualTo(null));
                Assert.That(thirdrticle, Is.Not.EqualTo(null));
            });

            // act 2
            ArticleBalancesReport? first_balance = firstArticle.GetArticleBalancesReport();
            ArticleBalancesReport? second_balance = secondArticle.GetArticleBalancesReport();
            ArticleBalancesReport? third_balance = thirdrticle.GetArticleBalancesReport();

            // assert 2
            Assert.Multiple(() =>
            {
                Assert.That(first_balance, Is.Not.EqualTo(null));
                Assert.That(second_balance, Is.Not.EqualTo(null));
                Assert.That(third_balance, Is.Not.EqualTo(null));



                Assert.That(first_balance!.RedemtionSum, Is.EqualTo(first_sum));
                Assert.That(first_balance!.RedemtionDelivered, Is.EqualTo(first_delivered));
                Assert.That(first_balance!.RedemtionOnWay, Is.EqualTo(first_onWay));
                Assert.That(first_balance!.RedemtionCanceled, Is.EqualTo(first_canceled));
                Assert.That(second_balance!.RedemtionSum, Is.EqualTo(second_sum));
                Assert.That(second_balance!.RedemtionDelivered, Is.EqualTo(second_delivered));
                Assert.That(second_balance!.RedemtionOnWay, Is.EqualTo(second_onWay));
                Assert.That(second_balance!.RedemtionCanceled, Is.EqualTo(second_canceled));
                Assert.That(third_balance!.RedemtionSum, Is.EqualTo(third_sum));
                Assert.That(third_balance!.RedemtionOnWay, Is.EqualTo(third_onWay));
            });

            // act 2
            _articleManager.ClearCollection();
            Assert.Throws<Exception>(_articleManager.AnalyzeArticles);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _testArticleRowInReports.Clear();
        }

        [TearDown]
        public void TearDown() 
        {
           // _testArticleRowInReports.Clear();
        
        }

        // arrange
        // act
        // assert
    }

}
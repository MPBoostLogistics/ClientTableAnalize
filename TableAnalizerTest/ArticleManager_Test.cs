using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Managers;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizerTest.Managers
{
    public class SKUManager_Test
    {
        #region Variables 
        private readonly int _deadLineDays = 14;
        private readonly int _firstSKUNumberIndex = 0;
        private readonly int _secondSKUNumberIndex = 1;
        private readonly int _thirdSKUNumberIndex = 2;

        private readonly string onWayStatusString = "В пути";
        private readonly string canceledStatusString = "Отменен";
        private readonly string deliveredStatusString = "Доставлен на ПВЗ";
        private readonly string withDrawnStatusString = "Забран с ПВЗ";

        private SKUManager _skuManager;
        private SKURowInReport[] _firstSKUNumberRowInReports;
        private SKURowInReport[] _secondSKUNumberRowInReports;
        private SKURowInReport[] _thirdSKUNumberRowInReports;
        private List<SKURowInReport> _testSKURowInReports; 
        private Dictionary<int, SKU> _testSKUs; 
        private Marketplace _marketplace;

        int[] _testCorrectSKUNumbers;
        int[] _testIncorrectSKUNumbers;
        #endregion

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _skuManager = new();

            _testCorrectSKUNumbers = [10010001, 10010002, 10010003];
            _testIncorrectSKUNumbers = [10010004, 10010005, 10010006];

            _firstSKUNumberRowInReports =  
            [
                new(_testCorrectSKUNumbers[_firstSKUNumberIndex], "18.03.2024 12:30", deliveredStatusString, "319 руб."),
                new(_testCorrectSKUNumbers[_firstSKUNumberIndex], "11.03.2024 11:45", withDrawnStatusString, "318 руб."),
                new(_testCorrectSKUNumbers[_firstSKUNumberIndex], "21.03.2024 00:44", onWayStatusString, "324 руб."),
                new(_testCorrectSKUNumbers[_firstSKUNumberIndex], "14.03.2024 17:21", canceledStatusString, "311 руб."),
            ]; 

            _secondSKUNumberRowInReports =  
            [
                new(_testCorrectSKUNumbers[_secondSKUNumberIndex], "17.03.2024 12:30", deliveredStatusString, "319 руб."),
                new(_testCorrectSKUNumbers[_secondSKUNumberIndex], "15.03.2024 09:15", deliveredStatusString, "311 руб."),
                new(_testCorrectSKUNumbers[_secondSKUNumberIndex], "20.03.2024 12:30", onWayStatusString, "333 руб."),
                new(_testCorrectSKUNumbers[_secondSKUNumberIndex], "14.03.2024 12:30", canceledStatusString, "311 руб.")
            ];

            _thirdSKUNumberRowInReports =  
            [
                new(_testCorrectSKUNumbers[_thirdSKUNumberIndex], "19.03.2024 03:15", onWayStatusString, "325 руб."),
                new(_testCorrectSKUNumbers[_thirdSKUNumberIndex], "20.03.2024 04:09", onWayStatusString, "324 руб.")
            ];

            _testSKURowInReports =
            [
                .. _firstSKUNumberRowInReports,
                .. _secondSKUNumberRowInReports,
                .. _thirdSKUNumberRowInReports,
            ];

            _testSKUs = new()
            {
                {_testCorrectSKUNumbers[0], new SKU(Marketplace.WB, _testCorrectSKUNumbers[0], [.. _firstSKUNumberRowInReports])},
                {_testCorrectSKUNumbers[1], new SKU(Marketplace.WB, _testCorrectSKUNumbers[1], [.. _secondSKUNumberRowInReports])},
                {_testCorrectSKUNumbers[2], new SKU(Marketplace.WB, _testCorrectSKUNumbers[2], [.. _thirdSKUNumberRowInReports])}
            };
        }

        [Test,  Order(1)]
        public void SetDeadlineDays_Test()
        {
            // arrange
            // ... 

            // act 
            _skuManager.SetDeadlineDays(_deadLineDays);

            // assert
            Assert.That(_deadLineDays, Is.EqualTo(_skuManager.DeadlineDays));
        }

        [Test,  Order(2)]
        public void SaveSKURowsInReport_Test() 
        {
            // arrange
            _marketplace = Marketplace.WB; 
            var rowGroups = from row in _testSKURowInReports
                group row by row.SKUNumber into rowGroup
                select rowGroup;

            // act
            _skuManager.SaveSKURowsInReport(_testSKURowInReports, _marketplace);

            // assert
            Assert.That(_skuManager.SKUsCount, Is.EqualTo(rowGroups.Count()));
        }

        [Test,  Order(3)]
        public void GetSKUByNumber_Test() 
        {
            // arrange 
            // ...  

            // act && assert
            foreach (var SKU_number in _testCorrectSKUNumbers)
            {
                _skuManager.GetSKUByNumber(SKU_number, out SKU? sku); 
                Assert.That(sku, Is.Not.EqualTo(null));
            }

            foreach (var SKU_number in _testIncorrectSKUNumbers)
            {
                _skuManager.GetSKUByNumber(SKU_number, out SKU? sku); 
                Assert.That(sku, Is.EqualTo(null));
            }
        }

        [Test,  Order(4)]
        public void GetSKUs_Test() 
        {
            // arrange
            //...

            // act
            var extensionSKUs = _skuManager.GetSKUs(); 

            foreach (var testSKU in _testSKUs)
                testSKU.Value.AnalyzeSKU(_deadLineDays);

             foreach (var extensionSKU in extensionSKUs)
                extensionSKU.Value.AnalyzeSKU(_deadLineDays);

            // assert
            Assert.That(extensionSKUs, Has.Count.EqualTo(_testSKUs.Count));
            Assert.Multiple(() =>
            {
                Assert.That(extensionSKUs.First().Key, Is.EqualTo(_testSKUs.First().Key));
                Assert.That(extensionSKUs.First().Value.Number, Is.EqualTo(_testSKUs.First().Value.Number));
                Assert.That(extensionSKUs.First().Value.SKURedempionBalance.SKURowsInRedemptionReport, Is.EqualTo(_testSKUs.First().Value.SKURedempionBalance.SKURowsInRedemptionReport));

                Assert.That(extensionSKUs.Last().Key, Is.EqualTo(_testSKUs.Last().Key));
                Assert.That(extensionSKUs.Last().Value.Number, Is.EqualTo(_testSKUs.Last().Value.Number));
                Assert.That(extensionSKUs.Last().Value.SKURedempionBalance.SKURowsInRedemptionReport, Is.EqualTo(_testSKUs.Last().Value.SKURedempionBalance.SKURowsInRedemptionReport));
            });
        }
        
        [Test,  Order(5)]
        public void AnalyzeSKUs_Test() 
        {
            // arrange
            int first_delivered = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_firstSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, deliveredStatusString));
            int first_withDrawn = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_firstSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, withDrawnStatusString));
            int first_onWay = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_firstSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, onWayStatusString));
            int first_canceled = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_firstSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, canceledStatusString));
            int first_sum = first_delivered + first_withDrawn + first_onWay + first_canceled;

            int second_delivered = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_secondSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, deliveredStatusString));
            int second_onWay = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_secondSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, onWayStatusString));
            int second_canceled = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_secondSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, canceledStatusString));
            int second_sum = second_delivered + second_onWay + second_canceled;

            int third_onWay = _testSKURowInReports
                .Count(arir => arir.SKUNumber == _testCorrectSKUNumbers[_thirdSKUNumberIndex] && 
                    string.Equals(arir.ItemStatus, onWayStatusString));
            int third_sum = third_onWay;

            // act 1
            _skuManager.AnalyzeSKUs();
            _skuManager.GetSKUByNumber(_testCorrectSKUNumbers[_firstSKUNumberIndex], out SKU? firstSKU); 
            _skuManager.GetSKUByNumber(_testCorrectSKUNumbers[_secondSKUNumberIndex], out SKU? secondSKU); 
            _skuManager.GetSKUByNumber(_testCorrectSKUNumbers[_thirdSKUNumberIndex], out SKU? thirdSKU);

            // assert 1
            Assert.Multiple(() =>
            {
                Assert.That(firstSKU, Is.Not.EqualTo(null));
                Assert.That(secondSKU, Is.Not.EqualTo(null));
                Assert.That(thirdSKU, Is.Not.EqualTo(null));
            });

            // act 2
            SKUBalancesReport? first_balance = firstSKU.GetArticleBalancesReport();
            SKUBalancesReport? second_balance = secondSKU.GetArticleBalancesReport();
            SKUBalancesReport? third_balance = thirdSKU.GetArticleBalancesReport();

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
            _skuManager.ClearCollection();
            Assert.Throws<Exception>(_skuManager.AnalyzeSKUs);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _testSKURowInReports.Clear();
        }

        // arrange
        // act
        // assert
    }

}
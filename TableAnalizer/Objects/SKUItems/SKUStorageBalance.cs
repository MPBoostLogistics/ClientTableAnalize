using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems
{
    public class SKUStorageBalance
    {
        #region Properties

        public Dictionary<ArticeStorageIndex, SKUStorageBalanceUnit> StorageBalances { get; private set;} = new() 
        {
            {ArticeStorageIndex.GoodsExpected, new ("Склад: ожидается товаров, шт.", 0)},
            {ArticeStorageIndex.GoodsReceived, new ("Склад: принято товаров, шт.", 0)},
            {ArticeStorageIndex.GoodsShipped, new ("Склад: расход товарами, шт.", 0)},
            {ArticeStorageIndex.GoodsRemaining, new ("Склад: остаток товаров, шт.", 0)}
        };

        //public int Undelivery { get; private set; } = default;                              // недовоз от ПВЗ, -
        //public int WriteoffShortages { get; private set; } = default;                       // списано по недостачам
        //public int PayShortagesCount { get; private set; } = default;                       // недостачи - количество 
        //public double PayShortagesSum { get; private set; } = default;                      // недостачи - сумма
        //public Dictionary<int, int> Regradings { get; private set; } = new();               // пересорты (на какой артикул, в каком количестве)
        //public int  DummiesCount { get; private set; } = default;                           // пустышки
        //public int TotalConsumption { get; private set; } = default;                        // общий расход = товарами + пересортом + компенсацией 
        #endregion

        #region Constructors
        public SKUStorageBalance() { }
        #endregion

        #region Functionality
        public void SetStorageBalance(ArticeStorageIndex index, int value)
        {
            var unit = StorageBalances[index];
            unit.Count += value;
            StorageBalances[index] = unit;
        }

        public int GetBalanceCountByStorageIndex(ArticeStorageIndex index) => StorageBalances[index].Count;

         public (int expected, int received, int undelivery, int shipped, int remaining)? AnalizeBalance() 
         {
            int expected = GetBalanceCountByStorageIndex(ArticeStorageIndex.GoodsExpected);
            int received = GetBalanceCountByStorageIndex(ArticeStorageIndex.GoodsReceived);
            int shipped = GetBalanceCountByStorageIndex(ArticeStorageIndex.GoodsShipped);
            int remaining = GetBalanceCountByStorageIndex(ArticeStorageIndex.GoodsRemaining);
            int undelivery = expected - received;

            return (expected, received, undelivery, shipped, remaining);
         } 

        //public bool IsItemOnWarehouseSearch() => GetSumBalanceByStorageIndex(ArticeStorageIndex.GoodsRemaining) > 0 && Undelivery == 0;
        
        //public bool IsItemArticleOnPointSearch() => GetSumBalanceByStorageIndex(ArticeStorageIndex.GoodsRemaining) <= 0 && Undelivery > 0;
 
        //public bool IsItemArticleOnPointAndWarehouseSearch() => GetSumBalanceByStorageIndex(ArticeStorageIndex.GoodsRemaining) > 0 && Undelivery > 0;

        // public void CalculatTotalConsumption()
        // {
        //     var goodsShippedCount = StorageBalances.FirstOrDefault(i => i.StorageIndex == ArticeStorageIndex.GoodsShipped).Count;
        //     // Общий расход
        //     TotalConsumption = goodsShippedCount +  // + передано товаром
        //         Regradings.Sum(re => re.Value) +    // + пересортам
        //         DummiesCount +                      // + пустышками
        //         PayShortagesCount;                  // + деньгами
        // }

        // public void CalculateTotalReceivedQuantity()
        // {
        //     // Не учитываем приходы и списание по недостачам
        //     var goodsShippedBalance = StorageBalances.FirstOrDefault(i => i.StorageIndex == ArticeStorageIndex.GoodsShipped);
        //     if(WriteoffShortages > 0)
        //         goodsShippedBalance.Count -= WriteoffShortages;
        // }

        // /// <summary>
        // /// Set undelivery by article.
        // /// </summary>
        // /// <param name="expectedQuantity ">Expected quantity of products.</param>
        // public void SetUndelivery(int expectedQuantity)
        // {
        //     var goodsShippedBalance = StorageBalances.FirstOrDefault(i => i.StorageIndex == ArticeStorageIndex.GoodsShipped);
        //     var undelivery = expectedQuantity - goodsShippedBalance.Count;
        //     Undelivery = undelivery > 0 ? undelivery : 0;
        // }
        
        // /// <summary>
        // /// Adding re-grading by article.
        // /// </summary>
        // /// <param name="article">Re-grade article.</param>
        // /// <param name="count">Number of re-grade products.</param>
        // public void SetRegrading(int article, int count)
        // {
        //     if(!Regradings.ContainsKey(article))
        //     {
        //         Regradings[article] = count;
        //     }
        //     else
        //     {
        //         Regradings[article] += count;
        //     }
        // }

        // /// <summary>
        // /// Adding  pay shortage by article.
        // /// </summary>
        // /// <param name="count">Quantity of paid goods.</param>
        // /// <param name="sum">Amount of paid goods</param>
        // public void SetPayShortage(int count, double amount)
        // {
        //     PayShortagesCount += count;
        //     PayShortagesSum += amount;
        // }

        // /// <summary>
        // /// Adding dummies by article.
        // /// </summary>
        // /// <param name="count">Quantity of goods.</param>
        // public void SetDummy(int count) => DummiesCount += count;
        #endregion
    }
}
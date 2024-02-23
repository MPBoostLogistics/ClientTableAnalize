namespace ClientTableAnalizer.Objects
{
    internal class ArticleStorageBalance
    {
        #region Properties
        public int ReceivedQuantity { get; private set; } = default;                        // приход товаров, 15
        public int ShippedQuantity { get; private set; } = default;                         // расход товаров, 16
        public int FinBalance { get; private set; } = default;                              // остаток товаров, 17
        public int Undelivery { get; private set; } = default;                              // недовоз от ПВЗ, -
        public int WriteoffShortages { get; private set; } = default;                       // списано по недостачам
        public int PayShortagesCount { get; private set; } = default;                       // недостачи - количество 
        public double PayShortagesSum { get; private set; } = default;                      // недостачи - сумма
        public Dictionary<int, int> Regradings { get; private set; } = new();               // пересорты (на какой артикул, в каком количестве)
        public int  DummiesCount { get; private set; } = default;                           // пустышки
        public int TotalConsumption { get; private set; } = default;                        // общий расход = товарами + пересортом + компенсацией 
        #endregion

        #region Constructors
        public ArticleStorageBalance() { }
        #endregion

        #region Functionality
        public void CalculatTotalConsumption()
        {
            // Общий расход
            TotalConsumption = ShippedQuantity +    // + передано товаром
                Regradings.Sum(re => re.Value) +    // + пересортам
                DummiesCount +                      // + пустышками
                PayShortagesCount;                  // + деньгами
        }

        public void CalculateTotalReceivedQuantity()
        {
            // Не учитываем приходы и списание по недостачам
            if(WriteoffShortages > 0)
                ReceivedQuantity -= WriteoffShortages;
        }

        /// <summary>
        /// Changes storage balance.
        /// </summary>
        /// <param name="index">Storage balance indicator.</param>
        /// <param name="value">Storage balance indicator value.</param>
        public void ChangesStorageBalance(ItemBalanceIndex index, int value)
        {
            switch (index)
            {
                case ItemBalanceIndex.Coming: ReceivedQuantity += value; break;
                case ItemBalanceIndex.Consumption: ShippedQuantity += value; CalculatTotalConsumption(); break;
                case ItemBalanceIndex.FinBalance: FinBalance += value; break;
                case ItemBalanceIndex.WriteoffShortages: WriteoffShortages += value; break;
                default: break;
            }
        }

        /// <summary>
        /// Set undelivery by article.
        /// </summary>
        /// <param name="expectedQuantity ">Expected quantity of products.</param>
        public void SetUndelivery(int expectedQuantity)
        {
            var undelivery = expectedQuantity - ReceivedQuantity;
            Undelivery = undelivery > 0 ? undelivery : 0;
        }
        
        /// <summary>
        /// Adding re-grading by article.
        /// </summary>
        /// <param name="article">Re-grade article.</param>
        /// <param name="count">Number of re-grade products.</param>
        public void SetRegrading(int article, int count)
        {
            if(!Regradings.ContainsKey(article))
            {
                Regradings[article] = count;
            }
            else
            {
                Regradings[article] += count;
            }
        }

        /// <summary>
        /// Adding  pay shortage by article.
        /// </summary>
        /// <param name="count">Quantity of paid goods.</param>
        /// <param name="sum">Amount of paid goods</param>
        public void SetPayShortage(int count, double amount)
        {
            PayShortagesCount += count;
            PayShortagesSum += amount;
        }

        /// <summary>
        /// Adding dummies by article.
        /// </summary>
        /// <param name="count">Quantity of goods.</param>
        public void SetDummy(int count) => DummiesCount += count;
        #endregion
    }
}
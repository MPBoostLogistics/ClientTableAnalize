namespace ClientTableAnalizer.Objects
{
    internal class ArticleStorageBalance
    {
        public int ReceivedQuantity { get; private set; } = default;                        // приход, 15
        public int ShippedQuantity { get; private set; } = default;                         // расход, 16
        public int FinBalance { get; private set; } = default;                              // остаток, 17
        public int Undelivery { get; private set; } = default;                              // недовоз, -
        public int WriteoffShortages { get; private set; } = default;                       // списано по недостачам
        public int PayShortagesCount { get; private set; } = default;                       // недостачи - количество 
        public int PayShortagesSum { get; private set; } = default;                         // недостачи - сумма
        public Dictionary<int, int> Regradings { get; private set; } = new();               // пересорты (на какой артикул, в каком количестве)
        public int TotalConsumption { get; private set; } = default;                        // общий расход = товарами + пересортом + компенсацией 

        public ArticleStorageBalance() { }

        private void CalculatTotalConsumption()
        {
            TotalConsumption = ShippedQuantity + Regradings.Sum(re => re.Value) + PayShortagesCount;
        }

        public void CalculateTotalReceivedQuantity()
        {
            if(WriteoffShortages > 0)
                ReceivedQuantity -= WriteoffShortages;
        }

        public void ChangeBalance(ItemBalanceUnit unit, int value)
        {
            switch (unit)
            {
                case ItemBalanceUnit.Coming:
                    ReceivedQuantity += value;
                    break;
                case ItemBalanceUnit.Consumption:
                    ShippedQuantity += value;
                    CalculatTotalConsumption();
                    break;
                case ItemBalanceUnit.FinBalance:
                    FinBalance += value;
                    break;
                case ItemBalanceUnit.WriteoffShortages:
                    WriteoffShortages += value;
                    break;
                default:
                    break;
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

            CalculatTotalConsumption();
        }

        public void SetPayShortage(int count, double sum)
        {
            PayShortagesCount += count;
            PayShortagesSum += count;

            CalculatTotalConsumption();
        } 

        public override string ToString()
        {
            return $"com {ReceivedQuantity}, con {ShippedQuantity}, fin {FinBalance}";
        }
    }
}
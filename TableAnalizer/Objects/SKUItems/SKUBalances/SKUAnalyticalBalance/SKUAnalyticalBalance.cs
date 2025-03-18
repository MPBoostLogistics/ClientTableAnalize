namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUAnalyticalBalance
{
    public class SKUAnalyticalBalance(int number, double price)
    {
        #region Properties 
        public int SKUNumber { get; private set; } = number;
        public double SKUPrice { get; private set; } = price;

        /// <summary>
        /// Перерасход товарами
        /// </summary>
        public int SKUOverspendingSum { get; private set; } = 0;

        /// <summary>
        /// Недорасход товарами
        /// </summary>
        public int SKUUnderspendingSum { get; private set; } = 0;

        /// <summary>
        /// Артикулы и кол-во товаров на которые учтен перерасход 
        /// </summary>
        public Dictionary<int, int> OverspendingResorts { get; private set; } = [];

        /// <summary>
        /// Артикулы и кол-во товаров с которых учтен перерасход 
        /// </summary>
        public Dictionary<int, int> UnderspendingResorts { get; private set; } = [];

        #endregion

        #region Functionality
        public void SetSourceSums(int overspendingSum, int underspendingSum)
        {
            SKUOverspendingSum = overspendingSum;
            SKUUnderspendingSum = underspendingSum;
        }
        #endregion
    }
}
using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.ArticleItems;

namespace ClientTableAnalizer.Objects.Managers
{
    public class SKUManager : BaseManager
    {
        #region Properties 
        public int DeadlineDays {get; private set;}
        public readonly (int skuIndex, int dateIndex, int statusIndex, int logisticFlagIndex, int priceIndex) RedemptionTableIndexes= (2, 7, 9, 10, 14);
        public readonly (int skuIndex, int nameIndex, int initBalanceIndex, int goodsReceivedIndex, int finBalanceIndex) StorageTableIndexes = (1, 6, 14, 15, 17);
        private readonly Dictionary<int, SKU> _skus  = new();
        public int SKUsCount => _skus.Count;

        #endregion

        #region Constructors and destructors
        public SKUManager(){}
        #endregion

        #region Functionality
        /// <summary>
        /// Clears collection of articles.
        /// </summary>
         /// <returns>Logical flag for performing an operation.</returns>
        /// <remarks><i>New version, tested</i><b> Use for testing only!</b></remarks>
        public bool ClearCollection() 
        {
             _skus.Clear();
             return SKUsCount == 0;
        }

        /// <summary>
        /// Sets period before deadline when paying a client (in days).
        /// </summary>
        /// <param name="days">Period until deadline, days</param>
        /// <remarks><i>New version, tested</i></remarks>
        public void SetDeadlineDays (int days) => DeadlineDays = days;

        // public bool GetArticlesByRedemtionStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus,ArticleRowInReport>>? groups) 
        // {
        //      groups = from article in Articles
        //               from item in article.Value.ArticleRedempionBalance.ArticleRowsInRedemptionReport
        //               group item by item.ItemStatus into itemGroup
        //               orderby itemGroup.Key
        //               select itemGroup;
                      
        //     return groups.Any();
        // }

        // public int? GetArticleStorgeBalanceCountByIndex(int articleNumber, ArticeStorageIndex index) 
        // {
        //     if(GetArticleByNumber(articleNumber, out Article? article) && article is not null) 
        //     {
        //         return article.ArticleStorageBalance.GetBalanceCountByStorageIndex(index);
        //     }
        //     return null;
        // }

        // public int GetArticlesStorgeBalanceByIndex(ArticeStorageIndex index) 
        // {
        //     var aa = from article in Articles.Values
        //              from balanceUnit in article.ArticleStorageBalance.StorageBalances
        //              select balanceUnit.Value
        //              group balanceUnit by balanceUnit.Key into balanceGroup
                    
        //              orderby balanceGroup.Key
        //              select balanceGroup;

            

        // }

        public bool GetSKUByObjectValue (object value, out SKU? sku) 
        {
            sku = null;
            int? skuNumber = ParseValueToInt(value);

            if (!skuNumber.HasValue) 
            {
                sku = null;
                return false;
            }

            return GetSKUByNumber(skuNumber.Value, out sku);
        }

        /// <summary>
        /// Returns a collection of SKUs.
        /// </summary>
        /// <returns>Collection of SKUs.</returns>
        public Dictionary<int, SKU> GetSKUs() => _skus;

        /// <summary>
        /// Returns saved article by its number or null.
        /// </summary>
        /// <param name="articleNumber">Article number to look up in  dict.</param>
        /// <param name="article">Link to a copy of article.</param>
        /// <returns>Logical flag for performing an operation.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public bool GetSKUByNumber (int articleNumber, out SKU? article) 
        {
            article = _skus.TryGetValue(articleNumber, out SKU? value) ? value : null;
            return article != null;
        }
        
        /// <summary>
        /// Saves ArticleRowInReport in corresponding article. 
        /// </summary>
        /// <param name="skuRowsInReport">Link to ArticleRowInReport.</param>
        /// <param name="marketplace">Marketplace products.</param>
        /// <returns>Time it takes to complete operation in seconds.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public double SaveSKURowsInReport(List<SKURowInReport> skuRowsInReport, Marketplace marketplace) 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var skuRowInReport in skuRowsInReport)
            {
                var skuNumber = skuRowInReport.SKUNumber;

                if(GetSKUByNumber(skuNumber, out SKU? sku) && sku != null) 
                {
                    sku.SetArticleRowInReport(skuRowInReport);
                } 
                else 
                {
                    _skus.Add(skuNumber, new SKU(marketplace, skuNumber, skuRowInReport));
                }
            }  

            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds / 1000;
        } 
        
        /// <summary>
        /// Performs analysis of elements in article collection.
        /// </summary>
        /// <exception cref="Exception">An exception occurred when trying to parse an empty collection.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public void AnalyzeSKUs() 
        {
            if(Equals(_skus.Count, 0)) 
                throw new Exception("An attempt to parse a dict with a number of elements - 0"); 
            
            foreach (var (key, value) in _skus) 
                value.AnalyzeSKU(DeadlineDays);
        }

        #endregion
    }
}
using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.ArticleItems;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ArticleManager : BaseManager
    {
        #region Properties 
        public int DeadlineDays {get; private set;}
        public readonly (int articleIndex, int dateIndex, int statusIndex, int logisticFlagIndex, int priceIndex) RedemptionTableIndexes= (2, 7, 9, 10, 14);
        public readonly (int articleIndex, int nameIndex, int initBalanceIndex, int goodsReceivedIndex, int finBalanceIndex) StorageTableIndexes = (1, 6, 14, 15, 17);
        public Dictionary<int, Article> Articles { get; } = new();

        // public Article? this[int key] 
        // {   
        //     get =>  Articles.TryGetValue(key, out Article? value) ?
        //         value : 
        //         null;  
            
        //     set 
        //     {
        //         if(value is not null) Articles[key] = value;  
        //     } 
        // } 

        public int ArticlesCount => Articles.Count;

        #endregion

        #region Constructors and destructors
        public ArticleManager()
        {
            
        }
        #endregion

        #region Functionality
        public void SetDeadlineDays (int days) => DeadlineDays = days;

        public bool GetArticlesByRedemtionStatus(out IOrderedEnumerable<IGrouping<ArticleRedemptionStatus,ArticleRowInReport>>? groups) 
        {
             groups = from article in Articles
                      from item in article.Value.ArticleRedempionBalance.ArticleRowsInRedemptionReport
                      group item by item.ItemStatus into itemGroup
                      orderby itemGroup.Key
                      select itemGroup;
                      
            return groups.Any();
        }

        public int? GetArticleStorgeBalanceCountByIndex(int articleNumber, ArticeStorageIndex index) 
        {
            if(GetArticleByNumber(articleNumber, out Article? article) && article is not null) 
            {
                return article.ArticleStorageBalance.GetBalanceCountByStorageIndex(index);
            }
            return null;
        }

        // public int GetArticlesStorgeBalanceByIndex(ArticeStorageIndex index) 
        // {
        //     var aa = from article in Articles.Values
        //              from balanceUnit in article.ArticleStorageBalance.StorageBalances
        //              select balanceUnit.Value
        //              group balanceUnit by balanceUnit.Key into balanceGroup
                    
        //              orderby balanceGroup.Key
        //              select balanceGroup;

            

        // }

        public bool GetArticleByObjectValue (object value, out Article? article) 
        {
            int? articleKey = ParseValueToInt(value);
            if (!articleKey.HasValue) 
            {
                article = null;
                return false;
            }

            if(GetArticleByNumber(articleKey.Value, out Article? art)) 
            {
                article = art;
                return true;
            } 
            else 
            { 
                article = null;
                return false;
            }
        }

        public bool GetArticleByNumber (int articleNumber, out Article? article) 
        {
            article = Articles.TryGetValue(articleNumber, out Article? value) ? value : null;
            return article != null;
        }
        
        public double SaveArticleRowsInReport(List<ArticleRowInReport> articleRowsInReport, Marketplace marketplace) 
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var articleRow in articleRowsInReport)
            {
                var articleNumber = articleRow.ArticleNumber;

                if(GetArticleByNumber(articleNumber, out Article? article) && article != null) 
                {
                    article.SetArticleRowInReport(articleRow);
                } 
                else 
                {
                    Articles.Add(articleNumber, new Article(marketplace, articleRow));
                }
            }  

            stopwatch.Stop();

            

            return stopwatch.ElapsedMilliseconds / 1000;
        } 
        public void AnalyzeArticles() 
        {
            if(Equals(Articles.Count, 0)) 
                throw new Exception("An attempt to parse a dict with a number of elements - 0"); 
            
            foreach (var (key, value) in Articles)
                value.AnalyzeArticle(key, DeadlineDays);
        }
        #endregion
    }
}
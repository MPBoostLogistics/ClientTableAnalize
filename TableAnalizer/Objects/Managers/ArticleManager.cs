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

        public Article? this[int key] 
        {   
            get =>  Articles.TryGetValue(key, out Article? value) ?
                value : 
                null;  
            
            set 
            {
                if(value is not null) Articles[key] = value;  
            } 
        } 

        public int ArticlesCount => Articles.Count;

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

        public bool GetArticleGroupsStorageIndex(out IOrderedEnumerable<IGrouping<ArticeStorageIndex, ArticleStorageBalanceUnit>>? balances) 
        {
            balances =  from article in Articles
                        from balance in article.Value.ArticleStorageBalance.StorageBalances
                        group balance by balance.StorageIndex into itemGroup
                        orderby itemGroup.Key
                        select itemGroup;

            return balances.Any();;
        }

        public bool GetArticleByObjectValue (object value, out Article? article) 
        {
            int? articleKey = ParseValueToInt(value);
            if (!articleKey.HasValue) 
            {
                article = null;
                return false;
            }

            article = this[articleKey.Value];
            return article is not null;
        }
        
        public async Task<bool> AnalyzeArticles() 
        {
            if(Equals(Articles.Count, 0)) 
                throw new Exception("An attempt to parse a dict with a number of elements - 0");

            foreach (var (key, value) in Articles) 
            {
                await Task.Run( () => value.AnalyzeArticle(key, DeadlineDays));
            }
            
            return true;
        }

        #endregion
    }
}
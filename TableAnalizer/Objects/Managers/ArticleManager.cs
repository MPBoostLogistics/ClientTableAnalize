using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.Managers
{
    public class ArticleManager
    {
        #region Properties 
        public int DeadlineDays {get; private set;}

        private Dictionary<int, Article> Articles {get; } = new();

        public Article this[int key] 
        {   
            get => Articles[key];  
            set => Articles[key] = value;  
        } 
        #endregion

        #region Functionality
        public void SetDeadlineDays (int days) => DeadlineDays = days;

        public bool GetArtriclesByRedemtionStatus(out IOrderedEnumerable<IGrouping<RedemptionStatus,ArticleRowInReport>>? groups) 
        {
             groups = from article in Articles
                      from item in article.Value.ArticleRowsInReport
                      group item by item.ItemStatus into itemGroup
                      orderby itemGroup.Key
                      select itemGroup;
            return groups.Any();
        }

        #endregion
    }
}
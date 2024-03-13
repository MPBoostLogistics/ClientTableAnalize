using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects
{
    public class Article
    {
        #region Variables && constants
        public Marketplace Marketplace { get; private set; }
        public string Name { get; private set; }
        public List<ArticleRowInReport> ArticleRowsInReport { get; }
        public ArticleRedempionBalance RedempionBalance { get; private set; }
        public ArticleStorageBalance StoreBalance { get; private set; }
        public ArticleShortage? ArticleShortage { get; private set; }
        public ArticleItemForReport? Overrun { get; private set; }
        public DateTime MaxRedemptionDate { get; private set; }
        public bool IsDeadline { get; private set; }
        public (ArticleStatus status, string statusComment, System.Drawing.Color color) StatusValue { get; private set; }
        public double AveragePrice {get; private set; } 
        public string Comment { get; private set; }

        #endregion

        #region Constructors && destructors
        public Article(Marketplace marketplace, ArticleRowInReport item)
        {
            Marketplace = marketplace;
            Name = "No name";
            ArticleRowsInReport = new List<ArticleRowInReport> { item };
            RedempionBalance = new();
            StoreBalance = new();
            MaxRedemptionDate = DateTime.Now;
            IsDeadline = false;
            StatusValue = (ArticleStatus.Unknown, "Статус не определен", System.Drawing.ColorTranslator.FromHtml("#e6fcff"));
            AveragePrice = 0.00;
            Comment = string.Empty;
        }

        #endregion

        #region Functionality

        public void SetArticleRowInReport(ArticleRowInReport articleRowInReport) =>
            ArticleRowsInReport.Add(articleRowInReport);
        public void SetName(string name) =>
            Name = name;

        public void SetComment(string message) =>
            Comment = string.IsNullOrEmpty(Comment) ? message : string.Concat(Comment, message);

        public void SetStoreBalance(ItemBalanceIndex unit, int value) =>
            StoreBalance.ChangesStorageBalance(unit, value);

        public void SetStoreBalance(int count, double price) =>
            StoreBalance.SetPayShortage(count, price);

        public void AnalyzeArticle(int articleNumber, int deadLineDays)
        {
            // Get max redemption date
            MaxRedemptionDate = ArticleRowsInReport.MaxBy(i => i.RedemptionDate).RedemptionDate;

            // Set IsDeadline
            this.IsDeadline = (this.MaxRedemptionDate - DateTime.Now).Duration().Days > deadLineDays;

            AnalizeRedempionBalanceOfItems();

            var articleShortage = new ArticleShortage (articleNumber, Name, AveragePrice);

            StoreBalance.SetUndelivery(RedempionBalance.WaitBalance);

            // Calculate total consumption
            StoreBalance.CalculatTotalConsumption();

            // Calculate total received quantity
            StoreBalance.CalculateTotalReceivedQuantity();

            // Set status article
            if (!IsDeadline)
            {
                StatusValue = (ArticleStatus.InWork, "В работе", System.Drawing.ColorTranslator.FromHtml("#ffe7cd"));
            }
            else
            {
                // Полный расчет (+ отдали больше)
                if (StoreBalance.TotalConsumption >= RedempionBalance.WaitBalance)
                {
                    StatusValue = (ArticleStatus.FullSet, "Полный расчет", System.Drawing.ColorTranslator.FromHtml("#e1ffdb"));

                    if(StoreBalance.TotalConsumption > RedempionBalance.WaitBalance)
                    {
                        var overrunCount = StoreBalance.TotalConsumption - RedempionBalance.WaitBalance;

                        SetComment($"рассчитались больше, чем должны; разница - ({overrunCount})");

                        Overrun = StoreBalance.Regradings.Count == 0 ?
                            new ArticleItemForReport(articleNumber, Name, overrunCount, AveragePrice, overrunCount * AveragePrice) :
                            null;
                    }
                }
                else
                {
                    // Если ожидание больше общего расхода
                    if(RedempionBalance.WaitBalance > StoreBalance.TotalConsumption)
                    {
                        int shortageWarehouse = 0;

                        if (StoreBalance.FinBalance > 0 && StoreBalance.Undelivery == 0)
                        {
                            StatusValue = (ArticleStatus.OnSearchWarehouse, "На поиске склада", System.Drawing.ColorTranslator.FromHtml("#ffd3d3"));
                        }
                        else if(StoreBalance.FinBalance <= 0 && StoreBalance.Undelivery > 0)
                        {
                            StatusValue = (ArticleStatus.OnSearchPoint, "На поиске ПВЗ", System.Drawing.ColorTranslator.FromHtml("#ffa0a0"));
                        }
                        else if (StoreBalance.FinBalance > 0 && StoreBalance.Undelivery > 0)
                        {
                            StatusValue = (ArticleStatus.OnSearch, "На поиске ПВЗ и склада", System.Drawing.ColorTranslator.FromHtml("#ff6d6d"));
                        }

                        // Calculate shortages
                        shortageWarehouse = StoreBalance.Undelivery > 0 ?
                            RedempionBalance.WaitBalance - StoreBalance.TotalConsumption - StoreBalance.Undelivery :
                            RedempionBalance.WaitBalance - StoreBalance.TotalConsumption;

                        if (shortageWarehouse > 0)
                            articleShortage.SetArticleShortage(ShortagesPlace.Warehouse, shortageWarehouse);

                        if(StoreBalance.Undelivery > 0)
                            articleShortage.SetArticleShortage(ShortagesPlace.PickupPoint, StoreBalance.Undelivery);
                    }
    
                    // Если общий расход меньше одидания 
                    if (StoreBalance.TotalConsumption < StoreBalance.ReceivedQuantity && StoreBalance.FinBalance <= 0)
                    {
                        StatusValue = (ArticleStatus.OnSearchWarehouse, "На поиске склада", System.Drawing.ColorTranslator.FromHtml("#ffd3d3"));
                        SetComment("Аномалия - расход меньше прихода и остаток 0");
                    }
                }
            }

            ArticleShortage = articleShortage.Shortages.Count > 0 ? articleShortage : null;
        }

        private void AnalizeRedempionBalanceOfItems()
        {
            var itemGroups = GetGroupItemsByStatus(ArticleRowsInReport);

            foreach (var group in itemGroups)
            {
                switch (group.Key)
                {
                    case RedemptionStatus.Delivered:
                        RedempionBalance.SetBalance(RedemptionStatus.Delivered, group.Count());
                        break;
                    case RedemptionStatus.WithDrawn:
                        RedempionBalance.SetBalance(RedemptionStatus.WithDrawn, group.Count());
                        break;
                    case RedemptionStatus.Canceled:
                        RedempionBalance.SetBalance(RedemptionStatus.Canceled, group.Count());
                        break;
                    case RedemptionStatus.OnWay:
                        RedempionBalance.SetBalance(RedemptionStatus.OnWay, group.Count());
                        break;
                    default:
                        break;
                }

                AveragePrice = group.Average(it => it.Price);
            }
        }

        private static IOrderedEnumerable<IGrouping<RedemptionStatus, ArticleRowInReport>> GetGroupItemsByStatus(List<ArticleRowInReport> items)
        {
           return from item in items
                  group item by item.ItemStatus
                  into itemGroup
                  orderby itemGroup.Key
                  select itemGroup;
        }
        #endregion
    }
}
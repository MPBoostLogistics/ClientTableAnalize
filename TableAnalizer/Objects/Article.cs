namespace ClientTableAnalizer.Objects
{
    internal class Article
    {
        #region Variables && constants
        private int DeadLineDays { get; }
        public Marketplace Marketplace { get; private set; }
        public string Name { get; private set; }
        public List<ArticleRowItem> Items { get; private set; }
        public ArticleRedempionBalance RedempionBalance { get; private set; }
        public ArticleStorageBalance StoreBalance { get; private set; }
        public ArticleShortage? ArticleShortage { get; private set; }
        public DateTime MaxRedemptionDate { get; private set; }
        public bool IsDeadline { get; private set; }
        public (ArticleStatus status, string statusComment, System.Drawing.Color color) StatusValue { get; private set; }
        public double AveragePrice {get; private set; } 
        public string Comment { get; private set; }
        #endregion

        #region Constructors && destructors
        public Article(Marketplace marketplace, ArticleRowItem item, int deadLineDays)
        {
            DeadLineDays = deadLineDays;
            Marketplace = marketplace;
            Name = "No name";
            Items = new List<ArticleRowItem> { item };
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
        public void SetName(string name) =>
            Name = name;

        public void SetComment(string message) =>
            Comment = string.IsNullOrEmpty(Comment) ? message : string.Concat(Comment, message);

        public void SetStoreBalance(ItemBalanceIndex unit, int value) =>
            StoreBalance.ChangesStorageBalance(unit, value);

        public void SetStoreBalance(int count, double price) =>
            StoreBalance.SetPayShortage(count, price);

        public void AnalyzeArticle(int articleNumber)
        {
            // Get max redemption date
            MaxRedemptionDate = Items.MaxBy(i => i.RedemptionDate).RedemptionDate;

            // Set IsDeadline
            this.IsDeadline = (this.MaxRedemptionDate - DateTime.Now).Duration().Days > DeadLineDays;

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
                        // TODO: Забрать перерасходы
                        SetComment($"рассчитались больше, чем должны; разница - ({StoreBalance.TotalConsumption - RedempionBalance.WaitBalance})");
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
            var itemGroups = GetGroupItemsByStatus(Items);

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

        private static IOrderedEnumerable<IGrouping<RedemptionStatus, ArticleRowItem>> GetGroupItemsByStatus(List<ArticleRowItem> items)
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
using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects
{
    public class Article
    {
        #region Variables && constants
        public Marketplace Marketplace { get; private set; }
        public string Name { get; private set; }
        public ArticleRedempionBalance ArticleRedempionBalance { get; private set; }
        public ArticleStorageBalance ArticleStorageBalance { get; private set; }
        public ArticleShortage? ArticleShortage { get; private set; }
        public ArticleItemForReport? Overrun { get; private set; }
        public DateTime MaxRedemptionDate { get; private set; }
        public bool IsDeadline { get; private set; }
        public (ArticleStatus status, string statusComment, System.Drawing.Color color) StatusValue { get; private set; }
        public double AveragePrice {get; private set; } 
        public string Comment { get; private set; }

        #endregion

        #region Constructors && destructors
        public Article(Marketplace marketplace, ArticleRowInReport articleRowInReport)
        {
            Marketplace = marketplace;
            Name = "No name";
            ArticleRedempionBalance = new(articleRowInReport);
            ArticleStorageBalance = new();
            MaxRedemptionDate = DateTime.Now;
            IsDeadline = false;
            StatusValue = (ArticleStatus.Unknown, "Статус не определен", System.Drawing.ColorTranslator.FromHtml("#e6fcff"));
            AveragePrice = 0.00;
            Comment = string.Empty;
        }

        #endregion

        #region Functionality

        public void SetArticleRowInReport(ArticleRowInReport articleRowInReport) =>
            ArticleRedempionBalance.ArticleRowsInRedemptionReport.Add(articleRowInReport);

        public void SetName(string name) =>
            Name = name;

        public void SetComment(string message) =>
            Comment = string.IsNullOrEmpty(Comment) ? message : string.Concat(Comment, message);

        public void SetStoreBalance(ArticeStorageIndex unit, int value) =>
            ArticleStorageBalance.SetStorageBalance(unit, value);

        public void SetStoreBalance(int count, double price) =>
            ArticleStorageBalance.SetPayShortage(count, price);

        public void AnalyzeArticle(int articleNumber, int deadLineDays)
        {

            // Analize artilce redempion balance and set average price, max redemption date && deadline
            (MaxRedemptionDate, AveragePrice, IsDeadline) = ArticleRedempionBalance.AnalizeBalance(deadLineDays);


            //var articleShortage = new ArticleShortage (articleNumber, Name, AveragePrice);

            // var waitBalance = ArticleRedempionBalance.GetWaitBalance();
            // ArticleStorageBalance.SetUndelivery(waitBalance);

            // Calculate total consumption
            // ArticleStorageBalance.CalculatTotalConsumption();

            // Calculate total received quantity
            // ArticleStorageBalance.CalculateTotalReceivedQuantity();

            // Set status article
            if (!IsDeadline)
            {
                StatusValue = (ArticleStatus.InWork, "В работе", System.Drawing.ColorTranslator.FromHtml("#ffe7cd"));
            }
            else
            {
                // Полный расчет (+ отдали больше)
                // if (ArticleStorageBalance.TotalConsumption >= waitBalance)
                // {
                //     StatusValue = (ArticleStatus.FullSet, "Полный расчет", System.Drawing.ColorTranslator.FromHtml("#e1ffdb"));

                //     if(ArticleStorageBalance.TotalConsumption > waitBalance)
                //     {
                //         var overrunCount = ArticleStorageBalance.TotalConsumption - waitBalance;

                //         SetComment($"рассчитались больше, чем должны; разница - ({overrunCount})");

                //         Overrun = ArticleStorageBalance.Regradings.Count == 0 ?
                //             new ArticleItemForReport(articleNumber, Name, overrunCount, AveragePrice, overrunCount * AveragePrice) :
                //             null;
                //     }
                // }
                // else
                // {
                    // Если ожидание больше общего расхода
                    // if(waitBalance > ArticleStorageBalance.TotalConsumption)
                    // {
                    //     int shortageWarehouse = 0;

                    //     if (ArticleStorageBalance.IsItemOnWarehouseSearch())
                    //     {
                    //         StatusValue = (ArticleStatus.OnSearchWarehouse, "На поиске склада", System.Drawing.ColorTranslator.FromHtml("#ffd3d3"));
                    //     }
                    //     else if(ArticleStorageBalance.IsItemArticleOnPointSearch())
                    //     {
                    //         StatusValue = (ArticleStatus.OnSearchPoint, "На поиске ПВЗ", System.Drawing.ColorTranslator.FromHtml("#ffa0a0"));
                    //     }
                    //     else if (ArticleStorageBalance.IsItemArticleOnPointAndWarehouseSearch())
                    //     {
                    //         StatusValue = (ArticleStatus.OnSearch, "На поиске ПВЗ и склада", System.Drawing.ColorTranslator.FromHtml("#ff6d6d"));
                    //     }

                    //     // Calculate shortages
                    //     shortageWarehouse = ArticleStorageBalance.Undelivery > 0 ?
                    //         waitBalance - ArticleStorageBalance.TotalConsumption - ArticleStorageBalance.Undelivery :
                    //         waitBalance - ArticleStorageBalance.TotalConsumption;

                    //     if (shortageWarehouse > 0)
                    //         articleShortage.SetArticleShortage(ShortagesPlace.Warehouse, shortageWarehouse);

                    //     if(ArticleStorageBalance.Undelivery > 0)
                    //         articleShortage.SetArticleShortage(ShortagesPlace.PickupPoint, ArticleStorageBalance.Undelivery);
                    // }
    
                    // Если общий расход меньше одидания 
                    // if (StoreBalance.TotalConsumption < StoreBalance.ReceivedQuantity && StoreBalance.FinBalance <= 0)
                    // {
                    //     StatusValue = (ArticleStatus.OnSearchWarehouse, "На поиске склада", System.Drawing.ColorTranslator.FromHtml("#ffd3d3"));
                    //     SetComment("Аномалия - расход меньше прихода и остаток 0");
                    // }
                   //}
            }

            //ArticleShortage = articleShortage.Shortages.Count > 0 ? articleShortage : null;
        }

        #endregion
    }
}
using System.Drawing;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Interfaces;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

namespace ClientTableAnalizer.Objects.SKUItems
{
    /// <remarks><i>New version.</i></remarks>
    public class SKU : IColorable
    {
        #region Properties
        public Marketplace Marketplace { get; }
        public int SKUNumber { get; }
        public string SKUBrand { get;}
        public string SKUName { get; }
        public SKURansomsBalance SKURansomsBalance { get;  }
        public double AveragePrice {get;}
        public DateOnly MaxRansomsDate { get; }
        public bool IsSKUDeadline => (MaxRansomsDate.ToDateTime(new TimeOnly()) - DateTime.Now).Duration().Days > 14;
        public SellmentStatus SellmentStatus { get; private set; }
        public Color FillColor { get; set;} = Color.Empty;

        // public SKUStorageBalance ArticleStorageBalance { get; private set; }
        // private SKUBalancesReport? _articleBalancesReport;
        // public ArticleShortage? ArticleShortage { get; private set; }
        // public SKUItemForReport? Overrun { get; private set; }
        // 
        // public bool IsDeadline { get; private set; }
        // 
        //  
        // public string Comment { get; private set; }

        //this.IsDeadline = (this.MaxRedemptionDate - DateTime.Now).Duration().Days > DeadLineDays;

        #endregion

        #region Constructors && destructors
        public SKU(Marketplace marketplace, int skuNumber, SKURansomsBalance skuRansomsBalance, double averagePrice, DateOnly maxRansomsDate) 
        {
            Marketplace = marketplace;
            SKUNumber = skuNumber;
            SKUBrand = "No name";
            SKUName = "No name";
            SKURansomsBalance = skuRansomsBalance;
            AveragePrice = averagePrice;
            MaxRansomsDate = maxRansomsDate;
            SellmentStatus = new(SKUSellmentStatus.Unknown);

            // Set fill color
            FillColor = !IsSKUDeadline ? Color.DarkOrange : Color.DarkRed;
        }

        public override string ToString() => 
            $"{SKUNumber} ({Marketplace}): '{SKUBrand} / {SKUName}', {AveragePrice}. Date: {MaxRansomsDate}\n{SKURansomsBalance}";


        // public SKU(Marketplace marketplace, int skuNumber, SKURowInReport articleRowInReport)
        // {
        //     Marketplace = marketplace;
        //     Number = skuNumber;
        //     Name = "No name";
        //     //SKURedempionBalance = new(articleRowInReport);
        //     ArticleStorageBalance = new();
        //     MaxRedemptionDate = DateTime.Now;
        //     IsDeadline = false;
        //     SellmentStatus = new(ArticleSellmentStatus.Unknown);
        //     AveragePrice = 0.00;
        //     Comment = string.Empty;
        // }

        // public SKU(Marketplace marketplace, int skuNumber, List<SKURowInReport> skuRowsInReport) 
        // {
        //     Marketplace = marketplace;
        //     Number = skuNumber;
        //     Name = "No name";
        //     //SKURedempionBalance = new(skuRowsInReport);
        //     ArticleStorageBalance = new();
        //     MaxRedemptionDate = DateTime.Now;
        //     IsDeadline = false;
        //     SellmentStatus = new(ArticleSellmentStatus.Unknown);
        //     AveragePrice = 0.00;
        //     Comment = string.Empty;
        // }

        #endregion

        #region Functionality

        public SKUOutput GetSKUOutput => new(this);




        // public void SetArticleRowInReport(SKURowInReport articleRowInReport) {} // Dummy
        //SKURedempionBalance.Set(articleRowInReport);

        // public void SetName(string name) => Name = name;

        // public void SetComment(string message) =>
        //   Comment = string.IsNullOrEmpty(Comment) ? message : string.Concat(Comment, " ", message);

        // public void SetStoreBalance(int count, double price) =>
        //     ArticleStorageBalance.SetPayShortage(count, price);

        // public void AnalyzeSKU(int deadLineDays)
        // {
        //     // Analize artilce rows  and set average price, max redemption date && deadline
        //    // (MaxRedemptionDate, AveragePrice, IsDeadline) = SKURedempionBalance.AnalizeArticleRows(deadLineDays);

        //     // Set expected balance of goods
        //     //ArticleStorageBalance.SetStorageBalance(ArticeStorageIndex.GoodsExpected, ArticleRedempionBalance.GetExpectedBalance());

        //     // Analize balances
        //     // TODO: Очередь задач
        //     _articleBalancesReport = new();
        //     AnalizeArticleRedemtionBalance(ref _articleBalancesReport);

        //     //ArticleStorageBalance.AnalizeBalance();

        //     //var articleShortage = new ArticleShortage (articleNumber, Name, AveragePrice);


        //     // Calculate total consumption
        //     // ArticleStorageBalance.CalculatTotalConsumption();

        //     // Calculate total received quantity
        //     // ArticleStorageBalance.CalculateTotalReceivedQuantity();

        //     // Set status article
        //     if (!IsDeadline)
        //     {
        //         SellmentStatus = new (ArticleSellmentStatus.InWork);
        //     }
        //     else
        //     {
        //         // Полный расчет (+ отдали больше)
        //         // if (ArticleStorageBalance.TotalConsumption >= waitBalance)
        //         // {
        //         //     StatusValue = (ArticleStatus.FullSet, "Полный расчет", System.Drawing.ColorTranslator.FromHtml("#e1ffdb"));

        //         //     if(ArticleStorageBalance.TotalConsumption > waitBalance)
        //         //     {
        //         //         var overrunCount = ArticleStorageBalance.TotalConsumption - waitBalance;

        //         //         SetComment($"рассчитались больше, чем должны; разница - ({overrunCount})");

        //         //         Overrun = ArticleStorageBalance.Regradings.Count == 0 ?
        //         //             new ArticleItemForReport(articleNumber, Name, overrunCount, AveragePrice, overrunCount * AveragePrice) :
        //         //             null;
        //         //     }
        //         // }
        //         // else
        //         // {
        //             // Если ожидание больше общего расхода
        //             // if(waitBalance > ArticleStorageBalance.TotalConsumption)
        //             // {
        //             //     int shortageWarehouse = 0;

        //             //     if (ArticleStorageBalance.IsItemOnWarehouseSearch())
        //             //     {
        //             //         StatusValue = (ArticleStatus.OnSearchWarehouse, "На поиске склада", System.Drawing.ColorTranslator.FromHtml("#ffd3d3"));
        //             //     }
        //             //     else if(ArticleStorageBalance.IsItemArticleOnPointSearch())
        //             //     {
        //             //         StatusValue = (ArticleStatus.OnSearchPoint, "На поиске ПВЗ", System.Drawing.ColorTranslator.FromHtml("#ffa0a0"));
        //             //     }
        //             //     else if (ArticleStorageBalance.IsItemArticleOnPointAndWarehouseSearch())
        //             //     {
        //             //         StatusValue = (ArticleStatus.OnSearch, "На поиске ПВЗ и склада", System.Drawing.ColorTranslator.FromHtml("#ff6d6d"));
        //             //     }

        //             //     // Calculate shortages
        //             //     shortageWarehouse = ArticleStorageBalance.Undelivery > 0 ?
        //             //         waitBalance - ArticleStorageBalance.TotalConsumption - ArticleStorageBalance.Undelivery :
        //             //         waitBalance - ArticleStorageBalance.TotalConsumption;

        //             //     if (shortageWarehouse > 0)
        //             //         articleShortage.SetArticleShortage(ShortagesPlace.Warehouse, shortageWarehouse);

        //             //     if(ArticleStorageBalance.Undelivery > 0)
        //             //         articleShortage.SetArticleShortage(ShortagesPlace.PickupPoint, ArticleStorageBalance.Undelivery);
        //             // }

        //             // Если общий расход меньше одидания 
        //             // if (StoreBalance.TotalConsumption < StoreBalance.ReceivedQuantity && StoreBalance.FinBalance <= 0)
        //             // {
        //             //     StatusValue = (ArticleStatus.OnSearchWarehouse, "На поиске склада", System.Drawing.ColorTranslator.FromHtml("#ffd3d3"));
        //             //     SetComment("Аномалия - расход меньше прихода и остаток 0");
        //             // }
        //            //}
        //     }

        //     //ArticleShortage = articleShortage.Shortages.Count > 0 ? articleShortage : null;
        // }

        // public void AnalizeArticleRedemtionBalance(ref SKUBalancesReport abr) 
        // {
        //     // SKURedempionBalance.AnalizeBalance(ref abr);
        // }

        //public SKUBalancesReport? GetArticleBalancesReport() => _articleBalancesReport;


        // public override string ToString()
        // {
        //     return $"{Marketplace}";
        // }

        #endregion
    }
}
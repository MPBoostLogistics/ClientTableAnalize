using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

namespace ClientTableAnalizer.Objects.SKUItems
{
    /// <remarks><i>New version.</i></remarks>
    public class SKU
    {
        #region  Variables and constants 
        public static readonly DateOnly DATEONLY_DEFAULT = new(0001,01,01);
        public static readonly double AVERAGE_PRICE_DEFAULT = 0.00;
        public static readonly int DEADLINE_DAYS_DEFAULT = 30;
        #endregion Variables and constants

        #region Properties
        public Marketplace? Marketplace { get; private set; }
        public int SKUNumber { get; }
        public string SKUBrand { get; private set; } = string.Empty;
        public string SKUName { get; private set; } = string.Empty;
        public SKURansomsBalance? SKURansomsBalance { get; private set;}
        public SKURemainingBalance? SKURemainingBalance { get; private set;}
        public SKUWriteOffBalance? SKUWriteOffBalance { get; private set;}
        public SKUCompensationBalance? SKUCompensationBalance { get; private set; }
        public SKUResortBalance? SKUResortBalance { get; private set; }
        public double AveragePrice { get; private set;} = AVERAGE_PRICE_DEFAULT;
        public DateOnly MaxRansomsDate { get; private set;} = DATEONLY_DEFAULT;
        public int DeadLineDays { get;  set; } = DEADLINE_DAYS_DEFAULT;

        #endregion

        #region Constructors && destructors

        // Constructor for ransoms
        public SKU(Marketplace? marketplace, int skuNumber, SKURansomsBalance skuRansomsBalance, double averagePrice, DateOnly maxRansomsDate, int deadlineDays)
        {
            SKUNumber = skuNumber;
            SKUBrand = string.Empty; SKUName = string.Empty;

            Marketplace = marketplace;
            SKURansomsBalance = skuRansomsBalance;
            AveragePrice = averagePrice;
            MaxRansomsDate = maxRansomsDate;

            DeadLineDays = deadlineDays;
        }

        // Constructor for remainingBalance 
        public SKU(Marketplace? marketplace, int skuNumber, SKURemainingBalance skuRemainingBalance)
        {
            Marketplace = marketplace;
            SKUNumber = skuNumber;

            SKURemainingBalance = skuRemainingBalance;

            var skuBrandAndName = skuRemainingBalance.SKUBrandAndName.Split('/');

            switch(skuBrandAndName.Length) 
            {
                case 1:
                    SKUName = skuBrandAndName[0].Trim();
                    break;
                case 2: case 3: case 4:
                    SKUBrand = skuBrandAndName[0].Trim();
                    SKUName = skuBrandAndName[1].Trim(); 
                    break;
                default:
                    break;
            }
        }

        // Constructor for generating a new entity from repository
        public SKU(Marketplace? marketplace, int skuNumber, string skuBrand, string skuName, 
            SKURansomsBalance? skuRansomsBalance, SKURemainingBalance? skuRemainingBalance, 
            SKUWriteOffBalance? skuWriteOffBalance, SKUCompensationBalance? skuCompensationBalance, 
            SKUResortBalance skuResortBalance, double? averagePrice, 
            DateOnly? maxRansomsDate, int? deadLineDays) 
       {
            Marketplace = marketplace;
            SKUNumber = skuNumber;
            SKUBrand = skuBrand ?? string.Empty;
            SKUName = skuName ?? string.Empty;
            SKURansomsBalance = skuRansomsBalance;
            SKURemainingBalance = skuRemainingBalance;
            SKUWriteOffBalance = skuWriteOffBalance;
            SKUCompensationBalance = skuCompensationBalance;
            SKUResortBalance = skuResortBalance;

            if(averagePrice.HasValue && !Equals(averagePrice, AVERAGE_PRICE_DEFAULT)) 
            {
                AveragePrice = averagePrice.Value;
            }

            if(maxRansomsDate.HasValue && !Equals(maxRansomsDate, DATEONLY_DEFAULT)) 
            {
                MaxRansomsDate = maxRansomsDate.Value;
            }

            if( deadLineDays.HasValue && !Equals(deadLineDays, DEADLINE_DAYS_DEFAULT)) 
            {
                DeadLineDays = deadLineDays.Value;
            }
        }

        // Constructor for compensationBalance 
        public SKU(int skuNumber, SKUCompensationBalance skuCompensationBalance) 
        {
            SKUNumber = skuNumber;
            SKUCompensationBalance = skuCompensationBalance;
        }

        // Constructor for writeOffBalance 
        public SKU(int skuNumber, Marketplace marketplace, SKUWriteOffBalance writeOffBalance) 
        {
            SKUNumber = skuNumber;
            Marketplace = marketplace;
            SKUWriteOffBalance = writeOffBalance;
        }

        // Constructor for resortBalance 
        public SKU(int skuNumber, SKUResortBalance skuResortBalance) 
        {
            SKUNumber = skuNumber;
            SKUResortBalance = skuResortBalance;
        }

        public override string ToString() =>
            $"{SKUNumber} ({Marketplace}): '{SKUBrand} / {SKUName}', {AveragePrice}. Date: {MaxRansomsDate}\n{SKURansomsBalance}";

        #endregion

        #region Functionality

        public SKUForTargetReports GetSKUForTargetReports => new(this);

        public void Update(SKUCompensationBalance skuShortageBalance) 
        {
            if(SKUCompensationBalance is null) 
            {
                SKUCompensationBalance = skuShortageBalance;
            } 
            else 
            {
                SKUCompensationBalance.Update(skuShortageBalance.Count, skuShortageBalance.Sum, skuShortageBalance.Comments);
            }
        }

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
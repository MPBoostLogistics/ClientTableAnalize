using System.Drawing;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Interfaces;

namespace ClientTableAnalizer.Objects.SKUItems
{
    public class SKUForTargetReports : IColorable
    {
        public Marketplace? SKUmp { get; set; }
        public int SKUNumber { get; set; }
        public string SKUBrand { get; set; }
        public string SKUName { get; set; }
        public double SKUPrice { get; set; }

        public DateOnly SKUDate { get; set; }
        private readonly bool isDeadline = false;
        public SellmentStatus SellmentStatus { get; private set; }
        public string SellmentStatusString { get; private set; }
        
        public int SKURansomsTotal { get; set; }
        public int SKURansomsCanceled { get; set; }
        public int SKURansomsOnWay { get; set; }
        public int SKURansomsDelivered { get; set; }
        public int SKURansomsWithDrawn { get; set; }

        public int SKURansomsWaiting { get; set; }          // Склад: ожидание
        public int SKUReceipt { get; set; }                 // Склад: приход
        public int SKUTotalСonsumption { get; set;}         // Склад: общий расход (товаром, компенсацией, пересортом, пустшками)    
        public int SKUShipped { get; set; }                 // Склад: Расход товаром 
        public int SKUPaid { get; set; }                    // Склад: Расход компенсацией 
        public int SKUResort { get; set; }                  // Склад: Расход пересортом 
        public int SKUDummies { get; set; }                 // Склад: Расход пустышками 
        public int SKURemaining { get; set; }               // Склад: остаток товаров

        public string SKUComment { get; set;} = string.Empty;
        public Color FillColor { get; set; }


        #region Constructors
        public SKUForTargetReports(SKU sku, int deadLineDays)
        {
            SKUmp = sku.Marketplace;
            SKUNumber = sku.SKUNumber;
            SKUBrand = sku.SKUBrand;
            SKUName = sku.SKUName;
            SKUPrice = sku.AveragePrice;
            
            SKUDate = sku.MaxRansomsDate;
            isDeadline = (SKUDate.ToDateTime(new TimeOnly()) - DateTime.Now).Duration().Days > deadLineDays;

            // TODO: Переделать 
            SellmentStatus = !isDeadline ? new SellmentStatus(SKUSellmentStatus.InWork) : new SellmentStatus (SKUSellmentStatus.Unknown);
            SellmentStatusString = !isDeadline ? new SellmentStatus(SKUSellmentStatus.InWork).StatusLabel : new SellmentStatus (SKUSellmentStatus.Unknown).StatusLabel;
            FillColor = SellmentStatus.StatusColor;


            // Fill SKURansomsBalance
            if(sku.SKURansomsBalance is not null) 
            {
                SKURansomsTotal = sku.SKURansomsBalance.SKURansomsTotal;
                SKURansomsOnWay = sku.SKURansomsBalance.SKURansomsOnWay;
                SKURansomsCanceled = sku.SKURansomsBalance.SKURansomsCanceled;
                SKURansomsDelivered = sku.SKURansomsBalance.SKURansomsDelivered;
                SKURansomsWithDrawn = sku.SKURansomsBalance.SKURansomsWithDrawn;
                SKURansomsWaiting = sku.SKURansomsBalance.SKURansomsWaiting;
            }

            // Fill SKURemainingBalance
            if(sku.SKURemainingBalance is not null) 
            {
                SKUReceipt = sku.SKURemainingBalance.SKUReceipt;
                SKUShipped = sku.SKURemainingBalance.SKUShipped;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            }
            
            // Fill SKUCompensationBalance 
            if(sku.SKUCompensationBalance is not null) 
            {
                SKUPaid = sku.SKUCompensationBalance.Count;
            }

            // Fill SKUResortBalance 
            if(sku.SKUResortBalance is not null)  
            {
                sku.SKUResortBalance.AddСommentToBalance();

                SKUResort = sku.SKUResortBalance.ResortSKUs.Aggregate(0, (acc, i) => acc + i.Value.resortSKUCount);
                string comment = sku.SKUResortBalance.ResortSKUs.Aggregate(string.Empty, (acc, i) => acc + i.Value.resortSKUComment + " ");
                SKUComment = !string.IsNullOrEmpty(SKUComment) ? string.Concat(SKUComment, comment) : comment;
            }

            // TODO: Дорабоать вопрос по пустышкам
            SKUDummies = 0;

            // Calculate total Сonsumption
            SKUTotalСonsumption = SKUShipped + SKUPaid + SKUResort + SKUDummies;
        }

        #endregion Constructors
    }
}
using System.Drawing;
using ClientTableAnalizer.Interfaces;

namespace ClientTableAnalizer.Objects.SKUItems
{
    /// <summary>
    /// An entity for displaying info about an SKU in a table.
    /// </summary>
    /// <remarks><i>New version.</i></remarks>
    public class SKUOutput : IColorable
    {
        #region Properties
        public string SKUMp { get;}
        public int SKUNumber { get;}
        public string SKUBrand { get;}
        public string SKUName { get;}
        public int SKURansomsTotal { get; }
        public int SKURansomsWaiting { get; }
        public int SKURansomsOnWay { get; }
        public int SKURansomsCanceled { get; }
        public int SKURansomsDelivered { get; }            
        public int SKURansomsWithDrawn { get; }   
        public double SKUAveragePrice {get;}
        public DateOnly SKUMaxRansomsDate { get; }    
        public string SKUSellmentStatus { get; }
        public Color FillColor { get; set; }
        public int SKUReceipt { get; }
        public int SKUShipped { get; }
        public int SKURemaining { get; }
        #endregion Properties

        #region  Constructors
        public SKUOutput(SKU sku)
        {
            SKUMp = sku.Marketplace.ToString();
            SKUNumber = sku.SKUNumber;
            SKUBrand = sku.SKUBrand;
            SKUName = sku.SKUName;
            SKURansomsTotal = sku.SKURansomsBalance is not null ?  sku.SKURansomsBalance.SKURansomsTotal : 0;
            SKURansomsWaiting = sku.SKURansomsBalance is not null ? sku.SKURansomsBalance.SKURansomsWaiting : 0;
            SKURansomsOnWay = sku.SKURansomsBalance is not null ? sku.SKURansomsBalance.SKURansomsOnWay : 0;
            SKURansomsCanceled = sku.SKURansomsBalance is not null ? sku.SKURansomsBalance.SKURansomsCanceled : 0;
            SKURansomsDelivered = sku.SKURansomsBalance is not null ? sku.SKURansomsBalance.SKURansomsDelivered : 0;
            SKURansomsWithDrawn = sku.SKURansomsBalance is not null ? sku.SKURansomsBalance.SKURansomsWithDrawn : 0;
            SKUAveragePrice = sku.AveragePrice;
            SKUMaxRansomsDate = sku.MaxRansomsDate;
            SKUSellmentStatus = sku.SellmentStatus.ToString()!;
            FillColor = sku.SellmentStatus.StatusColor;
            SKUReceipt = sku.SKURemainingBalance is not null ? sku.SKURemainingBalance.SKUReceipt : 0;
            SKUShipped = sku.SKURemainingBalance is not null ? sku.SKURemainingBalance.SKUShipped : 0;
            SKURemaining = sku.SKURemainingBalance is not null ? sku.SKURemainingBalance.SKURemaining : 0;
        }
        #endregion Constructors
    }
}
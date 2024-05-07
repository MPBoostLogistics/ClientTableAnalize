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
        public Color FillColor { get; set;}
        #endregion Properties

        #region  Constructors
        public SKUOutput(SKU sku)
        {
            SKUMp = sku.Marketplace?.ToString() ?? String.Empty;
            SKUNumber = sku.SKUNumber;
            SKUBrand = sku.SKUBrand;
            SKUName = sku.SKUName;
            SKURansomsTotal = sku.SKURansomsBalance.SKURansomsTotal;
            SKURansomsWaiting = sku.SKURansomsBalance.SKURansomsWaiting;
            SKURansomsOnWay = sku.SKURansomsBalance.SKURansomsOnWay;
            SKURansomsCanceled = sku.SKURansomsBalance.SKURansomsCanceled;
            SKURansomsDelivered = sku.SKURansomsBalance.SKURansomsDelivered;
            SKURansomsWithDrawn = sku.SKURansomsBalance.SKURansomsWithDrawn;
            SKUAveragePrice = sku.AveragePrice;
            SKUMaxRansomsDate = sku.MaxRansomsDate;
            SKUSellmentStatus = sku.SellmentStatus.ToString()!;
            FillColor = sku.SellmentStatus.StatusColor;
        }
        #endregion Constructors
    }
}
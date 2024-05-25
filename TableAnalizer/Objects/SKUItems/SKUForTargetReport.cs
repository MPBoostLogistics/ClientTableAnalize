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
        public int SKUWriteOff { get; set; }                // Склад: списание недостач
        public int SKUTotalСonsumption { get; set;}         // Склад: общий расход (товаром, компенсацией, пересортом, пустшками)    
        public int SKUShipped { get; set; }                 // Склад: Расход товаром 
        public int SKUPaid { get; set; }                    // Склад: Расход компенсацией 
        public int SKUResort { get; set; }                  // Склад: Расход пересортом 
        public int SKUDummies { get; set; }                 // Склад: Расход пустышками 
        public int SKURemaining { get; set; }               // Склад: остаток товаров

        // Delivery delay
        public int SKUDeliveryDelayTotal { get; set; }
        public int SKUDeliveryDelayPoints { get; set; }
        public double SKUDeliveryDelayPointsSum { get;set;}
        public int SKUDeliveryDelayStorage { get; set; }
        public double SKUDeliveryDelayStorageSum { get; set; }

        public string SKUComment { get; set;} = string.Empty;
        public Color FillColor { get; set; }

        #region Constructors
        public SKUForTargetReports(SKU sku)
        {
            SKUmp = sku.Marketplace;
            SKUNumber = sku.SKUNumber;
            SKUBrand = sku.SKUBrand;
            SKUName = sku.SKUName;
            SKUPrice = sku.AveragePrice;

            SKUDate = sku.MaxRansomsDate;
            isDeadline = (SKUDate.ToDateTime(new TimeOnly()) - DateTime.Now).Duration().Days > sku.DeadLineDays;

            // Fill SKURansomsBalance
            if (sku.SKURansomsBalance is not null)
            {
                SKURansomsTotal = sku.SKURansomsBalance.SKURansomsTotal;
                SKURansomsOnWay = sku.SKURansomsBalance.SKURansomsOnWay;
                SKURansomsCanceled = sku.SKURansomsBalance.SKURansomsCanceled;
                SKURansomsDelivered = sku.SKURansomsBalance.SKURansomsDelivered;
                SKURansomsWithDrawn = sku.SKURansomsBalance.SKURansomsWithDrawn;
                SKURansomsWaiting = sku.SKURansomsBalance.SKURansomsWaiting;
            }

            // Fill SKURemainingBalance
            if (sku.SKURemainingBalance is not null)
            {
                var receipt = sku.SKURemainingBalance.SKUReceipt;
                var writeOff = sku.SKUWriteOffBalance?.SKUWriteOff;
                
                SKUReceipt = writeOff.HasValue ? receipt - writeOff.Value : receipt;
                SKUShipped = sku.SKURemainingBalance.SKUShipped;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            }

            // Fill SKUCompensationBalance 
            if (sku.SKUCompensationBalance is not null)
            {
                SKUPaid = sku.SKUCompensationBalance.Count;
            }

            // Fill SKUResortBalance 
            if (sku.SKUResortBalance is not null)
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

            CalculateDeliveryDelay();
            DeterminingSellmentStatus();
        }

        private void CalculateDeliveryDelay()
        {
            // Недовоз от ПВЗ = Ожидание - Приход на склад
            SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceipt;
            if (SKUDeliveryDelayPoints < 0) SKUDeliveryDelayPoints = 0;

            // Недовоз от склада = Ожидание - Общий расход 
            SKUDeliveryDelayStorage = SKURansomsWaiting - SKUTotalСonsumption - SKUDeliveryDelayPoints;
            if (SKUDeliveryDelayStorage < 0) SKUDeliveryDelayStorage = 0;

            SKUDeliveryDelayTotal =  isDeadline ? SKUDeliveryDelayPoints + SKUDeliveryDelayStorage : 0;

            SKUDeliveryDelayPointsSum = isDeadline ? SKUDeliveryDelayPoints * SKUPrice : 0.00;
            SKUDeliveryDelayStorageSum = isDeadline ? SKUDeliveryDelayStorage  * SKUPrice : 0.00;
        }

        private void DeterminingSellmentStatus() 
        {
            // Если дедлайн не прошел - в работе
            if(!isDeadline) 
            {
                SellmentStatus = new SellmentStatus(SKUSellmentStatus.InWork);
            } 
            else 
            {
                if(SKUTotalСonsumption >= SKURansomsWaiting) 
                {
                    SellmentStatus = new SellmentStatus(SKUSellmentStatus.FullSet);
                } 
                else 
                {
                    switch(SKUDeliveryDelayPoints > 0, SKUDeliveryDelayStorage > 0) 
                    {
                        case (true, true):
                            SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchWarehouseAndPoints);
                            break;
                        case (true, false):
                            SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchPoint);
                            break;
                        case (false, true):
                            SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchWarehouse);
                            break;
                    }
                }
            }

            SellmentStatusString = SellmentStatus.StatusLabel;
            FillColor = SellmentStatus.StatusColor;
        }

        #endregion Constructors
    }
}
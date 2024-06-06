using System.Collections;
using System.Collections.Concurrent;
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

       
        public int SKUAccountingByResortsCount { get; set; } = 0;
        public string SKUAccountingByResortsComment { get; set; } = string.Empty;

         // Overruns 
        public int SKUOverrunsCount { get; set; } = 0;
        public double SKUOverrunsSum { get; set; } = 0.00;

        public string SKUComment { get; set;} = string.Empty;
        public Color FillColor { get; set; }

        #region Constructors
        public SKUForTargetReports(SKU sku, ConcurrentDictionary<int, int>? resortSKUs = null)
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
            CalculateOverruns(resortSKUs);
        }

        private void CalculateDeliveryDelay()
        {
            var isConsumptionLessWaiting = SKUTotalСonsumption < SKURansomsWaiting;
      
            // Недовоз от ПВЗ = Ожидание - Приход на склад
            SKUDeliveryDelayPoints = isConsumptionLessWaiting ? SKURansomsWaiting - SKUReceipt : 0;
            if (SKUDeliveryDelayPoints < 0) SKUDeliveryDelayPoints = 0;

            // Недовоз от склада = Ожидание - Общий расход - Недовоз от ПВЗ
            SKUDeliveryDelayStorage = isConsumptionLessWaiting ? SKURansomsWaiting - SKUTotalСonsumption - SKUDeliveryDelayPoints : 0;
            if (SKUDeliveryDelayStorage < 0) SKUDeliveryDelayStorage = 0;

            // Общий недовоз
            SKUDeliveryDelayTotal =  isDeadline && isConsumptionLessWaiting ? SKUDeliveryDelayPoints + SKUDeliveryDelayStorage : 0;

            // Суммы недовоза 
            SKUDeliveryDelayPointsSum = isDeadline && isConsumptionLessWaiting ? SKUDeliveryDelayPoints * SKUPrice : 0.00;
            SKUDeliveryDelayStorageSum = isDeadline && isConsumptionLessWaiting ? SKUDeliveryDelayStorage  * SKUPrice : 0.00;
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

        private void CalculateOverruns(ConcurrentDictionary<int, int>? resortSKUs) 
        {
            SKUOverrunsCount = SKUTotalСonsumption > SKURansomsWaiting && isDeadline ? SKUTotalСonsumption - SKURansomsWaiting : 0; 

            // There can be no negative overruns

            switch(SKUOverrunsCount) 
            {
                case <= 0:
                    SKUOverrunsCount = 0;
                    SKUOverrunsSum = 0.00;
                    break;
                case > 0:
                    if(resortSKUs == null) 
                    {
                        if(isDeadline) SKUOverrunsSum = SKUOverrunsCount * SKUPrice;
                    } 
                    else 
                    {
                        foreach (KeyValuePair<int, int> item in resortSKUs)
                        {
                            var skuFromResort = item.Key;
                            SKUAccountingByResortsCount += item.Value;

                            SKUAccountingByResortsComment = string.IsNullOrEmpty(SKUAccountingByResortsComment) ?  
                                $"{skuFromResort} - {item.Value}, " :
                                string.Concat(SKUAccountingByResortsComment, $"{skuFromResort} - {item.Value}, ");
                        }

                        SKUOverrunsCount -= SKUAccountingByResortsCount;
                        SKUOverrunsSum = isDeadline ? SKUOverrunsCount * SKUPrice : 0.00;

                        if(SKUOverrunsCount < 0) 
                        {
                            SKUOverrunsCount = 0;
                        }

                        if(SKUOverrunsSum < 0.00) 
                        {
                            SKUOverrunsSum = 0.00;
                        }


                    }   
                    break;
            }
        }

        #endregion Constructors
    }
}
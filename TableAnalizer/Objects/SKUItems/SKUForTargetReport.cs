using System.Drawing;
using System.Text;
using TableAnalizer.Enums;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Managers;

namespace TableAnalizer.Objects.SKUItems
{
    public class SKUForTargetReports : IColorable
    {
        #region Properties
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
        public int SKURemaining { get; set; }               // Склад: Остаток товаров

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

        // Is Parrales sku
        private bool IsParallelSku {get ;set;}

        public string SKUComment { get; set;} = string.Empty;
        public Color FillColor { get; set; }
        #endregion

        #region Constructors
        public SKUForTargetReports(SKU sku, bool isAnalyzeResortsForWarehouse, string? resortFromSKUsComment = null)
        {
            SKUmp = sku.Marketplace;
            SKUNumber = sku.SKUNumber;
            SKUBrand = sku.SKUBrand;
            SKUName = sku.SKUName;
            SKUPrice = sku.AveragePrice;

            SKUDate = sku.MaxRansomsDate;
            isDeadline = (SKUDate.ToDateTime(new TimeOnly()) - DateTime.Now).Duration().Days > sku.DeadLineDays;

            IsParallelSku = sku.IsParallelSKU;

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

                // Удаляем приходы из расхода по списанию
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
                CheckResortingsWithWriteOffin1C(in sku);
            }
            if(!string.IsNullOrEmpty(resortFromSKUsComment))
                SKUAccountingByResortsComment += resortFromSKUsComment;

            // Fill SKUDummiesBalance 
            if (sku.SKUDummiesBalance is not null)  
            {
                SKUDummies = sku.SKUDummiesBalance.Count;
            }

            SellmentStatus = new SellmentStatus(SKUSellmentStatus.Unknown);
            SellmentStatusString = SellmentStatus.StatusLabel;
            FillColor = SellmentStatus.StatusColor;

            Analize(isAnalyzeResortsForWarehouse);
        }

        private void CheckResortingsWithWriteOffin1C(in SKU sku)
        {
            // Учет пересортов на другие артикулы в балансе и комментарии 
            if(sku.SKUResortBalance is not null)
            {
                var writeOffIn1CItems = sku.SKUResortBalance.FindSkusInBalanceByPredicate(SKUManager.IsWriteOffIn1C);
                if (writeOffIn1CItems.HasValue)
                {
                    // Сумма списанных в 1С пересортов прибавляется к приходу и к расходу пересортами
                    if (int.IsPositive(writeOffIn1CItems.Value.sum))
                    {
                        SKUReceipt += writeOffIn1CItems.Value.sum;
                        SKUResort += writeOffIn1CItems.Value.sum;

                        SKUAccountingByResortsComment = writeOffIn1CItems.Value.comment;
                    }
                }
            };
        }

        private void Analize( bool isAnalyzeResortsForWarehouse) 
        {
            void CalculateTotalСonsumption() 
            {
                SKUTotalСonsumption = SKUShipped + SKUPaid + SKUResort + SKUDummies;
            }

            // 1. Первоначальный расчет общего расхода
            CalculateTotalСonsumption();

            // 2. Балансировка остатка и прихода
            BalancingReceiptShippedAndRemaining();

            // 3. Расчет перерасходов (информация для склада) 
            int skuShippedWithResort = 0;
            if(int.IsNegative(SKURemaining) && isAnalyzeResortsForWarehouse) 
            {
                skuShippedWithResort = Math.Abs(SKURemaining);
                SKURemaining = 0;
            }

            // 4. Расчет задержек довоза (в тч информация для склада)
            var notReceivedFromPoints = CalculateDeliveryDelay(isCalculateAmountsDelays: isDeadline);
            notReceivedFromPoints = isAnalyzeResortsForWarehouse ? notReceivedFromPoints : 0;

            // 4. Проверка на дедлайн
            if(!isDeadline) 
            {
                SellmentStatus = new SellmentStatus(SKUSellmentStatus.InWork);
            }
            else 
            {
                // Если ожидание <= расходу
                if(SKURansomsWaiting <= SKUTotalСonsumption) 
                {
                    // Статус - полный расчет 
                    SellmentStatus = new SellmentStatus(SKUSellmentStatus.FullSet);
                } 
 
                // 5. Расчет перерасходов
                CalculateOverruns();
           }

            // 6. Повторный расчет общего расхода
            CalculateTotalСonsumption();

            // 7. Строка статуса и цвет заливки
            SellmentStatusString = SellmentStatus.StatusLabel;
            FillColor = SellmentStatus.StatusColor;

            // Заполение комментария
            if(int.IsPositive(notReceivedFromPoints) || int.IsPositive(skuShippedWithResort)) 
            {
                StringBuilder stringBuilder = new("Информация для склада:\n");
                if(notReceivedFromPoints > 0) 
                {
                    stringBuilder.Append($"Недополучено складом от точек: {notReceivedFromPoints}");
                }

                if(skuShippedWithResort > 0) 
                {
                    stringBuilder.Append($"\nПередано клиенту с пересорром: {skuShippedWithResort}");
                }

                SKUComment += stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Balancing correctness of receipt, shipped and remaining.
        /// </summary>
        private void BalancingReceiptShippedAndRemaining() 
        {
            // SKUReceipt can`t be less than sum of SKUShipped plus SKURemaining + SKUResort.
            var shippedAndRemaining = SKUShipped + SKURemaining + SKUResort;
            if(SKUReceipt != shippedAndRemaining)
                SKUReceipt = shippedAndRemaining;
        }

        /// <summary>
        /// Calculation of delivery delays to client.
        /// </summary>
        /// <param name="isCalculateAmountsDelays">Flag for calculating amount of delayed delivery.</param>
        private int CalculateDeliveryDelay(bool isCalculateAmountsDelays)
        {
            var result = 0;

            // Если ожидание меньше или равно общему расходу и ожидание == приходу, задержки нулевые
            if(SKURansomsWaiting <= SKUTotalСonsumption && SKURansomsWaiting == SKUReceipt) 
            {
                SKUDeliveryDelayPoints = SKUDeliveryDelayStorage = SKUDeliveryDelayTotal = 0;
                SKUDeliveryDelayPointsSum = SKUDeliveryDelayStorageSum = 0.00;
            } 
            else 
            {
                // Если ожидание больше общего расхода
                if(SKURansomsWaiting > SKUTotalСonsumption) 
                {
                    // Задержка довоза от точек = ожидание минус приход
                    SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceipt;
                    if(int.IsNegative(SKUDeliveryDelayPoints)) SKUDeliveryDelayPoints = 0;
                } 
                else 
                {
                    // Недополучено от точек внутри склада
                    result = SKURansomsWaiting - SKUReceipt;
                    if(int.IsNegative(result)) result = 0;
                }
 
                if(isCalculateAmountsDelays && int.IsPositive(SKUDeliveryDelayPoints))
                {
                    SKUDeliveryDelayPointsSum = SKUDeliveryDelayPoints * SKUPrice;
                    if(double.IsNegative(SKUDeliveryDelayPointsSum)) SKUDeliveryDelayPointsSum = 0.00;
                }

                // Задержка довоза от склада - ожидание минус общий расход минус недовоз от точек
                if(int.IsPositive(SKURemaining)) 
                {
                    SKUDeliveryDelayStorage = SKURansomsWaiting - SKUTotalСonsumption - SKUDeliveryDelayPoints;
                    if(SKUReceipt > SKURansomsWaiting) 
                        SKUDeliveryDelayStorage += SKUReceipt - SKURansomsWaiting;

                    if(int.IsNegative(SKUDeliveryDelayStorage)) SKUDeliveryDelayStorage = 0;

                    if(isCalculateAmountsDelays && int.IsPositive(SKUDeliveryDelayStorage))
                    {
                        SKUDeliveryDelayStorageSum = SKUDeliveryDelayStorage * SKUPrice;
                        if(double.IsNegative(SKUDeliveryDelayStorageSum)) SKUDeliveryDelayStorageSum = 0.00;
                    }
                }
                
                // Общая задержка довоза
                SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;
                if(int.IsNegative(SKUDeliveryDelayTotal)) SKUDeliveryDelayTotal = 0;

                switch(SKUDeliveryDelayPoints > 0,SKUDeliveryDelayStorage > 0) 
                {
                    case (true, true):
                        SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchWarehouseAndPoints);
                        break;
                    case (true, false):
                        SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchPoints);
                        break;
                    case (false, true):
                        SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchWarehouse);
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        private void CalculateOverruns() 
        {
            // SKUOverrunsCount = SKUTotalСonsumption > SKURansomsWaiting && isDeadline ? SKUTotalСonsumption - SKURansomsWaiting : 0; 

            // switch(SKUOverrunsCount) 
            // {
            //     case <= 0:
            //         SKUOverrunsCount = 0;
            //         SKUOverrunsSum = 0.00;
            //         break;
            //     case > 0:
            //         if(resortSKUs == null) 
            //         {
            //             if(isDeadline) SKUOverrunsSum = SKUOverrunsCount * SKUPrice;
            //         } 
            //         else 
            //         {
            //             // по группам
            //             // сумма по группе артикула
            //             // - добавить в приход
            //             // - добавить в расход пересортами 
            //             // -- пересчитать общий расход 

            //             var g = from item in resortSKUs
                                



            //             foreach (KeyValuePair<int, int> item in resortSKUs)
            //             {
                            





            //                 var skuFromResort = item.Key;
            //                 SKUAccountingByResortsCount += item.Value;

            //                 SKUAccountingByResortsComment = string.IsNullOrEmpty(SKUAccountingByResortsComment) ?  
            //                     $"Пересорты от артикулов (вычтены с расхода товаром): {skuFromResort} - {item.Value}, " :
            //                     string.Concat(SKUAccountingByResortsComment, $"{skuFromResort} - {item.Value}, ");
            //             }

            //             // // Total sum of consumption of a sku is reduced by count of re-sorts for this item
            //             // if(int.IsPositive(SKUAccountingByResortsCount)) 
            //             //     SKUShipped -= SKUAccountingByResortsCount;
                        
            //             // SKUOverrunsCount -= SKUAccountingByResortsCount;
            //             // SKUOverrunsSum = isDeadline ? SKUOverrunsCount * SKUPrice : 0.00;

            //             // if(SKUOverrunsCount < 0)  SKUOverrunsCount = 0;
            //             // if(SKUOverrunsSum < 0.00)  SKUOverrunsSum = 0.00;
            //             // if(int.IsNegative(SKURemaining)) SKURemaining = 0;
            //         }   
            //         break;
            }

            // // If sku is parallel with another client, overruns is not considered
            // if(IsParallelSku) 
            // {
            //     SKUOverrunsCount = 0;
            //     SKUOverrunsSum = 0.00;
            //     // SKUComment += " '||'";
            // }
       // }

        #endregion 

    }
}
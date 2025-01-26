using System.Drawing;
using System.Text;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Enums;
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
        public SellmentStatus SellmentStatus { get; private set; }  = new SellmentStatus(SKUSellmentStatus.Unknown);
        public string SellmentStatusString => SellmentStatus.StatusLabel;
        
        public int SKURansomsTotal { get; set; }
        public int SKURansomsCanceled { get; set; }
        public int SKURansomsOnWay { get; set; }
        public int SKURansomsDelivered { get; set; }
        public int SKURansomsWithDrawn { get; set; }

        public int SKURansomsWaiting { get; set; }          // Склад: ожидание
        
        // Приход
        public int SKUTotalReceipt { get; set; }            // Склад: приход, всего 
        public int SKUReceipt { get; set; }                 // Склад: приход  товарамим
        public int SKUReceivedResorts { get; set; }         // Склад: приход пересортами

        public int SKUWriteOff { get; set; }                // Склад: списание недостач

        public int SKUTotalСonsumption { get; set;}         // Склад: общий расход (товаром, компенсацией, пересортом, пустшками)    
        public int SKUShipped { get; set; }                 // Склад: Расход товаром 
        public int SKUPaid { get; set; }                    // Склад: Расход компенсацией 
        public int SKUResort { get; set; }                  // Склад: Расход пересортом 
       
        // Склад: Списано пересортицей, товаров
        public int SKUWriteOffByResorts { get; set; }       // Склад: списано пересортами

        public int SKURemaining { get; set; }               // Склад: Остаток товаров

        // Delivery delay
        public int SKUDeliveryDelayTotal { get; set; }          // недовоз, товаров всего 
        public int SKUDeliveryDelayPoints { get; set; }         // недовоз от точек, товаров
        public double SKUDeliveryDelayPointsSum { get;set;}     // недовоз от точек, сумма
        public int SKUDeliveryDelayStorage { get; set; }        // недовоз от склада, товаров
        public double SKUDeliveryDelayStorageSum { get; set; }  // недовоз от склада, сумма 

        public string SKUResortsComment { get; set; } = string.Empty;  // Комментарий по пересортам

        // Overruns 
        public int SKUOverrunsCount { get; set; } = 0;          // перерасходы, товаров
        public double SKUOverrunsSum { get; set; } = 0.00;      // перерасходы, сумма

        // Is Parrales sku
        private bool IsParallelSku {get ;set;}

        public string SKUComment { get; set;} = string.Empty;

        public Color FillColor { get; set;}

        // Work properties
        /// <summary>
        /// Передано складом с пересортами (из отрицательных значений остатков).
        /// Слубежная информация, только для склада. 
        /// </summary>
        private int SKUShippedWithResort { get ; set;} = 0;
        /// <summary>
        /// Не довезено от точек до склада согласно предварительного расчета. 
        /// Слубежная информация, только для склада. 
        /// </summary>
        private int SKUNotReceivedFromPoints {get; set;} = 0;
        
        #endregion

        #region Constructors
        public SKUForTargetReports(SKU sku, bool isRawReport, Dictionary<int, int>? resortFromSkusValues = null)
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

            // Article analysis
            AnalizeSKU(sku, isRawReport, resortFromSkusValues);
        }

        private void SetSellmentStatus(SKUSellmentStatus skuSellmentStatus) 
        {
            SellmentStatus = new SellmentStatus(skuSellmentStatus);
            FillColor = SellmentStatus.StatusColor;
        }

        private void CalculateTotalСonsumption() 
        {
            SKUTotalСonsumption = SKUShipped + SKUPaid + SKUResort;
        }

         private void CalculateTotalReceipt() 
        {
            SKUTotalReceipt = SKUReceipt + SKUReceivedResorts;
        }

        private void AnalizeSKU(SKU sku, bool isRawReport, Dictionary<int, int>? resortFromSkusValues) 
        {
            CalculateTotalReceipt();

            // 0. Установка неопределенного статуса для всех позиций
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // 1. Подготовка данных для перерасходов
            // Если остаток товаров отрицательный, он выносится на калькуляцию в комментарий и 
            // определяется в отчете как 0.
            if(!isRawReport && SKURemaining < 0) 
            {
                SKUShippedWithResort = Math.Abs(SKURemaining);
                SKURemaining = 0;
            }

            // 2. Фикация исходящих пересортов 
            if (!isRawReport && sku.SKUResortBalance is not null)
            {
                var writeOffIn1CItems = sku.SKUResortBalance.FindSkusInBalanceByPredicate(SKUManager.IsWriteOffIn1C);
                if (writeOffIn1CItems.HasValue && writeOffIn1CItems.Value.sum > 0)
                { 
                    SKUWriteOffByResorts += writeOffIn1CItems.Value.sum;
                    SKUResortsComment = writeOffIn1CItems.Value.comment;
                }
            }

            // 3. Фикация входящих пересортов 
            if(!isRawReport && resortFromSkusValues is not null && resortFromSkusValues.Count > 0) 
            {
                // Оприходовано пересортицей
                SKUReceivedResorts = resortFromSkusValues.Aggregate(0, (acc, i) => acc + i.Value);
                
                // Приход товаром уменьшается на количество товаров в пересорте
                SKUReceipt -= SKUReceivedResorts;

                // Расход товаром уменьшается на количество товаров в пересорте
                SKUShipped -= SKUReceivedResorts;

                // Расход пересортом увеличивается на количество пересортов
                SKUResort += SKUReceivedResorts;

                // Обновление комментария по пересортам
                StringBuilder stringBuilder = new("Пересортица от артикулов (добавлено в \"Склад: Оприходовано пересортицей\", \"Склад: Расход пересортом\", вычтено из \"Склад: Приход товаром\",\"Склад: Расход товаром\"):\n"); 
                foreach ((int resortFromSkusValueSku, int resortFromSkusValueSum)  in resortFromSkusValues)
                {
                    stringBuilder.Append($"{resortFromSkusValueSku} шт.:{resortFromSkusValueSum}, ");
                }

                if(string.IsNullOrEmpty(SKUResortsComment)) 
                {
                    SKUResortsComment = stringBuilder.ToString();
                } 
                else 
                {
                    SKUResortsComment += $"\n{stringBuilder}";
                }
            }

            // Расчет общего расхода
            CalculateTotalСonsumption();

            // 4. Расчет задержек довоза и перерасходов
            var skuNotReceivedFromPoints = CalculateDelayAndOverruns();
            SKUNotReceivedFromPoints = !isRawReport ? skuNotReceivedFromPoints : 0;

            // 5. Итоговое заполнение общего комментация по артикулу
            if(!isRawReport && SKUNotReceivedFromPoints > 0 || SKUShippedWithResort > 0) 
            {
                if(SKUNotReceivedFromPoints > 0) 
                {
                    StringBuilder stringBuilder = new("Информация для склада:\n");
                    stringBuilder.Append($"Недополучено складом от точек: {SKUNotReceivedFromPoints}");
                    
                    if(SKUShippedWithResort > 0) 
                    {
                        stringBuilder.Append($"\nПередано клиенту с пересортом: {SKUShippedWithResort}");
                    }
                    SKUComment += stringBuilder.ToString();
                }
            }
        }

        private int CalculateDelayAndOverruns()
        {
            var result = 0;

            bool isGoodsDelivered = SKURansomsWaiting == SKUTotalСonsumption;
            bool isGoodsDeliveredWithOverruns = SKURansomsWaiting < SKUTotalСonsumption;
            bool isDelay = SKURansomsWaiting > SKUTotalСonsumption;

            SKUDeliveryDelayPoints = SKUDeliveryDelayStorage = SKUDeliveryDelayTotal = 0;
            SKUDeliveryDelayPointsSum = SKUDeliveryDelayStorageSum = 0.00;
            
            SKUOverrunsCount = 0;
            SKUOverrunsSum = 0.00;

            switch(isGoodsDelivered, isGoodsDeliveredWithOverruns, isDelay) 
            {
                // Все довезли, недовоза и перерасхода нет = полный расчет
                case (true, false, false): 
                    SetSellmentStatus(SKUSellmentStatus.FullSet);
                    break;
                // Все довезли c перерасходом, считаем перерасход
                case (false, true, false):                     
                    SKUOverrunsCount = SKUResort == 0 ? SKUTotalСonsumption - SKURansomsWaiting : 0;
                    if(int.IsNegative(SKUOverrunsCount)) SKUOverrunsCount = 0;
                    SKUOverrunsSum = SKUOverrunsCount > 0 && isDeadline ? SKUOverrunsCount * SKUPrice : 0.00;

                    SetSellmentStatus(SKUSellmentStatus.FullSet);
                    break;
                // Не все довезли, задержка довоза от точек и/или склада
                case (false, false, true): 

                    // Недовоз от точек = ожидание минус приход минус калькуляция минус вычтенные из прихода входяшие пересорты
                    SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceipt - SKUReceivedResorts;
                    if(int.IsNegative(SKUDeliveryDelayPoints)) SKUDeliveryDelayPoints = 0;
                    SKUDeliveryDelayPointsSum = SKUDeliveryDelayPoints > 0 && isDeadline ? SKUDeliveryDelayPoints * SKUPrice : 0.00;
                    
                    // Недовоз от склада = ожидание минус недовоз от точек - общий расход
                    SKUDeliveryDelayStorage = SKURansomsWaiting - SKUDeliveryDelayPoints - SKUTotalСonsumption - SKUWriteOffByResorts;
                    if(int.IsNegative(SKUDeliveryDelayStorage)) SKUDeliveryDelayStorage = 0;
                    SKUDeliveryDelayStorageSum = SKUDeliveryDelayStorage > 0 && isDeadline ? SKUDeliveryDelayStorage * SKUPrice : 0.00;
                    
                    // Общий недовоз 
                    SKUDeliveryDelayTotal = SKUDeliveryDelayPoints > 0 || SKUDeliveryDelayStorage > 0 ? 
                        SKUDeliveryDelayPoints + SKUDeliveryDelayStorage :
                        0;

                    switch(SKUDeliveryDelayPoints > 0, SKUDeliveryDelayStorage > 0) 
                    {
                        case (true, true):
                            SetSellmentStatus(SKUSellmentStatus.OnSearchWarehouseAndPoints);
                            break;
                        case (true, false):
                            SetSellmentStatus(SKUSellmentStatus.OnSearchPoints);
                            break;
                        case (false, true):
                            SetSellmentStatus(SKUSellmentStatus.OnSearchWarehouse);
                            break;
                        case (false, false):
                            SetSellmentStatus(SKUSellmentStatus.FullSet);
                            break;
                    }

                    break;
            }






            // // Если ожидание меньше или равно общему расходу и ожидание <= приходу, задержки нулевые
            // if(SKURansomsWaiting <= SKUTotalСonsumption && SKURansomsWaiting <= SKUReceipt) 
            // {
            //     SKUDeliveryDelayPoints = SKUDeliveryDelayStorage = SKUDeliveryDelayTotal = 0;
            //     SKUDeliveryDelayPointsSum = SKUDeliveryDelayStorageSum = 0.00;
            // } 
            // else 
            // {
            //     // Если ожидание больше общего расхода
            //     if(SKURansomsWaiting > SKUTotalСonsumption) 
            //     {
            //         // Задержка довоза от точек = ожидание минус приход
            //         SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceipt - ResortFromSum;
            //         if(int.IsNegative(SKUDeliveryDelayPoints)) SKUDeliveryDelayPoints = 0;
            //     } 
            //     else 
            //     {
            //         // Недополучено от точек внутри склада
            //         result = SKURansomsWaiting - SKUReceipt;
            //         result = result < 0 ? 0 : result; 
            //     }
 
            //     if(isCalculateAmountsDelays && SKUDeliveryDelayPoints > 0)
            //     {
            //         SKUDeliveryDelayPointsSum = SKUDeliveryDelayPoints * SKUPrice;
            //         if(double.IsNegative(SKUDeliveryDelayPointsSum)) SKUDeliveryDelayPointsSum = 0.00;
            //     }

            //     // Задержка довоза от склада - ожидание минус общий расход минус недовоз от точек
            //     if(SKURemaining > 0) 
            //     {
            //         SKUDeliveryDelayStorage = SKURansomsWaiting - SKUTotalСonsumption - SKUDeliveryDelayPoints - ResortFromSum;
            //         if(SKUReceipt > SKURansomsWaiting) 
            //             SKUDeliveryDelayStorage += SKUReceipt - SKURansomsWaiting;

            //         if(SKUDeliveryDelayStorage < 0) SKUDeliveryDelayStorage = 0;

            //         if(isCalculateAmountsDelays && SKUDeliveryDelayStorage > 0)
            //         {
            //             SKUDeliveryDelayStorageSum = SKUDeliveryDelayStorage * SKUPrice;
            //             if(double.IsNegative(SKUDeliveryDelayStorageSum)) SKUDeliveryDelayStorageSum = 0.00;
            //         }
            //     }
                
            //     // Общая задержка довоза
            //     SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;
            //     if(SKUDeliveryDelayTotal > 0) SKUDeliveryDelayTotal = 0;

            //     switch(SKUDeliveryDelayPoints > 0,SKUDeliveryDelayStorage > 0) 
            //     {
            //         case (true, true):
            //             SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchWarehouseAndPoints);
            //             break;
            //         case (true, false):
            //             SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchPoints);
            //             break;
            //         case (false, true):
            //             SellmentStatus = new SellmentStatus(SKUSellmentStatus.OnSearchWarehouse);
            //             break;
            //         default:
            //             break;
            //     }
            // }

            return result;
        }

        #endregion 
    }
}
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using OfficeOpenXml.Drawing.Style.ThreeD;
using OneOf.Types;
using Serilog.Formatting.Raw;
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
        
        /// <summary>
        /// Склад: Приход товаром
        /// </summary>
        public int SKUReceiptGoods { get; set; }                 
        /// <summary>
        /// Склад: Оприходовано пересортицей
        /// </summary>
        public int SKUReceiptResorts { get; set; }

        /*
        arrival of goods
a       rrival of mixed goods
        */

        public int SKUWriteOff { get; set; }                // Склад: списание недостач

        /// <summary>
        /// Склад: общий расход (товаром, компенсацией, пересортом) 
        /// </summary>
        public int SKUTotalСonsumption =>  SKUShipped + SKUPaid + SKUResort;
        
        public int SKUShipped { get; set; }                 // Склад: Расход товаром 
        public int SKUPaid { get; set; }                    // Склад: Расход компенсацией 
        /// <summary>
        /// Склад: расход пересортом                
        /// </summary>
        public int SKUResort { get; set; }    
       
        /// <summary>
        /// Склад: Списано пересортицей, товаров
        /// </summary>
        public int SKUWriteOffByResorts { get; set; }       

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
        public SKUForTargetReports(SKU sku, bool isRawReport, int traceDelay, Dictionary<int, int>? resortFromSkusValues = null)
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

                if(writeOff is not null && writeOff > 0) 
                {
                    Trace.WriteLine($"writeOff - {writeOff}");
                    Task.Delay(traceDelay);
                }
                
                // Удаляем приходы из расхода по списанию
                SKUReceiptGoods = writeOff.HasValue ? receipt - writeOff.Value : receipt;

                SKUShipped = sku.SKURemainingBalance.SKUShipped;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            }

            // Fill SKUCompensationBalance 
            if (sku.SKUCompensationBalance is not null)
            {
                SKUPaid = sku.SKUCompensationBalance.Count;
            }

            // Article analysis
            AnalizeSKU(sku, isRawReport, traceDelay, resortFromSkusValues);
        }

        private void SetSellmentStatus(SKUSellmentStatus skuSellmentStatus) 
        {
            SellmentStatus = new SellmentStatus(skuSellmentStatus);
            FillColor = SellmentStatus.StatusColor;
        }

        private void AnalizeSKU(SKU sku, bool isRawReport, int traceDelay, Dictionary<int, int>? resortFromSkusValues)
        {
            // Установка неопределенного статуса 
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Калькуляция пересоров 
            CalcResorts(sku, isRawReport, resortFromSkusValues);


            // Расчет остатка
            // Остаток на складе: общий приход минус общий расход 
            // SKURemaining = SKUTotalReceipt - SKUTotalСonsumption - SKUWriteOffByResorts;

            // 4. Расчет задержек довоза и перерасходов
            var skuNotReceivedFromPoints = CalculateDelayAndOverruns();
            SKUNotReceivedFromPoints = !isRawReport ? skuNotReceivedFromPoints : 0;

            // // 5. Итоговое заполнение общего комментация по артикулу
            // if (!isRawReport && SKUNotReceivedFromPoints > 0 || SKUShippedWithResort > 0)
            // {
            //     if (SKUNotReceivedFromPoints > 0)
            //     {
            //         StringBuilder stringBuilder = new("Информация для склада:\n");
            //         stringBuilder.Append($"Недополучено складом от точек: {SKUNotReceivedFromPoints}");

            //         if (SKUShippedWithResort > 0)
            //         {
            //             stringBuilder.Append($"\nПередано клиенту с пересортом: {SKUShippedWithResort}");
            //         }
            //         SKUComment += stringBuilder.ToString();
            //     }
            // }
        }

        private void CalcResorts(SKU sku, bool isRawReport, Dictionary<int, int>? resortFromSkusValues)
        {
            // 1. Информация для склада по отрицательному остатку
            // Если остаток товаров отрицательный, он выносится на калькуляцию в комментарий для склада и 
            // определяется в отчете как 0.
            if (!isRawReport && int.IsNegative(SKURemaining))
            {
                var bulder = new StringBuilder();
                if(string.IsNullOrEmpty(SKUComment)) 
                {
                    bulder.Append("Инфомрмация для склада:\n");
                }
                bulder.Append($"Отрицательный остаток склада '{SKURemaining}'");

                SKUComment += bulder.ToString();
                SKURemaining = 0;
            }

            // 2. Фикация исходящих пересортов 
            if (!isRawReport && sku.SKUResortBalance is not null)
            {
                // Переcорты, списанные в 1С
                var writeOffIn1CItems = sku.SKUResortBalance.FindSkusInBalanceByPredicate(SKUResortBalanceTarget.ResortIn1C);
                if (writeOffIn1CItems.HasValue && writeOffIn1CItems.Value.sum > 0)
                {
                    SKUWriteOffByResorts += writeOffIn1CItems.Value.sum;
                    SKUResortsComment = string.IsNullOrEmpty(SKUResortsComment) ?
                            writeOffIn1CItems.Value.comment :
                            SKUResortsComment += writeOffIn1CItems.Value.comment;
                }
            }

            // Переcорты, компенсированные между помещеняими 3 и 1 в 1С
            var compensation31 = sku.SKUResortBalance?.FindSkusInBalanceByPredicate(SKUResortBalanceTarget.CompensationInWarehouse31);
            {
                if (compensation31.HasValue && compensation31.Value.sum > 0)
                {
                    SKUReceiptGoods -= compensation31.Value.sum;
                    SKUResortsComment = string.IsNullOrEmpty(SKUResortsComment) ?
                        compensation31.Value.comment :
                        SKUResortsComment += compensation31.Value.comment;
                }
            }

            // 3. Фикация входящих пересортов 
            if (!isRawReport && resortFromSkusValues is not null && resortFromSkusValues.Count > 0)
            {
                // Оприходовано пересортицей
                SKUReceiptResorts = resortFromSkusValues.Aggregate(0, (acc, i) => acc + i.Value);

                // Приход товаром уменьшается на Оприходовано пересортицей
                SKUReceiptGoods -= SKUReceiptResorts;

                // Расход товаром уменьшается на Оприходовано пересортицей
                SKUShipped -= SKUReceiptResorts;

                // Расход пересортом увеличивается на Оприходовано пересортицей
                SKUResort += SKUReceiptResorts;

                // Обновление комментария по пересортам
                StringBuilder stringBuilder = new("Пересортица от артикулов (добавлено в \"Склад: Оприходовано пересортицей\" и \"Склад: Расход пересортом\", а также вычтено из \"Склад: Приход товаром\" и \"Склад: Расход товаром\"):\n");
                foreach ((int resortFromSkusValueSku, int resortFromSkusValueSum) in resortFromSkusValues)
                {
                    stringBuilder.Append($"{resortFromSkusValueSku} шт.:{resortFromSkusValueSum}, ");
                }

                if (string.IsNullOrEmpty(SKUResortsComment))
                {
                    SKUResortsComment = stringBuilder.ToString();
                }
                else
                {
                    SKUResortsComment += $"\n\n{stringBuilder.ToString()}";
                }
            }
        }

        private int CalculateDelayAndOverruns()
        {
            SKUDeliveryDelayPoints = SKUDeliveryDelayStorage = SKUDeliveryDelayTotal = SKUDeliveryDelayTotal = SKUOverrunsCount = 0;
            SKUDeliveryDelayPointsSum = SKUDeliveryDelayStorageSum = SKUOverrunsSum = 0.00;

         
                if(SKURansomsWaiting == SKUReceiptGoods + SKUReceiptResorts) 
                {
                    if(SKUReceiptGoods == SKUShipped) 
                    {
                        // Полный расчет
                    }
                } 
                // Ожидание больше прихода товаров 
                else if (SKURansomsWaiting > SKUReceiptGoods + SKUReceiptResorts) 
                {
                    SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceiptGoods;
                }
                // Ожидание меньше прихода товаров 
                else if (SKURansomsWaiting < SKUReceiptGoods + SKUReceiptResorts) 
                {
                    var tc = SKUShipped + SKUWriteOffByResorts;
                    if(tc >= SKURansomsWaiting) 
                    {
                        SKUOverrunsCount = tc - SKURansomsWaiting;
                    } 
                    else 
                    {
                        SKUDeliveryDelayStorage = SKURansomsWaiting - tc;
                    }
                }

                SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;

                SKUDeliveryDelayPointsSum = isDeadline && SKUDeliveryDelayPoints > 0 ?
                    SKUDeliveryDelayPoints * SKUPrice : 
                    0.00;

                SKUDeliveryDelayStorageSum = isDeadline && SKUDeliveryDelayStorage > 0 ?
                    SKUDeliveryDelayStorage * SKUPrice : 
                    0.00;
            



            
           







            //    // // Общий результат взаиморасчета по артикулу: общий расход + списание по пересортами
            //     //var resultSellment = SKUTotalСonsumption + SKUWriteOffByResorts;

            //     // Общий результат взаиморасчета меньше ожидания то мы остались должны клиенту
            //     if(SKUTotalСonsumption < SKURansomsWaiting) 
            //     {
            //         // Остаток расчетов = ожидание - общий резльтат взаиморасчета
            //         var remainder = SKURansomsWaiting - SKUTotalСonsumption - SKUWriteOffByResorts;

            //         if(SKUReceiptGoods < SKURansomsWaiting) 
            //         {
            //             SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceiptGoods;
            //             remainder -= SKUDeliveryDelayPoints;

            //             if(remainder > 0) 
            //             {
            //                 SKUDeliveryDelayStorage = remainder;
            //             }
            //         } 
            //         else if(SKUReceiptGoods > SKURansomsWaiting) 
            //         {
            //             SKUComment += SKUReceiptGoods - SKURansomsWaiting;
            //         }

            //         SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;
            //     } 






            //     // // Справочно в комментарий
            //     // // Общий расход + Списано пересортицей - Расход пересортом = перерасход
            //     // if((SKUTotalСonsumption + SKUWriteOffByResorts - SKUResort) > SKURansomsWaiting)
            //     //     SKUComment += SKUTotalСonsumption + SKUWriteOffByResorts - SKURansomsWaiting;



            //     // // Недовоз от точек
            //     // SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceipt;
            //     // if(int.IsPositive(SKUDeliveryDelayPoints) && SKUDeliveryDelayPoints > 0 && isDeadline) 
            //     // {
            //     //     SKUDeliveryDelayPointsSum = SKUDeliveryDelayPoints * SKUPrice;
            //     // } 
            //     // else 
            //     // {
            //     //     SKUDeliveryDelayPoints = 0;
            //     //     SKUDeliveryDelayPointsSum = 0.00;
            //     // }

            //     // // Недовоз от склада
            //     // SKUDeliveryDelayStorage = SKURansomsWaiting - SKUDeliveryDelayPoints - SKUTotalСonsumption - SKUWriteOffByResorts;
            //     // if(int.IsPositive(SKUDeliveryDelayStorage) && SKUDeliveryDelayStorage > 0 && isDeadline) 
            //     // {
            //     //     SKUDeliveryDelayStorageSum = SKUDeliveryDelayStorage * SKUPrice;
            //     // } 
            //     // else 
            //     // {
            //     //     SKUDeliveryDelayStorage = 0;
            //     //     SKUDeliveryDelayStorageSum = 0.00;
            //     // }


            //     // var balanceDelayOfSettlement = SKURansomsWaiting - SKUTotalСonsumption;
            //     // switch(int.IsPositive(balanceDelayOfSettlement)) 
            //     // {
            //     //     // Число недовоза положительное
            //     //     case true:
            //     //        switch(balanceDelayOfSettlement == 0) 
            //     //        {
            //     //             // Число недовоза равно нулю, выходим
            //     //             case true:
            //     //                 break;
            //     //             // Число недовоза больше нуля, считаем недовозы (точки и склад)
            //     //             case false:

            //     //                 // Недовозы точки 
            //     //                 SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceipt;
            //                 if(int.IsNegative(SKUDeliveryDelayPoints)) SKUDeliveryDelayPoints = 0;

            //                 SKUDeliveryDelayPointsSum = SKUDeliveryDelayPoints > 0 && isDeadline ? 
            //                     SKUDeliveryDelayPoints * SKUPrice : 
            //                     SKUDeliveryDelayPointsSum = 0;

            //                 // Недовозы склад
            //                 SKUDeliveryDelayStorage = balanceDelayOfSettlement - SKUDeliveryDelayPoints - SKUTotalСonsumption;
            //                 if(int.IsNegative(SKUDeliveryDelayStorage)) SKUDeliveryDelayStorage = 0;

            //                 SKUDeliveryDelayStorageSum = SKUDeliveryDelayStorage > 0 && isDeadline ? 
            //                     SKUDeliveryDelayStorage * SKUPrice : 
            //                     0.00;

            //                 // Недовозы общие 
            //                 switch(SKUDeliveryDelayPoints > 0, SKUDeliveryDelayStorage > 0) 
            //                 {
            //                     case (true, true):
            //                         SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;
            //                         break;
            //                     case (true, false):
            //                         SKUDeliveryDelayTotal = SKUDeliveryDelayPoints;
            //                         break;
            //                     case (false, true):
            //                         SKUDeliveryDelayTotal = SKUDeliveryDelayStorage;
            //                         break;
            //                 }

            //                 break;
            //        }
            //         break;

            //     // Число отрицательное - есть перерасходы
            //     case false:
            //         SKUOverrunsCount = Math.Abs(balanceDelayOfSettlement);
            //         SKUOverrunsSum = int.IsPositive(SKUOverrunsCount) && SKUOverrunsCount > 0 && isDeadline ? 
            //             SKUOverrunsCount * SKUPrice :
            //             0.00;
            //         break;
            // }

            // Add status 
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

            return 0;
        }

        #endregion 
    }
}
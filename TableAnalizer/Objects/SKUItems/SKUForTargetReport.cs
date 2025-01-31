using System.Diagnostics;
using System.Drawing;
using System.Text;
using OfficeOpenXml.Drawing.Style.ThreeD;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Finance.Implementations;
using OneOf.Types;
using Serilog.Formatting.Raw;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Enums;
using TableAnalizer.Objects.Managers;
using Tensorflow;
using Tensorflow.Keras.ArgsDefinition;

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
        
        public int SKUReceiptTolal => SKUReceiptGoods + SKUReceiptResorts;

        /// <summary>
        /// Склад: Приход товаром
        /// </summary>
        public int SKUReceiptGoods { get; set; } = 0;              
        /// <summary>
        /// Склад: Оприходовано пересортицей
        /// </summary>
        public int SKUReceiptResorts { get; set; } = 0;

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


        // ...
        public int SKUInlineResort { get; set; } = 0;
        
        #endregion

        #region Constructors
        public SKUForTargetReports(SKU sku, bool isRawReport, int traceDelay, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues = null)
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

        private void AnalizeSKU(SKU sku, bool isRawReport, int traceDelay, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // Установка неопределенного статуса 
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Калькуляция пересоров 
            if (!isRawReport)
            {
                CalcResorts(sku, resortFromSkusValues);
            }

            // Расчет задержек довоза и перерасходов
            var skuNotReceivedFromPoints = CalculateDelayAndOverruns();
            SKUNotReceivedFromPoints = !isRawReport ? skuNotReceivedFromPoints : 0;

            // Получение информации для пересортов (в работу склада)
            GetInfoForResorts();
        }

        private void GetInfoForResorts()
        {
            // Информация для склада
            // Если общий приход больше ожидания: выносим в комментарий
            if (SKUReceiptTolal > SKURansomsWaiting)
            {
                SKUComment += $"Общий приход больше ожидания на {Math.Abs(SKUReceiptTolal - SKURansomsWaiting)}\n\n";
            }

            // Информация для склада
            // Если остаток товаров отрицательный: выносим в комментарий и обнуляем его 
            if (int.IsNegative(SKURemaining))
            {
                SKUComment += $"Отрицательный остаток {Math.Abs(SKURemaining)}\n\n";
                SKURemaining = 0;
            }
        }

        private void CalcResorts(SKU sku, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // 2. Фикация исходящих пересортов 
            if (sku.SKUResortBalance is not null)
            {
                // 2.1 Переcорты прихода, списанные в 1С
                var resortIn1CReceiptItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.ResortIn1CReceipt);
                if (resortIn1CReceiptItems.HasValue && resortIn1CReceiptItems.Value.sum > 0)
                {
                    SKUReceiptGoods -= resortIn1CReceiptItems.Value.sum;
                    SKUResortsComment = string.IsNullOrEmpty(SKUResortsComment) ?
                            resortIn1CReceiptItems.Value.comment :
                            SKUResortsComment += resortIn1CReceiptItems.Value.comment;
                }

                // Переcорты, списанные в 1С
                var resortIn1CItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.ResortIn1C);
                if (resortIn1CItems.HasValue && resortIn1CItems.Value.sum > 0)
                {
                    SKUWriteOffByResorts += resortIn1CItems.Value.sum;
                    SKUResortsComment = string.IsNullOrEmpty(SKUResortsComment) ?
                            resortIn1CItems.Value.comment :
                            SKUResortsComment += resortIn1CItems.Value.comment;
                }

                // Пересорты, созданные без списания в 1С (концептуальные) 
                var conceptualItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.Conceptual);
                if (conceptualItems.HasValue && conceptualItems.Value.sum > 0)
                {
                    //SKUWriteOffByResorts += conceptualResortItems.Value.sum;
                    SKUResortsComment = string.IsNullOrEmpty(SKUResortsComment) ?
                            conceptualItems.Value.comment :
                            SKUResortsComment += conceptualItems.Value.comment;
                }
            }

            // 3. Фикация входящих пересортов 
            if (resortFromSkusValues is not null && resortFromSkusValues.Count > 0)
            {
                void UpdateResortComment(string message) 
                {
                    if(string.IsNullOrEmpty(SKUResortsComment)) 
                    {
                        SKUResortsComment = message;
                    } 
                    else 
                    {
                        SKUResortsComment += $"\n{message}";
                    }
                }

                IEnumerable<(int sku, int sum)>? GetResortValuesBy(SKUResortBalanceTarget balanceTarget)  => 
                    from resortFromSkusValue in resortFromSkusValues
                    from bb in  resortFromSkusValue.Value
                    where bb.Key == balanceTarget
                    let skuAndSum = (sku: resortFromSkusValue.Key, sum: bb.Value)
                    select skuAndSum;
                
                // Входящие пересорты: пересорты приходов
                var skuAndSumsByResortIn1CReceipt = GetResortValuesBy(SKUResortBalanceTarget.ResortIn1CReceipt);
                var skuAndSumsByResortIn1CReceiptSum = skuAndSumsByResortIn1CReceipt?.Aggregate(0, (acc, i) => acc + i.sum);
                if(skuAndSumsByResortIn1CReceipt is not null && skuAndSumsByResortIn1CReceipt.Any()) 
                {
                    // Прибавляются в Склад: Оприходовано пересортицей
                    //SKUReceiptResorts += skuAndSumsByResortIn1CReceiptSum ?? 0;

                    StringBuilder resortIn1CReceiptSB = new("Пересортица прихода от других артикулов (см. в \"Склад: Приход товаром\"): \n");
                    foreach ((int sk, int su) in skuAndSumsByResortIn1CReceipt)
                    {
                        resortIn1CReceiptSB.Append($"{sk} шт.:{su}, ");
                    }
                    UpdateResortComment(resortIn1CReceiptSB.ToString());
                }

                // Входящие пересорты: пересорты расходов
                var skuAndSumsByResortIn1C = GetResortValuesBy(SKUResortBalanceTarget.ResortIn1C);
                var skuAndSumsByResortIn1CSum = skuAndSumsByResortIn1C?.Aggregate(0, (acc, i) => acc + i.sum);
                if(skuAndSumsByResortIn1C is not null && skuAndSumsByResortIn1C.Any())  
                {
                    SKUResort += (int)skuAndSumsByResortIn1CSum!;   // Добавляется в Склад: Расход пересортом
                    SKUReceiptGoods -= SKUResort;                   // Вычитается из Склад: Приход товаром
                    SKUReceiptResorts += SKUResort;                 // Добавляется в Склад: Оприходовано пересортицей
                    SKUShipped -= SKUResort;                        // Вычитается из Склад: Расход товаром

                    // Обновление комментария
                    StringBuilder goodsResortsSb = new("Пересортица расхода от других артикулов (вычтено из \"Склад: Приход товаром\", \"Склад: Расход товаром\", добавлено в \"Склад: Оприходовано пересортицей\", \"Склад: Расход пересортом\",\n");
                    foreach (var (sk, su) in skuAndSumsByResortIn1C)
                    {
                        goodsResortsSb.Append($"{sk} шт.:{su}, ");
                    }

                    UpdateResortComment(goodsResortsSb.ToString());
                }
            }
        }

        private int CalculateDelayAndOverruns()
        {
            SKUDeliveryDelayPoints = SKUDeliveryDelayStorage = SKUDeliveryDelayTotal = SKUDeliveryDelayTotal = SKUOverrunsCount = 0;
            SKUDeliveryDelayPointsSum = SKUDeliveryDelayStorageSum = SKUOverrunsSum = 0.00;

            SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceiptGoods - SKUReceiptResorts;
            if(isDeadline && int.IsPositive(SKUDeliveryDelayPoints) && SKUDeliveryDelayPoints > 0)
            {
                SKUDeliveryDelayPointsSum = SKUDeliveryDelayPoints * SKUPrice;
            } 
            else 
            {
                SKUDeliveryDelayPoints = 0;
            }
            
            SKUDeliveryDelayStorage = SKURansomsWaiting - SKUTotalСonsumption - SKUWriteOffByResorts - SKUDeliveryDelayPoints;
            if(isDeadline && int.IsPositive(SKUDeliveryDelayStorage) && SKUDeliveryDelayStorage > 0)
            {
                SKUDeliveryDelayStorageSum = SKUDeliveryDelayStorage * SKUPrice;
            } 
            else 
            {
                SKUDeliveryDelayStorage = 0;
            }

            SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;

            if(SKUShipped > SKURansomsWaiting) 
            {
                SKUOverrunsCount = SKUShipped - SKURansomsWaiting - SKUResort;
                if(int.IsNegative(SKUOverrunsCount)) SKUOverrunsCount = 0;
                if(isDeadline && int.IsPositive(SKUOverrunsCount)&& SKUOverrunsCount > 0) 
                {
                    SKUOverrunsSum = SKUOverrunsCount * SKUPrice;
                } 
                else 
                {
                    SKUOverrunsSum = 0;
                }
            }

         
            // if(SKURansomsWaiting == SKUReceiptGoods + SKUReceiptResorts) 
            // {
            //     if(SKUReceiptGoods == SKUShipped) 
            //     {
            //         // Полный расчет
            //     }
            // } 
            // // Ожидание больше прихода товаров 
            // else if (SKURansomsWaiting > SKUReceiptGoods + SKUReceiptResorts) 
            // {
            //     SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceiptGoods;
            // }
            // // Ожидание меньше прихода товаров 
            // else if (SKURansomsWaiting < SKUReceiptGoods + SKUReceiptResorts) 
            // {
            //     var tc = SKUShipped + SKUWriteOffByResorts;
            //     if(tc >= SKURansomsWaiting) 
            //     {
            //         SKUOverrunsCount = tc - SKURansomsWaiting;
            //     } 
            //     else 
            //     {
            //         SKUDeliveryDelayStorage = SKURansomsWaiting - tc;
            //     }
            // }

            // SKUDeliveryDelayTotal = SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;

            // SKUDeliveryDelayPointsSum = isDeadline && SKUDeliveryDelayPoints > 0 ?
            //     SKUDeliveryDelayPoints * SKUPrice : 
            //     0.00;

            // SKUDeliveryDelayStorageSum = isDeadline && SKUDeliveryDelayStorage > 0 ?
            //     SKUDeliveryDelayStorage * SKUPrice : 
            //     0.00;
            



            
           







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
            if(!isDeadline) 
            {
                SetSellmentStatus(SKUSellmentStatus.InWork);
            } 
            else 
            {
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
            }
        
            return 0;
        }

        #endregion 
    }
}
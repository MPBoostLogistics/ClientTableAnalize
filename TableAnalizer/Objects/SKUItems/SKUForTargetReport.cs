using System.Drawing;
using System.Text;

namespace TableAnalizer.Objects.SKUItems
{
    public class SKUForTargetReports
    {
        #region Fields
        private const string RESORT_CONSUMPTION_FIRST_LINE = "Пересортица расхода от др.арт. (добавлен в \"Приход товаром\" и \"Расход пересортом\"):\n";

        #endregion

        #region Properties
        #region Properties - Common
        public Marketplace? SKUmp { get; set; }
        public int SKUNumber { get; set; }
        public string SKUBrand { get; set; }
        public string SKUName { get; set; }
        public double SKUPrice { get; set; }
        public DateOnly SKUDate { get; set; }
        private readonly bool isDeadline = false;
        public SellmentStatus SellmentStatus { get; private set; } = new SellmentStatus(SKUSellmentStatus.Unknown);
        public string SellmentStatusString => SellmentStatus.StatusLabel;

        // Is Parrales sku
        private bool IsParallelSku { get; set; }
        public Color FillColor { get; set; }

        #endregion
        #region Properties - Ransoms
        public int SKURansomsTotal => SKURansomsCanceled + SKURansomsOnWay + SKURansomsDelivered + SKURansomsWithDrawn;
        public int SKURansomsCanceled { get; set; }
        public int SKURansomsOnWay { get; set; }
        public int SKURansomsDelivered { get; set; }
        public int SKURansomsWithDrawn { get; set; }

        #endregion
        #region Properties - Waiting && Receipt
        /// <summary>
        /// Склад: Ожидание
        /// </summary>
        public int SKURansomsWaiting { get; set; }

        /// <summary>
        /// Приход от точек, всего
        /// </summary>
        public int SKUReceiptTotal => SKUReceiptGoods + SKUPaidPoints;

        /// <summary>
        /// из них, Склад: Приход товарами
        /// </summary>
        public int SKUReceiptGoods { get; set; } = 0;

        /// <summary>
        /// из них: Денежная компенсация
        /// </summary>
        public int SKUPaidPoints { get; set; } = 0;

        /// <summary>
        /// Склад: Приход от точек меньше ожидания
        /// </summary>
        public int LessReceiptThanWaiting =>
            SKUReceiptTotal < SKURansomsWaiting ?
            SKURansomsWaiting - SKUReceiptTotal :
            0;

        /// <summary>
        /// Склад: Приход от точек больше ожидания
        /// </summary>
        public int MoreReceiptThanWaiting =>
            SKUReceiptTotal > SKURansomsWaiting ?
            SKUReceiptTotal - SKURansomsWaiting :
            0;
        #endregion
        #region Properties - Comments

        private string skuResortsComment = string.Empty;

        /// <summary>
        /// Комментарий к пересортами
        /// </summary>
        public string SKUResortsComment
        {
            get => skuResortsComment;
            set => skuResortsComment = string.IsNullOrEmpty(skuResortsComment) ? skuResortsComment = value : skuResortsComment += $"\n{value}";
        }
        #endregion
        #region Properties - Mutual settlement

        /// <summary>
        /// Общий взаиморасчет (товаром, передачей на фулфилмент, компенсацией склада, пересортом) 
        /// </summary>
        public int SKUMutualSettlement => SKUOutgoingProducts + SKUTransfered + SKUPaidWarehouse + SKUResort;

        /// <summary>
        /// расход товарами
        /// </summary>
        public int SKUOutgoingProducts { get; set; }

        /// <summary>
        /// Склад: Передано товаром (фулфилмент)
        /// </summary>
        public int SKUTransfered { get; set; }

        /// <summary>
        /// Склад: Денежная компенсация
        /// </summary>                       
        public int SKUPaidWarehouse { get; set; } = 0;

        /// <summary>
        /// Склад: расход пересортом                
        /// </summary>
        public int SKUResort { get; set; }

        /// <summary>
        /// Общий взаиморасчет меньше ожидания клиента
        /// </summary>
        public int LessMutualSettlementThanWaiting => 
            SKUMutualSettlement < SKURansomsWaiting ? 
                SKURemaining > 0 ? 
                SKURansomsWaiting - SKUMutualSettlement - SKURemaining : 
                SKURansomsWaiting - SKUMutualSettlement : 
            0;

        /// <summary>
        /// Общий взаиморасчет больше ожидания клиента
        /// </summary>
        public int MoreMutualSettlementThanWaiting =>
            SKUMutualSettlement > SKURansomsWaiting ?
            SKUMutualSettlement - SKURansomsWaiting :
            0;

        #endregion
        #region Properties - Remaining
        /// <summary>
        /// Склад: Остаток товаров
        /// </summary>
        public int SKURemaining { get; set; }
        #endregion
        #region Properties - Delivery  
        /// <summary>
        /// Задержка довоза, всего
        /// </summary>
        public int SKUDeliveryDelayTotal => SKUDeliveryDelayPoints + SKUDeliveryDelayStorage;

        private int skuDeliveryDelayPoints = 0;
        /// <summary>
        /// Задержка довоза от точек, кол-во
        /// </summary>
        public int SKUDeliveryDelayPoints
        {
            get => skuDeliveryDelayPoints;
            set => skuDeliveryDelayPoints = value > 0 ? value : 0;
        }
        /// <summary>
        /// Задержка довоза от точек, сумма руб.
        /// </summary>
        public double SKUDeliveryDelayPointsSum =>
            SKUDeliveryDelayPoints > 0 && isDeadline ?
            SKUDeliveryDelayPoints * SKUPrice :
            0.00;

        private int skuDeliveryDelayStorage = 0;
        /// <summary>
        /// Задержка довоза от склада, кол-во
        /// </summary>
        public int SKUDeliveryDelayStorage
        {
            get => skuDeliveryDelayStorage;
            set => skuDeliveryDelayStorage = value > 0 ? value : 0;
        }
        /// <summary>
        /// Задержка довоза от склада, сумма руб.
        /// </summary>
        public double SKUDeliveryDelayStorageSum =>
            SKUDeliveryDelayStorage > 0 && isDeadline ?
            SKUDeliveryDelayStorage * SKUPrice :
            0.00;

        #endregion
        #region Properties - Overruns 
        private int skuOverrunsCount = 0;
        /// <summary>
        /// Перерасход, кол-во
        /// </summary>
        public int SKUOverrunsCount
        {
            get => skuOverrunsCount;
            set => skuOverrunsCount = value > 0 ? value : 0;
        }
        /// <summary>
        /// Перерасход, сумма руб.
        /// </summary>              
        public double SKUOverrunsSum =>
            SKUOverrunsCount > 0 && isDeadline ?
             SKUOverrunsCount * SKUPrice :
             0.00;

        #endregion

        #endregion

        #region Constructors
        public SKUForTargetReports(SKU sku, bool isRawReport, bool isMulty, int traceDelay, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues = null)
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
                SKUReceiptGoods = writeOff.HasValue ? receipt - writeOff.Value : receipt;

                SKUOutgoingProducts = sku.SKURemainingBalance.SKUShipped;
                SKUTransfered = sku.SKURemainingBalance.SKUTransfered;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            }

            // Fill SKUCompensationBalance 
            if (sku.SKUCompensationBalance is not null)
            {
                SKUPaidWarehouse = sku.SKUCompensationBalance.CompensationItems
                    .Where(el => el.Key == CompensationTarget.Warehouse)
                    .Aggregate(0, (acc, i) => acc + i.Value.count);

                SKUPaidPoints = sku.SKUCompensationBalance.CompensationItems
                    .Where(el => el.Key == CompensationTarget.PickupPoint)
                    .Aggregate(0, (acc, i) => acc + i.Value.count);
            }

            // Article analysis
            AnalizeSKU(sku, isRawReport, isMulty, traceDelay, resortFromSkusValues);
        }

        #endregion

        #region Functionality
        private void AnalizeSKU(SKU sku, bool isInteralReport, bool isSummary, int traceDelay, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // Setting status to undefined
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Calculation of resorts
            CalculateResorts(sku, resortFromSkusValues);

            // Compensation for goods receipts anomalies
            if(!isInteralReport)
                CompensationGoodsReceiptsAnomalies();

            // Calculation of shortages from points 
            CalculateDelay(SKUSellmentStatus.OnSearchPoints);
      
            // Calculation of shortages from the warehouse
            CalculateDelay(SKUSellmentStatus.OnSearchWarehouse);

            // Calculation of overruns
            var totalСonsumptionWithoutResorts = SKUMutualSettlement - SKUResort;
            if (totalСonsumptionWithoutResorts > SKURansomsWaiting)
            {
                SKUOverrunsCount = totalСonsumptionWithoutResorts - SKURansomsWaiting;
            }

            // Setting the status 
            SetStatus();
        }

        private void CalculateDelay(SKUSellmentStatus sellmentStatus)
        {
            switch (sellmentStatus)
            {
                case SKUSellmentStatus.OnSearchPoints:
                    SKUDeliveryDelayPoints = SKUMutualSettlement >= SKURansomsWaiting ? 0 : SKURansomsWaiting - SKUReceiptTotal;
                    break;
                case SKUSellmentStatus.OnSearchWarehouse:
                    SKUDeliveryDelayStorage = SKURansomsWaiting - SKUDeliveryDelayPoints - SKUMutualSettlement;
                    break;
                default:
                    break;
            }
        }

        private void CalculateResorts(SKU sku, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // Фикация исходящих пересортов 
            if (sku.SKUResortBalance is not null)
            {
                // Исходящие: Пересорты расхода
                var resortConsumptionItems = sku.SKUResortBalance?.FindSkusInBalanceBy(SKUResortBalanceTarget.ResortingConsumption);
                if (resortConsumptionItems.HasValue && resortConsumptionItems.Value.sum > 0)
                {
                    SKUOutgoingProducts -= resortConsumptionItems.Value.sum;

                    if (resortConsumptionItems.Value.sum >= MoreReceiptThanWaiting)
                    {
                        SKUReceiptGoods -= resortConsumptionItems.Value.sum;
                        if(int.IsNegative(SKUReceiptGoods)) 
                        {
                            SKUReceiptGoods = 0;
                        }
                    }

                    SKUResortsComment += resortConsumptionItems.Value.comment;
                }
            }

            // Фикация входящих пересортов 
            if (resortFromSkusValues is not null && resortFromSkusValues.Count > 0)
            {
                IEnumerable<(int sku, int sum)>? GetResortValuesBy(SKUResortBalanceTarget balanceTarget) =>
                    from resortFromSkusValue in resortFromSkusValues
                    from targetWithSum in resortFromSkusValue.Value
                    where targetWithSum.Key == balanceTarget
                    let skuAndSum = (sku: resortFromSkusValue.Key, sum: targetWithSum.Value)
                    select skuAndSum;


                // Исходящие: Пересорты расхода
                var resortConsumptionItems = GetResortValuesBy(SKUResortBalanceTarget.ResortingConsumption);
                var resortConsumptionItemsSum = resortConsumptionItems?.Aggregate(0, (acc, i) => acc + i.sum);
                if (resortConsumptionItems is not null && resortConsumptionItems.Any() && resortConsumptionItemsSum > 0)
                {

                    SKUResort += (int)resortConsumptionItemsSum;

                    if(resortConsumptionItemsSum >= LessReceiptThanWaiting) 
                    {
                        SKUReceiptGoods += (int)resortConsumptionItemsSum;
                    }

                    StringBuilder goodsResortsSb = new(RESORT_CONSUMPTION_FIRST_LINE);
                    foreach (var (sk, su) in resortConsumptionItems)
                    {
                        goodsResortsSb.Append($"{sk} шт.:{su}, ");
                    }

                    SKUResortsComment = goodsResortsSb.ToString();
                }

            }
            // {
            //     IEnumerable<(int sku, int sum)>? GetResortValuesBy(SKUResortBalanceTarget balanceTarget)  => 
            //         from resortFromSkusValue in resortFromSkusValues
            //         from targetWithSum in  resortFromSkusValue.Value
            //         where targetWithSum.Key == balanceTarget
            //         let skuAndSum = (sku: resortFromSkusValue.Key, sum: targetWithSum.Value)
            //         select skuAndSum;

            //     // Входящие пересорты: пересорты приходов (1С)
            //     var skuAndSumsByResortIn1CReceipt = GetResortValuesBy(SKUResortBalanceTarget.ResortIn1CReceipt);
            //     var skuAndSumsByResortIn1CReceiptSum = skuAndSumsByResortIn1CReceipt?.Aggregate(0, (acc, i) => acc + i.sum);
            //     if(skuAndSumsByResortIn1CReceipt is not null && skuAndSumsByResortIn1CReceipt.Any() && skuAndSumsByResortIn1CReceiptSum > 0) 
            //     {
            //         // Данные здесь не меняются, так как есть проводка в 1С

            //         StringBuilder resortIn1CReceiptSB = new("\nПересортица прихода от др.арт. (1С, см \"Склад: Приход товаром\"): \n");
            //         foreach ((int sk, int su) in skuAndSumsByResortIn1CReceipt)
            //         {
            //             resortIn1CReceiptSB.Append($"{sk} шт.:{su}, ");
            //         }

            //         SKUResortsComment = resortIn1CReceiptSB.ToString();
            //     }

            //     // Входящие пересорты: пересорты расходов (1С)
            //     var skuAndSumsByResortIn1C = GetResortValuesBy(SKUResortBalanceTarget.ResortIn1C);
            //     var skuAndSumsByResortIn1CSum = skuAndSumsByResortIn1C?.Aggregate(0, (acc, i) => acc + i.sum);
            //     if(skuAndSumsByResortIn1C is not null && skuAndSumsByResortIn1C.Any())  
            //     {
            //         SKUResort += (int)skuAndSumsByResortIn1CSum!;   // Добавляется в Склад: Расход пересортом

            //         // Обновление комментария
            //         StringBuilder goodsResortsSb = new("\nПересортица расхода от др. арт. (1С, добавлено в \"Склад: Расход пересортом\",\n");
            //         foreach (var (sk, su) in skuAndSumsByResortIn1C)
            //         {
            //             goodsResortsSb.Append($"{sk} шт.:{su}, ");
            //         }

            //         SKUResortsComment = goodsResortsSb.ToString();
            //     }

            //     // Входящие пересорты: пересорты расходов (концептуальные)
            //     var skuAndSumsByResortConceprual = GetResortValuesBy(SKUResortBalanceTarget.Conceptual);
            //     var skuAndSumsByResortConceprualSum = skuAndSumsByResortConceprual?.Aggregate(0, (acc, i) => acc + i.sum);
            //     if(skuAndSumsByResortConceprual is not null && skuAndSumsByResortConceprual.Any())  
            //     {
            //         // //SKUResort += (int)skuAndSumsByResortConceprualSum!;   // Добавляется в Склад: Расход пересортом
            //         // SKUReceiptGoods -= (int)skuAndSumsByResortConceprualSum!;
            //         // SKUShipped -= (int)skuAndSumsByResortConceprualSum!;    // Вычитается из Склад: Расход товаром

            //         // // Обновление комментария
            //         // StringBuilder goodsResortsSb = new("\nПересортица расхода от др. арт. (баланс, вычтено из \"Склад: Приход товаром\" и \"Склад: Расход товаром\": \n");
            //         // foreach (var (sk, su) in skuAndSumsByResortConceprual)
            //         // {
            //         //     goodsResortsSb.Append($"{sk} шт.:{su} ");
            //         // }

            //         // SKUResortsComment = goodsResortsSb.ToString();
            //     }
            // }
        }

        private void SetStatus()
        {
            // Add status 
            if (!isDeadline)
            {
                SetSellmentStatus(SKUSellmentStatus.InWork);
            }
            else
            {
                switch (SKUDeliveryDelayPoints > 0, SKUDeliveryDelayStorage > 0)
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
        }

        private void SetSellmentStatus(SKUSellmentStatus skuSellmentStatus)
        {
            SellmentStatus = new SellmentStatus(skuSellmentStatus);
            FillColor = SellmentStatus.StatusColor;
        }

        /// <summary>
        /// Компенсация приходов товаров
        /// </summary>
        private void CompensationGoodsReceiptsAnomalies() 
        {
            /// Если приход меньше ожидания, но взаиморасчет больше или равен ожиданию
            /// приход товаров уменьшается на эту разницу
            if(LessReceiptThanWaiting > 0 && SKUMutualSettlement >= SKURansomsWaiting) 
            {
                SKUReceiptGoods -= LessReceiptThanWaiting;
            }

            /// Если приход больше ожидания, приход товаров уменьшается на эту разницу
            if(MoreReceiptThanWaiting > 0) 
            {
                SKUReceiptGoods -= MoreReceiptThanWaiting;
            }
        }
        #endregion
    }
}
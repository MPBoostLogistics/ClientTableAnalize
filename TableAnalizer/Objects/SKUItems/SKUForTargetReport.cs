using System.Diagnostics;
using System.Drawing;
using System.Text;
using TableAnalizer.Interfaces;

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

        /// <summary>
        /// Склад: Ожидание
        /// </summary>
        public int SKURansomsWaiting { get; set; }  

        /// <summary>
        /// Склад: Приход товаром
        /// </summary>
        public int SKUReceiptGoods { get; set; } = 0;              

        /// <summary>
        /// Положительная разница между приходом и ожиданием 
        /// </summary>
        public int SKUIncreasedReceiptGoods => 
            SKUReceiptGoods > SKURansomsWaiting ? 
            SKUReceiptGoods - SKURansomsWaiting : 
            0;

        /// <summary>
        /// Склад: Компенсация (точки)
        /// </summary>                       
        private readonly int skuPaidWarehouse = 0;
        /// <summary>
        /// Склад: Компенсация (склад)
        /// </summary>                       
        private readonly int skuPaidPoints = 0; 

        private string skuPaidComment = string.Empty;
        public string SKUPaidComment 
        { 
            get => skuPaidComment;
            set => skuPaidComment = string.IsNullOrEmpty(skuPaidComment) ? skuPaidComment = value : skuPaidComment += $"\n{value}" ;
        }

        /// <summary>
        /// Склад: общий расход (товаром, передачей на фулфилмент, компенсацией, пересортом) 
        /// </summary>
        public int SKUTotalСonsumption => SKUShipped + SKUTransfered + SKUResort + SKUPaid;
        /// <summary>
        /// Склад: Расход товаром
        /// </summary>
        public int SKUShipped { get; set; }     
        /// <summary>
        /// Склад: Передано товаром (фулфилмент)
        /// </summary>
        public int SKUTransfered { get; set; } 
        /// <summary>
        /// Склад: Расход компенсацией (склад)
        /// </summary>                       
        public int SKUPaid { get; set; }       

        /// <summary>
        /// Склад: расход пересортом                
        /// </summary>
        public int SKUResort { get; set; }         

        /// <summary>
        /// Склад: Остаток товаров
        /// </summary>
        public int SKURemaining { get; set; }             

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
        public double SKUDeliveryDelayPointsSum => SKUDeliveryDelayPoints > 0 && isDeadline ? SKUDeliveryDelayPoints * SKUPrice : 0.00;
        
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
        public double SKUDeliveryDelayStorageSum => SKUDeliveryDelayStorage > 0 && isDeadline ? SKUDeliveryDelayStorage * SKUPrice : 0.00;

        private string skuResortsComment = string.Empty;
        /// <summary>
        /// Комментарий к пересортами
        /// </summary>
        public string SKUResortsComment 
        { 
            get => skuResortsComment;
            set => skuResortsComment = string.IsNullOrEmpty(skuResortsComment) ? skuResortsComment = value : skuResortsComment += $"\n{value}" ;
        }

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
        public double SKUOverrunsSum => SKUOverrunsCount > 0 && isDeadline ? SKUOverrunsCount * SKUPrice : 0.00;

        // Is Parrales sku
        private bool IsParallelSku {get ;set;}

        public Color FillColor { get; set;}

        /// <summary>
        /// Положительна разница между общим расходом и приходом 
        /// (c учетом компенсации и перерасходов)
        /// </summary>
        public int TotalConsumptionIsGreaterReceipt => 
            SKUTotalСonsumption > (SKUReceiptGoods + SKUPaid + SKUResort) ? 
            SKUTotalСonsumption - SKUReceiptGoods : 
            0;

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
                SKUReceiptGoods = writeOff.HasValue ? receipt - writeOff.Value : receipt;

                SKUShipped = sku.SKURemainingBalance.SKUShipped;
                SKUTransfered = sku.SKURemainingBalance.SKUTransfered;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            } 

            // Fill SKUCompensationBalance 
            if (sku.SKUCompensationBalance is not null )
            {
                skuPaidWarehouse = sku.SKUCompensationBalance.CompensationItems
                    .Where(el => el.Key == CompensationTarget.Warehouse)
                    .Aggregate(0, (acc, i) => acc + i.Value.count);

                skuPaidPoints = sku.SKUCompensationBalance.CompensationItems
                    .Where(el => el.Key == CompensationTarget.PickupPoint)
                    .Aggregate(0, (acc, i) => acc + i.Value.count);
            }

            // Article analysis
            AnalizeSKU(sku, isRawReport, isMulty, traceDelay, resortFromSkusValues);
        }
        
        #endregion

        #region Functionality
        private void AnalizeSKU(SKU sku, bool isRawReport, bool isSummary, int traceDelay, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // Setting status to undefined
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Checking compensation
            SetCompensation();

            // Calculation of resorts
            if (!isRawReport)
            {
                CalculateResorts(sku, resortFromSkusValues);

                // Calculation of shortages from points (повторно)
                CalculateDalay(SKUSellmentStatus.OnSearchPoints);
            } 
            else 
            {
                // Calculation of shortages from points
                CalculateDalay(SKUSellmentStatus.OnSearchPoints);
            }

            // Calculation of shortages from the warehouse
            CalculateDalay(SKUSellmentStatus.OnSearchWarehouse);

            // Calculation of overruns
            var totalСonsumptionWithoutResorts = SKUTotalСonsumption - SKUResort;
            if(totalСonsumptionWithoutResorts > SKURansomsWaiting) 
            {
                SKUOverrunsCount = totalСonsumptionWithoutResorts - SKURansomsWaiting;
            }

            // Setting the status 
            SetStatus();
        }

        /// <summary>
        /// Sets amount of compensation from points and warehouse.
        /// </summary>
        private void SetCompensation()
        {
            if(skuPaidPoints == 0 && skuPaidWarehouse == 0) return;

             StringBuilder paidSB = new("Оплачено денежными компенсациями:\n");

            // Если оплата компенсации от склада
            // - добавляется в расход компенсацией склада
            // - обновляется комментарий
            if(skuPaidWarehouse > 0) 
            {
                SKUPaid += skuPaidWarehouse;
                paidSB.Append($"- от склада (добавлено в \"Склад: Расход компенсацией\"): {skuPaidWarehouse}\n");
            }

            // Если оплата компенсации от точек
            // - добавляется в приход и расход компенсацией склада
            // - обновляется комментарий
            if(skuPaidPoints > 0) 
            {
                SKUReceiptGoods += skuPaidPoints;
                SKUPaid += skuPaidPoints;
                paidSB.Append($"- от точек (добавлено в \"Склад: Приход товаром\" и \"Склад: Расход компенсацией\"): {skuPaidPoints}\n");
            }

            SKUPaidComment = paidSB.ToString();
        }

        private void CalculateDalay(SKUSellmentStatus sellmentStatus) 
        {
            switch(sellmentStatus) 
            {
                case SKUSellmentStatus.OnSearchPoints:
                    SKUDeliveryDelayPoints = SKUTotalСonsumption >= SKURansomsWaiting ?  0 : SKURansomsWaiting - SKUReceiptGoods;
                    break;
                case SKUSellmentStatus.OnSearchWarehouse:
                    SKUDeliveryDelayStorage = SKURansomsWaiting - SKUDeliveryDelayPoints - SKUTotalСonsumption;
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
                // Переcорты приходов (проведено в 1С)
                var resortIn1CReceiptItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.ResortIn1CReceipt);
                if (resortIn1CReceiptItems.HasValue && resortIn1CReceiptItems.Value.sum > 0)
                {
                    SKUReceiptGoods -= resortIn1CReceiptItems.Value.sum;
                    SKUResortsComment = string.IsNullOrEmpty(SKUResortsComment) ?
                            resortIn1CReceiptItems.Value.comment :
                            SKUResortsComment += resortIn1CReceiptItems.Value.comment;
                }

                // Переcорты расходов (проведено в 1С)
                var resortIn1CItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.ResortIn1C);
                if (resortIn1CItems.HasValue && resortIn1CItems.Value.sum > 0)
                {
                    SKUReceiptGoods -= resortIn1CItems.Value.sum;
                    SKUShipped -= resortIn1CItems.Value.sum;
                    SKUResortsComment = resortIn1CItems.Value.comment;
                }

                // Переcорты расходов (концептуальные, не проведено в 1С) 
                var conceptualItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.Conceptual);
                if (conceptualItems.HasValue && conceptualItems.Value.sum > 0)
                {
                    // SKUReceiptGoods -=  conceptualItems.Value.sum;
                    // SKUShipped -= conceptualItems.Value.sum;
                    SKUResort += conceptualItems.Value.sum;
                    SKUResortsComment = conceptualItems.Value.comment;
                }
            }

            // Фикация входящих пересортов 
            if (resortFromSkusValues is not null && resortFromSkusValues.Count > 0)
            {
                IEnumerable<(int sku, int sum)>? GetResortValuesBy(SKUResortBalanceTarget balanceTarget)  => 
                    from resortFromSkusValue in resortFromSkusValues
                    from targetWithSum in  resortFromSkusValue.Value
                    where targetWithSum.Key == balanceTarget
                    let skuAndSum = (sku: resortFromSkusValue.Key, sum: targetWithSum.Value)
                    select skuAndSum;
                
                // Входящие пересорты: пересорты приходов (1С)
                var skuAndSumsByResortIn1CReceipt = GetResortValuesBy(SKUResortBalanceTarget.ResortIn1CReceipt);
                var skuAndSumsByResortIn1CReceiptSum = skuAndSumsByResortIn1CReceipt?.Aggregate(0, (acc, i) => acc + i.sum);
                if(skuAndSumsByResortIn1CReceipt is not null && skuAndSumsByResortIn1CReceipt.Any() && skuAndSumsByResortIn1CReceiptSum > 0) 
                {
                    // Данные здесь не меняются, так как есть проводка в 1С

                    StringBuilder resortIn1CReceiptSB = new("\nПересортица прихода от др.арт. (1С, см \"Склад: Приход товаром\"): \n");
                    foreach ((int sk, int su) in skuAndSumsByResortIn1CReceipt)
                    {
                        resortIn1CReceiptSB.Append($"{sk} шт.:{su}, ");
                    }
                    
                    SKUResortsComment = resortIn1CReceiptSB.ToString();
                }

                // Входящие пересорты: пересорты расходов (1С)
                var skuAndSumsByResortIn1C = GetResortValuesBy(SKUResortBalanceTarget.ResortIn1C);
                var skuAndSumsByResortIn1CSum = skuAndSumsByResortIn1C?.Aggregate(0, (acc, i) => acc + i.sum);
                if(skuAndSumsByResortIn1C is not null && skuAndSumsByResortIn1C.Any())  
                {
                    SKUResort += (int)skuAndSumsByResortIn1CSum!;   // Добавляется в Склад: Расход пересортом

                    // Обновление комментария
                    StringBuilder goodsResortsSb = new("\nПересортица расхода от др. арт. (1С, добавлено в \"Склад: Расход пересортом\",\n");
                    foreach (var (sk, su) in skuAndSumsByResortIn1C)
                    {
                        goodsResortsSb.Append($"{sk} шт.:{su}, ");
                    }

                    SKUResortsComment = goodsResortsSb.ToString();
                }

                // Входящие пересорты: пересорты расходов (концептуальные)
                var skuAndSumsByResortConceprual = GetResortValuesBy(SKUResortBalanceTarget.Conceptual);
                var skuAndSumsByResortConceprualSum = skuAndSumsByResortConceprual?.Aggregate(0, (acc, i) => acc + i.sum);
                if(skuAndSumsByResortConceprual is not null && skuAndSumsByResortConceprual.Any())  
                {
                    //SKUResort += (int)skuAndSumsByResortConceprualSum!;   // Добавляется в Склад: Расход пересортом
                    SKUReceiptGoods -= (int)skuAndSumsByResortConceprualSum!;
                    SKUShipped -= (int)skuAndSumsByResortConceprualSum!;    // Вычитается из Склад: Расход товаром

                    // Обновление комментария
                    StringBuilder goodsResortsSb = new("\nПересортица расхода от др. арт. (баланс, вычтено из \"Склад: Расход товаром\",\n");
                    foreach (var (sk, su) in skuAndSumsByResortConceprual)
                    {
                        goodsResortsSb.Append($"{sk} шт.:{su}, ");
                    }

                    SKUResortsComment = goodsResortsSb.ToString();
                }
            }
        }

        private void SetStatus()
        {
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
        }
        
        private void SetSellmentStatus(SKUSellmentStatus skuSellmentStatus) 
        {
            SellmentStatus = new SellmentStatus(skuSellmentStatus);
            FillColor = SellmentStatus.StatusColor;
        }
        
        
        #endregion 
    }
}
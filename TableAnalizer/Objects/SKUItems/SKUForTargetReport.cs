using System.Diagnostics;
using System.Drawing;
using System.Text;
using TableAnalizer.Interfaces;
using TableAnalizer.Objects.Enums;
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

        /// <summary>
        /// Положительная разница между приходом и ожиданием 
        /// </summary>
        private int SKUIncreasedReceiptGoods => 
            SKUReceiptGoods > SKURansomsWaiting ? 
            SKUReceiptGoods - SKURansomsWaiting : 
            0;
 
        public int SKUWriteOff { get; set; }                // Склад: списание недостач

        /// <summary>
        /// Склад: общий расход (товаром, компенсацией, пересортом) 
        /// </summary>
        public int SKUTotalСonsumption => SKUShipped + SKUResort + SKUPaid;
        /// <summary>
        /// Склад: Расход товаром
        /// </summary>
        public int SKUShipped { get; set; }     
        /// <summary>
        /// Склад: Расход компенсацией
        /// </summary>                       
        public int SKUPaid { get; set; }           
        /// <summary>
        /// Склад: расход пересортом                
        /// </summary>
        public int SKUResort { get; set; }    

        /// <summary>
        /// Склад: Списано пересортицей, товаров
        /// </summary>
        public int SKUWriteOffByResorts { get; set; }       

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

        /// <summary>
        /// Комментарий к пересортами
        /// </summary>
        public string SKUResortsComment { get; set; } = string.Empty;

        // Overruns 
        public int SKUOverrunsCount { get; set; } = 0;          // перерасходы, товаров
        public double SKUOverrunsSum { get; set; } = 0.00;      // перерасходы, сумма

        // Is Parrales sku
        private bool IsParallelSku {get ;set;}

        private string skuComment = string.Empty;
        public string SKUComment 
         { 
            get => skuComment;
            set => skuComment = string.IsNullOrEmpty(skuComment) ? skuComment = value : skuComment += $"\n{value}" ;
        }

        public Color FillColor { get; set;}

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
            
            if(isRawReport)
                GetInfoForWarehouse(isRawReport);

            // Калькуляция пересоров 
            if (!isRawReport)
            {
                CalcResorts(sku, resortFromSkusValues);
            }

            // Расчет задержек довоза и перерасходов
            var _ = CalculateDelayAndOverruns();

            if(!isRawReport)
                GetInfoForWarehouse(isRawReport);

        }

        private void GetInfoForWarehouse(bool isRawReport)
        {
            /// Приход больше ожидания: инфо для склада в комментарий 
            if (SKUIncreasedReceiptGoods > 0)
            {
                SKUComment = $"Приход товарами больше ожидания на: {Math.Abs(SKUReceiptGoods - SKURansomsWaiting)}";
            }

            // Отрицательный остаток товаров на складе: инфо для склада в комментарий и остаток = 0
            if (!isRawReport && int.IsNegative(SKURemaining))
            {
                SKUComment = $"Передано клиенту с пересортами: {Math.Abs(SKURemaining)}";
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
                    SKUReceiptGoods += resortIn1CReceiptItems.Value.sum;
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
                if(skuAndSumsByResortIn1CReceipt is not null && skuAndSumsByResortIn1CReceipt.Any() && skuAndSumsByResortIn1CReceiptSum > 0) 
                {
                    StringBuilder resortIn1CReceiptSB = new("Пересортица прихода на другие артикулы (вычтено из \"Склад: Приход товаром\"): \n");
                    SKUReceiptGoods -= (int)skuAndSumsByResortIn1CReceiptSum;
                    
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

            SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceiptGoods;
            SKUDeliveryDelayStorage = SKURansomsWaiting - SKUTotalСonsumption - SKUWriteOffByResorts - SKUDeliveryDelayPoints;

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
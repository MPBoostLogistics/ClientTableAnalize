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
        private int SKUIncreasedReceiptGoods => 
            SKUReceiptGoods > SKURansomsWaiting ? 
            SKUReceiptGoods - SKURansomsWaiting : 
            0;

        /// <summary>
        /// Склад: Компенсация (точки)
        /// </summary>                       
        private int skuPaidWarehouse = 0;
        /// <summary>
        /// Склад: Компенсация (склад)
        /// </summary>                       
        private int skuPaidPoints = 0; 

        /// <summary>
        /// Склад: общий расход (товаром, компенсацией, пересортом) 
        /// </summary>
        public int SKUTotalСonsumption => SKUShipped + SKUResort + SKUPaid;
        /// <summary>
        /// Склад: Расход товаром
        /// </summary>
        public int SKUShipped { get; set; }     
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

        /// <summary>
        /// Комментарий к пересортами
        /// </summary>
        public string SKUResortsComment { get; set; } = string.Empty;

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

        private string skuComment = string.Empty;
        public string SKUComment 
         { 
            get => skuComment;
            set => skuComment = string.IsNullOrEmpty(skuComment) ? skuComment = value : skuComment += $"\n{value}" ;
        }

        public Color FillColor { get; set;}

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
        private void SetSellmentStatus(SKUSellmentStatus skuSellmentStatus) 
        {
            SellmentStatus = new SellmentStatus(skuSellmentStatus);
            FillColor = SellmentStatus.StatusColor;
        }

        private void AnalizeSKU(SKU sku, bool isRawReport, bool isMulty, int traceDelay, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // Установка неопределенного статуса 
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Проверка компенсаций
            SetCompensation();

            // Расчет недовозов от точек
            CalculateDalay(SKUSellmentStatus.OnSearchPoints);

            // Калькуляция пересоров 
            if (!isRawReport)
            {
                CalcResorts(sku, resortFromSkusValues);
            }

            // Расчет недовозов от склада
            CalculateDalay(SKUSellmentStatus.OnSearchWarehouse);

            // Расчет перерасходов
            if(SKUShipped > SKURansomsWaiting) 
            {
                SKUOverrunsCount = SKUShipped - SKURansomsWaiting - SKUResort;
            }

            // Установка статуса взаморасчетов 
            SetStatus();

            // Информация для пересортов (склад)
            // - если это не сводный расчет и сырой отчет
            if(!isMulty && isRawReport) 
            {
                GetInfoForResort();
            }
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

            SKUComment = paidSB.ToString();
        }

        private void CalculateDalay(SKUSellmentStatus sellmentStatus) 
        {
            switch(sellmentStatus) 
            {
                case SKUSellmentStatus.OnSearchPoints:
                    SKUDeliveryDelayPoints = SKURansomsWaiting - SKUReceiptGoods;
                    break;
                case SKUSellmentStatus.OnSearchWarehouse:
                    SKUDeliveryDelayStorage = SKURansomsWaiting - SKUDeliveryDelayPoints - SKUTotalСonsumption;
                    break;
                default:
                    break;
            }
        }

        private void GetInfoForResort()
        {
            /// Приход больше ожидания: инфо для склада в комментарий 
            if (SKUIncreasedReceiptGoods > 0)
            {
                SKUComment = $"Приход товарами больше ожидания на: {Math.Abs(SKUReceiptGoods - SKURansomsWaiting)}";
            }

            // Отрицательный остаток товаров на складе: инфо для склада в комментарий и остаток = 0
            if (int.IsNegative(SKURemaining))
            {
                SKUComment = $"Передано клиенту с пересортами: {Math.Abs(SKURemaining)}";
                SKURemaining = 0;
            }
        }

        private void CalcResorts(SKU sku, Dictionary<int, Dictionary<SKUResortBalanceTarget, int>>? resortFromSkusValues)
        {
            // Фикация исходящих пересортов 
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

                // Переcорты расхода, списанные в 1С
                var resortIn1CItems = sku.SKUResortBalance.FindSkusInBalanceBy(SKUResortBalanceTarget.ResortIn1C);
                if (resortIn1CItems.HasValue && resortIn1CItems.Value.sum > 0)
                {
                    SKUReceiptGoods -= resortIn1CItems.Value.sum;
                    SKUShipped -= resortIn1CItems.Value.sum;
                    //SKUWriteOffByResorts += resortIn1CItems.Value.sum;
                    SKUResortsComment = resortIn1CItems.Value.comment;
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

            // Фикация входящих пересортов 
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
                    StringBuilder resortIn1CReceiptSB = new("Пересортица прихода (1С, входящий, см \"Склад: Приход товаром\"): \n");
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
                    //SKUReceiptGoods -= SKUResort;                   // Вычитается из Склад: Приход товаром
                    //SKUReceiptResorts += SKUResort;                 // Добавляется в Склад: Оприходовано пересортицей
                    //SKUShipped -= SKUResort;                        // Вычитается из Склад: Расход товаром

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
        #endregion 
    }
}
using System.Diagnostics;

namespace TableAnalizer.Objects.SKUItems
{
    public class SKUForTargetReports
    {
        #region Fields
        private const string RESORT_CONSUMPTION_FIRST_LINE = "Пересортица расходов:";
        private const string RESORT_RECEIPT_FIRST_LINE = "Пересортица приходов:";
        #endregion

        #region Properties
        #region Properties - Common
        public string SKULogins { get; set; }
        public Marketplace? SKUmp { get; set; }
        public int SKUNumber { get; set; }
        public string SKUBrand { get; set; }
        public string SKUName { get; set; }
        public double SKUPrice { get; set; }
        public DateOnly SKUDate { get; set; }
        private readonly bool isDeadline = false;
        public SellmentStatus SellmentStatus { get; private set; } = new SellmentStatus(SKUSellmentStatus.Unknown);
        public string SellmentStatusString => SellmentStatus.StatusLabel;

        public System.Drawing.Color FillColor { get; set; }

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
        public int SKUReceiptTotal => SKUReceiptGoods + SKUWrittenOffFromPoints;

        /// <summary>
        /// из них, Склад: Приход товарами
        /// </summary>
        public int SKUReceiptGoods { get; set; } = 0;

        /// <summary>
        /// Списано с точек 
        /// </summary>
        public int SKUWrittenOffFromPoints { get; set; } = 0;
        /// <summary>
        /// Списано с точек через склад 
        /// </summary>
        public int SKUWrittenOffFromPointsViaWarehouse { get; set; } = 0;
        
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

        private string skuReceipComment = string.Empty;

        /// <summary>
        /// Комментарий к приходам
        /// </summary>
        public string SKUReceipComment
        {
            get => skuReceipComment;
            set => skuReceipComment = string.IsNullOrEmpty(skuReceipComment) ? skuReceipComment = value : skuReceipComment += $"\n{value}";
        } 

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
        /// Общий взаиморасчет с клиентом (товарами, компенсацией)
        /// </summary>
        public int SKUSummarySettlement => SKUSettlementByGoods + SKUSettlementByCompensation;

        /// <summary>
        /// Взаиморачет товарами (отгрузки, перемещения, пересорты)
        /// </summary>
        public int SKUSettlementByGoods => SKUOutgoingProducts + SKUTransfered + SKUResort;

        /// <summary>
        /// Взаиморасчет компенсацией (от склада и организации )
        /// </summary>
        public int SKUSettlementByCompensation => SKUPaidWarehouse + SKUPaidOrganization;

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
        /// Организация: Денежная компенсация
        /// </summary>                       
        public int SKUPaidOrganization { get; set; } = 0;

        /// <summary>
        /// Склад: расход пересортом                
        /// </summary>
        public int SKUResort { get; set; }

        /// <summary>
        /// Общий взаиморасчет меньше ожидания клиента
        /// </summary>
        public int LessMutualSettlementThanWaiting => 
            SKUSummarySettlement < SKURansomsWaiting ? 
                SKURemaining > 0 ? 
                SKURansomsWaiting - SKUSummarySettlement - SKURemaining >= 0 ?
                    SKURansomsWaiting - SKUSummarySettlement - SKURemaining :
                    0 : 
                SKURansomsWaiting - SKUSummarySettlement > 0 ? 
                SKURansomsWaiting - SKUSummarySettlement :
                0 : 
            0;

        /// <summary>
        /// Общий взаиморасчет больше ожидания клиента
        /// </summary>
        public int MoreMutualSettlementThanWaiting =>
            SKUSummarySettlement > SKURansomsWaiting ?
            SKUSummarySettlement - SKURansomsWaiting :
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
        #region Properties - Analitycs
        /// <summary>
        /// Артикул нормализован
        /// </summary>
        /// <remarks>
        /// <para>Ожидание == Приход == Взаиморасчет и Остаток на складе == 0</para> 
        /// <para>Ожидание == Приход и Взаиморасчет + Остаток на складе == Ожидание</para> 
        /// </remarks>
        
        private bool IsParallelClientsSKU { get; } = false; 
        public bool IsSkuNormalize => 
            SKURansomsWaiting == SKUReceiptTotal && SKUReceiptTotal == SKUSummarySettlement && SKURemaining == 0 || 
            SKURansomsWaiting == SKUReceiptTotal && SKUSummarySettlement + SKURemaining == SKURansomsWaiting;

        /// <summary>
        /// Долг по оплате компенсации организацией перед клиентом.
        /// </summary>
        public int SKUDebtToClientForCompensationSum => 
            SKUWrittenOffFromPoints - SKUSettlementByCompensation > 0 ? 
            SKUWrittenOffFromPoints - SKUSettlementByCompensation : 
            0;

        #endregion

        #endregion

        #region Constructors
        public SKUForTargetReports(SKU sku)
        {
            SKULogins = string.Join(", ", sku.ClientLogins);
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
                SKURansomsOnWay = sku.SKURansomsBalance.SKUInTransitNumberPurchases;
                SKURansomsCanceled = sku.SKURansomsBalance.SKUCancelledNumberPurchases;
                SKURansomsDelivered = sku.SKURansomsBalance.SKUDeliveredToPointsNumberPurchases;
                SKURansomsWithDrawn = sku.SKURansomsBalance.SKUPickedupFromPointsNumberPurchases;
                SKURansomsWaiting = sku.SKURansomsBalance.SKUWaitingNumberPurchases;
            }

            // Fill SKURemainingBalance
            if (sku.SKURemainingBalance is not null)
            {
                SKUReceiptGoods = sku.SKURemainingBalance.SKUReceipt;
                SKUOutgoingProducts = sku.SKURemainingBalance.SKUOutgoingProducts;
                SKUTransfered = sku.SKURemainingBalance.SKUTransfered;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            }

            // Fill SKUWriteOff units
            if (sku.SKUWriteOffBalance is not null) 
            {
                SKUWrittenOffFromPoints = sku.SKUWriteOffBalance!.SKUWriteOffPoints;
                SKUWrittenOffFromPointsViaWarehouse = sku.SKUWriteOffBalance!.SKUWriteOffPointsViaWarehouse;

                if(SKUWrittenOffFromPointsViaWarehouse > 0) 
                {
                    SKUReceiptGoods -= SKUWrittenOffFromPointsViaWarehouse;

                    SKUReceipComment = $"Списано с точек через склад\n(вычтено из прихода):\n{SKUWrittenOffFromPointsViaWarehouse}";
                }
            }

            // Fill SKUCompensationBalance 
            if (sku.SKUCompensationBalance is not null)
            {
                SKUPaidWarehouse += sku.SKUCompensationBalance.CompensationItems
                    .Where(el => el.Key == CompensationTarget.Warehouse)
                    .Aggregate(0, (acc, i) => acc + i.Value.count);

                SKUPaidOrganization += sku.SKUCompensationBalance.CompensationItems
                    .Where(el => el.Key == CompensationTarget.PickupPoint)
                    .Aggregate(0, (acc, i) => acc + i.Value.count);
            }
        
            // Is parallel clients SKU
            IsParallelClientsSKU = sku.IsParallelClientsSKU;
        }

        #endregion

        #region Functionality
        public void FinalAnalize()
        {
            // Setting status to undefined
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Calculation of shortages from points 
            CalculateDelay(SKUSellmentStatus.DelayDeliveryFromPoints);
      
            // Calculation of shortages from the warehouse
            CalculateDelay(SKUSellmentStatus.DelayDeliveryFromWarehouse);

            // Calculation of overruns
            var totalСonsumptionWithoutResorts = SKUSummarySettlement - SKUResort;
            if (totalСonsumptionWithoutResorts > SKURansomsWaiting && !IsParallelClientsSKU)
            {
                SKUOverrunsCount = totalСonsumptionWithoutResorts - SKURansomsWaiting;
            }

            // Setting the status 
            SetStatus();
        }

        public void MakeResort(SKUResortTarget target, SKUResortDirection dir, int skuNumber, int sum) 
        {
            switch (target)
            {
                case SKUResortTarget.ResortingConsumptionByOutgoing: case SKUResortTarget.ResortingConsumptionByTransfer:
                    if(!SKUResortsComment.Contains(RESORT_CONSUMPTION_FIRST_LINE)) 
                    {
                        SKUResortsComment = RESORT_CONSUMPTION_FIRST_LINE;
                    }
                    
                    break;
                case SKUResortTarget.ResortingReceipt:
                    if(!SKUResortsComment.Contains(RESORT_RECEIPT_FIRST_LINE)) 
                    {
                        SKUResortsComment = RESORT_RECEIPT_FIRST_LINE;
                    }
                    break;
            }
            

            switch (target)
            {
                case SKUResortTarget.ResortingConsumptionByTransfer:
                    switch (dir)
                    {
                        case SKUResortDirection.Incoming:
                            SKUResort += sum;
                            SKUResortsComment = $"- от арт. {skuNumber}: {sum} (фф)";
                            break;
                        case SKUResortDirection.Outgoing:
                            SKUTransfered -= sum;
                            SKUResortsComment = $"- на арт. {skuNumber}: {sum} (фф)";
                            break;
                    }
                    break;

                case SKUResortTarget.ResortingConsumptionByOutgoing:
                    switch (dir)
                    {
                        case SKUResortDirection.Incoming:
                            SKUResort += sum;
                            SKUResortsComment = $"- от арт. {skuNumber}: {sum} (р)";
                            break;
                        case SKUResortDirection.Outgoing:
                            SKUOutgoingProducts -= sum;
                            SKUResortsComment = $"- на арт. {skuNumber}: {sum} (р)";
                            break;
                    }
                    break;
                
                case SKUResortTarget.ResortingReceipt:
                switch (dir) 
                {
                    case SKUResortDirection.Incoming:
                        SKUReceiptGoods += sum;
                        SKUResortsComment = $"- от арт. {skuNumber}: {sum} (п)";
                        break;
                    case SKUResortDirection.Outgoing:
                        SKUReceiptGoods -= sum;
                        SKUResortsComment = $"- на арт. {skuNumber}: {sum} (п)";
                        break;
                    case SKUResortDirection.Internal:
                    
                        if(SKUReceiptGoods >= sum) 
                        {
                            SKUReceiptGoods -= sum;
                            SKUResortsComment = $"- приход списано: {sum} (эп)";
                        } 

                        // Final balance receipt
                        if(SKUReceiptTotal > SKURansomsWaiting) 
                        {
                            SKUReceiptGoods -= SKUReceiptTotal - SKURansomsWaiting;
                        }

                        break;
                    default:
                        break;
                }
                break;
            }
        }

        private void CalculateDelay(SKUSellmentStatus sellmentStatus)
        {
            switch (sellmentStatus)
            {
                case SKUSellmentStatus.DelayDeliveryFromPoints:
                    SKUDeliveryDelayPoints = SKURansomsWaiting > SKUReceiptTotal ? 
                    SKURansomsWaiting - SKUReceiptTotal :
                    0;
                    break;
                case SKUSellmentStatus.DelayDeliveryFromWarehouse:
                    SKUDeliveryDelayStorage = (SKURansomsWaiting - SKUSummarySettlement - SKUDeliveryDelayPoints) > 0 ? 
                    SKURansomsWaiting - SKUDeliveryDelayPoints - SKUSummarySettlement : 
                    0;
                    break;
                default:
                    break;
            }
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
                switch (IsSkuNormalize) 
                {
                    case true:
                        switch(SKURemaining > 0) 
                        {
                            case true:
                                SetSellmentStatus(SKUSellmentStatus.DelayDeliveryFromWarehouse);
                                break;
                            case false:
                                 SetSellmentStatus(SKUSellmentStatus.FullSet);
                                break;
                        }
                        break;
                    case false:
                        switch (SKUDeliveryDelayPoints > 0, SKUDeliveryDelayStorage > 0)
                        {
                            case (true, true):
                                SetSellmentStatus(SKUSellmentStatus.DelayDeliveryFromPointsAndWarehouse);
                                break;
                            case (true, false):
                                SetSellmentStatus(SKUSellmentStatus.DelayDeliveryFromPoints);
                                break;
                            case (false, true):
                                SetSellmentStatus(SKUSellmentStatus.DelayDeliveryFromWarehouse);
                                break;
                            case (false, false):
                                SetSellmentStatus(SKUSellmentStatus.FullSet);
                                break;
                        }
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
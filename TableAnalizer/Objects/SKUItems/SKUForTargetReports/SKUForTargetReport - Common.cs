
namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports
{
    public partial class SKUForTargetReport
    {
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
        public int SKUReceiptTotal => SKUReceiptGoods + SKUWriteOffFromPoints;

        /// <summary>
        /// из них, Склад: Приход товарами
        /// </summary>
        public int SKUReceiptGoods { get; set; } = 0;

        #endregion
        #region Properties - Comments
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
        #region Properties - Mutual settlement

        /// <summary>
        /// Общий взаиморасчет с клиентом (товарами, компенсацией)
        /// </summary>
        public int SKUSummarySettlement => SKUSettlementByGoods + SKUCompensatedTotalToClient;

        /// <summary>
        /// Взаиморачет товарами (отгрузки, перемещения, пересорты)
        /// </summary>
        public int SKUSettlementByGoods => SKUOutgoingProducts + SKUTransfered + SKUResort;

        /// <summary>
        /// расход товарами
        /// </summary>
        public int SKUOutgoingProducts { get; set; }

        /// <summary>
        /// Склад: Передано товаром (фулфилмент)
        /// </summary>
        public int SKUTransfered { get; set; }

        #endregion
        #region Properties - Remaining
        /// <summary>
        /// Склад: Остаток товаров
        /// </summary>
        public int SKURemaining { get; set; }
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
        public SKUForTargetReport(SKU sku)
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

            // Fill sku WriteOffs
            SetSKUWriteOffs(sku.SKUWriteOffBalance);

            // Fill SKUCompensationBalance 
            SetSKUCompensateds(sku.SKUCompensationBalance);
            if (sku.SKUCompensationBalance is not null)
        
            // Set is parallel clients SKU
            IsParallelClientsSKU = sku.IsParallelClientsSKU;
        }

        #endregion

        #region Functionality
        public int FinalAnalize(int paidWriteOffRemainder)
        {
            // Setting status to undefined
            SetSellmentStatus(SKUSellmentStatus.Unknown);

            // Calculate compensation from organization
            int paidWriteOffReminderResult;
            if(paidWriteOffRemainder > 0 && SKUDeliveryDelayOrganization == 0) 
            {
                 CalculateDelay(out paidWriteOffReminderResult, in paidWriteOffRemainder);
            } 
            else 
            {
                paidWriteOffReminderResult = paidWriteOffRemainder;
            }
               
            // Calculation of overruns
            var totalСonsumptionWithoutResorts = SKUSummarySettlement - SKUResort;
            if (totalСonsumptionWithoutResorts > SKURansomsWaiting && !IsParallelClientsSKU)
            {
                SKUOverrunsCount = totalСonsumptionWithoutResorts - SKURansomsWaiting;
            }

            // Calculation of shortages from points 
            CalculateDelay(SKUSellmentStatus.DelayDeliveryFromPoints);
      
            // Calculation of shortages from the warehouse
            CalculateDelay(SKUSellmentStatus.DelayFromWarehouse);


            // Setting the status 
            SetStatus();

            return paidWriteOffReminderResult;
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
                                SetSellmentStatus(SKUSellmentStatus.DelayFromWarehouse);
                                break;
                            case false:
                                 SetSellmentStatus(SKUSellmentStatus.FullSet);
                                break;
                        }
                        break;
                    case false:
                        switch (SKUDeliveryDelayOrganization > 0, SKUDeliveryDelayWarehouse > 0, SKUDeliveryDelayPoints > 0)
                        {
                            case (true, false, false):
                                SetSellmentStatus(SKUSellmentStatus.DelayFromOrganization);
                                break;
                            case (true, true, false):
                                SetSellmentStatus(SKUSellmentStatus.DelayFromOrganizationAndWarehouse);
                                break;
                            case (false, true, false):
                                SetSellmentStatus(SKUSellmentStatus.DelayFromWarehouse);
                                break;
                            case (false, true, true):
                                SetSellmentStatus(SKUSellmentStatus.DelayFromPointsAndWarehouse);
                                break;
                            case (false, false, true):
                                SetSellmentStatus(SKUSellmentStatus.DelayDeliveryFromPoints);
                                break;
                            case (false, false, false):
                                SetSellmentStatus(SKUSellmentStatus.FullSet);
                                break;
                            default:
                                SetSellmentStatus(SKUSellmentStatus.Unknown);
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
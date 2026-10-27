
using System.Text;

namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports
{
    public partial class SKUForFinalReport
    {
        #region Properties

        #region Properties - Common
        public string SKULogins { get; set; }
        public Marketplace? SKUmp { get; set; }
        public long SKUNumber { get; set; }
        public string SKUBrand { get; set; }
        public string SKUName { get; set; }
        public double SKUPrice { get; set; }
        public DateOnly SKUDate { get; set; }
        public readonly bool isDeadline = false;
        public SellmentStatus SellmentStatus { get; private set; } = new SellmentStatus(SKUSellmentStatus.Unknown);
        public string SellmentStatusString => SellmentStatus.StatusLabel;

        public System.Drawing.Color FillColor { get; set; }

        #endregion
        #region Properties - Ransoms
        public int SKURansomsTotal => SKURansomsCanceled + SKURansomsOnWay + SKURansomsDelivered + SKURansomsWithDrawn;
        public int SKURansomsCanceled { get; set; } // Суммируется при объединении алиасов
        public int SKURansomsOnWay { get; set; } // Суммируется при объединении алиасов
        public int SKURansomsDelivered { get; set; } // Суммируется при объединении алиасов
        public int SKURansomsWithDrawn { get; set; } // Суммируется при объединении алиасов

        #endregion
        #region Properties - Waiting && Receipt
        /// <summary>
        /// Склад: Ожидание
        /// </summary>
        public int SKURansomsWaiting { get; set; }

        /// <summary>
        /// Приход от точек, всего
        /// </summary>
        public int SKUReceiptPointsTotal => SKUReceiptPointsGoods + SKUWriteOffFromPointsAndCouriers;

        /// <summary>
        /// из них, Склад: Приход товарами
        /// </summary>
        public int SKUReceiptPointsGoods { get; set; } = 0; // Суммируется при объединении алиасов

        /// <summary>
        /// Оприходовано по ордерам пересорта.
        /// </summary>
        public int SKUReceiptsPointsOnResortOrders { get; set; } = 0;

        /// <summary>
        /// Списано по ордерам пересорта.
        /// </summary>
        public int SKUWriteOffOnResortOrders { get; set; } = 0;

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

        private string skuShipmentComment = string.Empty;

        /// <summary>
        /// Комментарий к расходам
        /// </summary>
        public string SKUShipmentComment
        {
            get => skuShipmentComment;
            set => skuShipmentComment = string.IsNullOrEmpty(skuShipmentComment) ? skuShipmentComment = value : skuShipmentComment += $"\n{value}";
        }

        private string skuAliasesComment = string.Empty;

        /// <summary>
        /// Комментарий к приходам
        /// </summary>
        public string SKUAliasesComment
        {
            get => skuAliasesComment;
            set => skuAliasesComment = string.IsNullOrEmpty(skuAliasesComment) ? skuAliasesComment = value : skuAliasesComment += $"\n{value}";
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
        public int SKUSettlementByGoods => SKUShipmentGoods + SKUTransfered + SKUResort;

        /// <summary>
        /// расход товарами
        /// </summary>
        public int SKUShipmentGoods { get; set; } // Суммируется при объединении алиасов

        /// <summary>
        /// Склад: Передано товаром (фулфилмент)
        /// </summary>
        public int SKUTransfered { get; set; } // Суммируется при объединении алиасов

        #endregion
        #region Properties - Remaining

        private int skuRemaining;

        /// <summary>
        /// Склад: Остаток товаров
        /// Суммируется при объединении алиасов
        /// </summary>
        public int SKURemaining
        {
            get => skuRemaining;
            set => skuRemaining = value;
        }
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
        public SKUForFinalReport(SKU sku, int? adjustmentShipmentSum)
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
                // Списано из прихода по ордерам пересорта
                SKUWriteOffOnResortOrders = sku.SKURemainingBalance.SKUWriteOffOnResortOrders;

                // Приход товарами и ордерами пересортов (минус расход ордерами пересорьтов)
                SKUReceiptPointsGoods = SKUWriteOffOnResortOrders > 0 ?
                    sku.SKURemainingBalance.SKUReceipt - SKUWriteOffOnResortOrders :
                    sku.SKURemainingBalance.SKUReceipt;

                // Расход товаров
                SKUShipmentGoods = sku.SKURemainingBalance.SKUShipmentGoods;

                // Корректировка расходов по данным от Клиента
                if (adjustmentShipmentSum != null && adjustmentShipmentSum != 0)
                {
                    var difference = (int)adjustmentShipmentSum;
                    var oldValue = SKUShipmentGoods;

                    StringBuilder sb = new("Корректировка по расходам от Клиента:\n");
                    string signString;

                    switch (difference > 0)
                    {
                        case true:
                            signString = "+";
                            SKUShipmentGoods += difference;
                            break;
                        case false:
                            signString = "-";
                            SKUShipmentGoods -= Math.Abs(difference);
                            break;
                    }

                    sb.Append($"{oldValue} {signString} {Math.Abs(difference)} = {SKUShipmentGoods}");
                    SKUShipmentComment += sb;
                }

                SKUTransfered = sku.SKURemainingBalance.SKUTransfered;
                SKURemaining = sku.SKURemainingBalance.SKURemaining;
            }

            // Fill sku WriteOffs
            SetSKUWriteOffs(sku.SKUWriteOffBalance);

            // Fill SKUCompensationBalance 
            SetSKUCompensateds(sku.SKUPaidShortageBalance);

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
            if (paidWriteOffRemainder > 0 && SKUDeliveryDelayOrganization == 0)
            {
                CalculateDelay(out paidWriteOffReminderResult, in paidWriteOffRemainder);
            }
            else
            {
                paidWriteOffReminderResult = paidWriteOffRemainder;
            }

            // Calculation of shortages from points 
            CalculateDelay(SKUSellmentStatus.DelayDeliveryFromPoints);

            // Calculation of shortages from the warehouse
            CalculateDelay(SKUSellmentStatus.DelayFromWarehouse);

            // Calculation of overruns
            CalculateOverruns();

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
                        switch (SKURemaining > 0)
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
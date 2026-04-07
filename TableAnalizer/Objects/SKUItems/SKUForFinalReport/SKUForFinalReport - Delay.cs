namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports
{
    /// <summary>
    /// Delay and overruns part of "SKUForTargetReport" type.
    /// </summary>
    public partial class SKUForFinalReport
    {
        #region Properties - DeliveryDelay  
        /// <summary>
        /// Задержка довоза, всего
        /// </summary>
        public int SKUDeliveryDelayTotal => SKUDeliveryDelayOrganization + SKUDeliveryDelayPoints + SKUDeliveryDelayWarehouse;

        private int skuDeliveryDelayOrganization = 0;

        /// <summary>
        /// Задержка оплаты от организации, кол-во
        /// </summary>
        public int SKUDeliveryDelayOrganization
        {
            get => skuDeliveryDelayOrganization;
            set => skuDeliveryDelayOrganization = value > 0 ? value : 0;
        }

        /// <summary>
        /// Задержка оплаты от организации, сумма руб.
        /// </summary>
        public double SKUDeliveryDelayOrganizationSum =>
            SKUDeliveryDelayOrganization > 0 && isDeadline ?
            SKUDeliveryDelayOrganization * SKUPrice :
            0.00;

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
        public int SKUDeliveryDelayWarehouse
        {
            get => skuDeliveryDelayStorage;
            set => skuDeliveryDelayStorage = value > 0 ? value : 0;
        }
        /// <summary>
        /// Задержка довоза от склада, сумма руб.
        /// </summary>
        public double SKUDeliveryDelayWarehouseSum =>
            SKUDeliveryDelayWarehouse > 0 && isDeadline ?
            SKUDeliveryDelayWarehouse * SKUPrice :
            0.00;

        #endregion

        #region Functionality
        private void CalculateDelay(SKUSellmentStatus sellmentStatus)
        {
            switch (sellmentStatus)
            {
                case SKUSellmentStatus.DelayDeliveryFromPoints:
                    SKUDeliveryDelayPoints = SKURansomsWaiting > SKUSummarySettlement &&
                        SKURansomsWaiting < SKUReceiptTotal ?
                        SKURansomsWaiting - SKUReceiptTotal :
                        0;
                    break;
                case SKUSellmentStatus.DelayFromWarehouse:
                    SKUDeliveryDelayWarehouse = SKURansomsWaiting > SKUSummarySettlement &&
                        (SKURansomsWaiting - SKUSummarySettlement - SKUDeliveryDelayOrganization - SKUDeliveryDelayPoints) > 0 ?
                        SKURansomsWaiting - SKUDeliveryDelayPoints - SKUDeliveryDelayOrganization - SKUSummarySettlement :
                    0;
                    break;
                default:
                    break;
            }
        }

        private void CalculateOverruns()
        {
            var sellmentAmount = SKUSummarySettlement + SKURemaining;
            if (SKURansomsWaiting < sellmentAmount)
            {
                SKUOverrunsCount = SKURansomsWaiting - sellmentAmount;
            }
        }

        #endregion


    }
}
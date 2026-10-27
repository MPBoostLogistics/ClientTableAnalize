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
        public int SKUDeliveryDelayTotal =>
            SKUDeliveryDelayOrganization +  // задержка оплаты от организации
            SKUDeliveryDelayPoints +        // задержка довозов от точек
            SKUDeliveryDelayWarehouse;      // задердка довоза от склада

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
            // Общая задержка = Ожидание - Общий взаиморасчет
            var skuDelayTotal = SKURansomsWaiting - SKUSummarySettlement;
            if (int.IsNegative(skuDelayTotal))
                skuDelayTotal = 0;


            // Задержка от точек = Ожидание - Приход то точек - Компенсация от точек
            var skuDelayPoints = SKURansomsWaiting - SKUReceiptPointsTotal - SKUPaidShortageOrganization;
            if (skuDelayTotal == 0) // костыль
                skuDelayPoints = 0;

            // Задержка от склада = Общая задержка - Задержка от точек - компенсакция от склада
            var skuDelayWarehouse = skuDelayTotal - skuDelayPoints - SKUPaidShortageWarehouse;

            // костыль
            if (skuDelayTotal == 0)
            {
                skuDelayPoints = 0;
                skuDelayWarehouse = 0;
            }

            switch (skuDelayTotal == skuDelayPoints + skuDelayWarehouse)
            {
                // Если общая задержка равна задержка от склада плюс задержка от точек - пропускаем
                case true:
                    break;
                case false:
                    // Если общая задержка меньше чем задержка от точек + задержка от склада
                    if (skuDelayTotal < skuDelayPoints + skuDelayWarehouse)
                    {
                        skuDelayPoints = skuDelayPoints + skuDelayWarehouse - skuDelayTotal;
                    }
                    break;
            }



            // SKUCompensatedTotalToClient - общая компенсация
            // SKUPaidShortageWarehouse - компенсация от склада
            // SKUPaidShortageOrganization - компенсация от точек
            // SKUPaidShortageOrganizationAndWarehouse - компенсация от склада и точек


            switch (sellmentStatus)
            {
                case SKUSellmentStatus.DelayDeliveryFromPoints:
                    SKUDeliveryDelayPoints = skuDelayPoints;
                    break;
                case SKUSellmentStatus.DelayFromWarehouse:
                    SKUDeliveryDelayWarehouse = skuDelayWarehouse;
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

        private (int point, int warehouse) CalculateDelay()
        {
            int point = default; int warehouse = default;

            // Если приход от точек (товарами и пересортами) больше ожидания, то уменьшаем приход товарами на эту разницу
            if (SKUReceiptPointsGoods + SKUWriteOffFromPointsAndCouriers > SKURansomsWaiting)
            {
                SKUReceiptPointsGoods -= SKURansomsWaiting - (SKUReceiptPointsGoods + SKUWriteOffFromPointsAndCouriers);
            }

            // Если общий взаиморасчет больше или равен ожиданию, недовоз от точек и склада 0 
            if (SKUSummarySettlement >= SKURansomsWaiting)
            {
                point = 0;
                warehouse = 0;
            }
            // Если общий взаиморасчет меньше ожидания
            else
            {
                // Недовоз от точек = Ожидание - Общий взаиморасчет - остаток товаров на складе
                point = SKURansomsWaiting - SKUSummarySettlement - SKURemaining;
                if (int.IsNegative(point))
                    point = 0;

                // Недовоз от склада = Ожидание - Недовоз от точек
                warehouse = SKURansomsWaiting - point;
            }

            return (point, warehouse);
        }

        #endregion


    }
}
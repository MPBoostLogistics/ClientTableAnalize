using System.Diagnostics;
using TableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports
{
    /// <summary>
    /// Сompensation / shortages part of "SKUForTargetReport" type.
    /// </summary>
    public partial class SKUForTargetReport
    {
        #region Properties
        /// <summary>
        /// Списано с точек, товаров 
        /// </summary>
        /// <remarks>
        /// <para>Учитывается <b>отчет прямого списания</b> товаров с точек.</para> 
        /// </remarks>
        public int SKUWriteOffFromPoints { get; set; } = 0; // Суммируется при объединении алиасов
        
        /// <summary>
        /// Списаны недостачи точек через склад, товаров
        /// </summary>
        /// <remarks>
        /// <para>Учитывается <b>предыдущий алгоритм списания</b> товаров с точек через склад.</para> 
        /// </remarks>
        public int SKUWriteOffFromPointsViaWarehouseShortage { get; set; } = 0;
        
        /// <summary>
        /// Списаны корректировки точек через склад, товаров
        /// </summary>
        /// <remarks>
        /// <para>Учитывается <b>предыдущий алгоритм корректировки</b> остатков точек через склад.</para> 
        /// </remarks>
        public int SKUWrittenOffFromPointsViaWarehouseAdjustment { get; set; } = 0;

        /// <summary>
        /// Фактическая денежная компенсация от склада и организации (точек) в адрес клиента. 
        /// </summary>
        public int SKUCompensatedTotalToClient =>
            SKUPaidShortageWarehouse + SKUPaidShortageOrganization + SKUPaidShortageOrganizationAndWarehouse;

        /// <summary>
        /// Денежная компенсация от склада в адрес клиента
        /// </summary>                       
        public int SKUPaidShortageWarehouse { get; set; } = 0; // Суммируется при объединении алиасов

        /// <summary>
        /// Денежная компенсация от организации (точек) в адрес клиента
        /// </summary>                       
        public int SKUPaidShortageOrganization { get; set; } = 0; // Суммируется при объединении алиасов

        /// <summary>
        /// Денежная компенсация от организации (точек) и склада в адрес клиента
        /// </summary> 
        public int SKUPaidShortageOrganizationAndWarehouse { get; set; } = 0; // Суммируется при объединении алиасов
        #endregion

        #region Functionality
        private void SetSKUWriteOffs(SKUWriteOffBalance? writeOffBalance)
        {
            if (writeOffBalance is null) return;

            SKUWriteOffFromPoints = writeOffBalance.SKUWriteOffPoints;
            SKUWriteOffFromPointsViaWarehouseShortage = writeOffBalance.SKUWriteOffPointsViaWarehouseShortage;
            SKUWrittenOffFromPointsViaWarehouseAdjustment = writeOffBalance.SKUWriteOffPointsViaWarehouseAdjustment;
        }

        private void SetSKUCompensateds(SKUPaidShortageBalance? skuPaidShortageBalance)
        {
            if (skuPaidShortageBalance is null) return;

            SKUPaidShortageWarehouse += skuPaidShortageBalance.PaidShortageItems
                .Where(el => el.Key == ClaimExecutor.Warehouse)
                .Aggregate(0, (acc, i) => acc + i.Value.sum);

            SKUPaidShortageOrganization += skuPaidShortageBalance.PaidShortageItems
                .Where(el => el.Key == ClaimExecutor.Organization)
                .Aggregate(0, (acc, i) => acc + i.Value.sum);

            SKUPaidShortageOrganizationAndWarehouse += skuPaidShortageBalance.PaidShortageItems
                .Where(el => el.Key == ClaimExecutor.OrganizationAndWarehouse)
                .Aggregate(0, (acc, i) => acc + i.Value.sum);
        }

        /// <summary>
        /// Производит корректировку недостач, списанных с точек.
        /// </summary>
        /// <param name="writeOffSource">Источник списания.</param>
        /// <remarks>Производит сравнение экстра прихода и списания с точек через склад (предыдущие периоды).</remarks>
        /// <returns>Остаток после списания, ...</returns>
        public int MakeCorrectionOnWriteOffShortages(SourceReportItemKey writeOffSource)
        {
            int difference;
            switch (writeOffSource)
            {
                case SourceReportItemKey.WriteOffPointsViaWarehouseShortage:

                    if (SKUWriteOffFromPointsViaWarehouseShortage == 0) return 0;

                    difference = SKUWriteOffFromPointsViaWarehouseShortage;
                    SKUReceiptGoods -= difference;
                    SKUWriteOffFromPointsViaWarehouseShortage = 0;

                    SKUReceipComment = $"Приход уменьшен на списанное от точек: {difference}";

                    // Если взиморасчет меньше ожидания и оплаченная компенсация меньше полученной от точек
                    if (UnderMutualSettlement > 0 &&
                        SKUDeliveryDelayOrganization == 0 &&
                        SKUCompensatedTotalToClient < difference)
                    {
                        var dif = difference - SKUCompensatedTotalToClient;
                        CalculateDelay(out int paidWriteOffRemainder, in dif);
                        return paidWriteOffRemainder;
                    }
                    else
                    {
                        return 0;
                    }
                case SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment:

                    if (SKUWrittenOffFromPointsViaWarehouseAdjustment == 0) return 0;

                    difference = SKUWrittenOffFromPointsViaWarehouseAdjustment;
                    SKUReceiptGoods -= difference;
                    SKUWrittenOffFromPointsViaWarehouseAdjustment = 0;

                    SKUReceipComment = $"Приход уменьшен на коррект. от точек: {difference}";

                    return 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Вычисляет недовоз (задержку оплаты) от организации. 
        /// </summary>
        /// <param name="paidWriteOffRemainder">Остаток оплаченного.</param>
        /// <param name="compensation">Компенсировано.</param>
        /// <returns>Остаток от компенсации.</returns>
        private void CalculateDelay(out int paidWriteOffRemainder, in int compensation)
        {
            //Trace.WriteLine($"{SKUNumber}, Сompensation in: {compensation}");

            SKUDeliveryDelayOrganization = UnderMutualSettlement >= compensation ?
                compensation :
                UnderMutualSettlement;

            paidWriteOffRemainder = compensation - SKUDeliveryDelayOrganization;

            //Trace.WriteLine($"{SKUNumber}, SKUDeliveryDelayOrganization: {SKUDeliveryDelayOrganization}");
            //Trace.WriteLine($"{SKUNumber}, Сompensation out: {paidWriteOffRemainder}");
        }

        #endregion
    }
}
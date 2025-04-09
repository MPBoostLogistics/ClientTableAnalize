using System.Diagnostics;
using TableAnalizer.Objects.SKUItems.SKUBalances;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUCompensationBalance;

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
        public int SKUWriteOffFromPoints { get; set; } = 0;
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
            SKUCompensatedWarehouse +  SKUCompensatedOrganization;

        /// <summary>
        /// Денежная компенсация от склада в адрес клиента
        /// </summary>                       
        public int SKUCompensatedWarehouse { get; set; } = 0;

         /// <summary>
        /// Денежная компенсация от организации (точек) в адрес клиента
        /// </summary>                       
        public int SKUCompensatedOrganization { get; set; } = 0;
        #endregion
   
        #region Functionality
        private void SetSKUWriteOffs(SKUWriteOffBalance? writeOffBalance) 
        {
            if(writeOffBalance is null) return;

            SKUWriteOffFromPoints = writeOffBalance.SKUWriteOffPoints;
            SKUWriteOffFromPointsViaWarehouseShortage = writeOffBalance.SKUWriteOffPointsViaWarehouseShortage;
            SKUWrittenOffFromPointsViaWarehouseAdjustment = writeOffBalance.SKUWriteOffPointsViaWarehouseAdjustment;
        }
        private void SetSKUCompensateds(SKUCompensationBalance?  compensationBalance) 
        {
            if(compensationBalance is null) return;

            SKUCompensatedWarehouse += compensationBalance.CompensationItems
                .Where(el => el.Key == CompensationTarget.Warehouse)
                .Aggregate(0, (acc, i) => acc + i.Value.count);

            SKUCompensatedOrganization += compensationBalance.CompensationItems
                .Where(el => el.Key == CompensationTarget.PickupPoint)
                .Aggregate(0, (acc, i) => acc + i.Value.count);
        }
        
        /// <summary>
        /// Производит корректировку недостач, списанных с точек.
        /// </summary>
        /// <param name="writeOffSource">Источник списания.</param>
        /// <remarks>Производит сравнение экстра прихода и списания с точек через склад (предыдущие периоды).</remarks>
        /// <returns>Остаток после списания, ...</returns>
        public int MakeCorrectionOnWriteOffShortages(WriteOffSource writeOffSource) 
        {
            int difference;
            switch (writeOffSource)
            {
                case WriteOffSource.PointsViaWarehouseShortage:

                    if(SKUWriteOffFromPointsViaWarehouseShortage == 0) return 0;

                    difference = SKUWriteOffFromPointsViaWarehouseShortage;
                    SKUReceiptGoods -= difference;
                    SKUWriteOffFromPointsViaWarehouseShortage = 0;

                    SKUReceipComment = $"Приход уменьшен на списанное от точек: {difference}";

                    // Если взиморасчет меньше ожидания и оплаченная компенсация меньше полученной от точек
                    if(LessMutualSettlementThanWaiting > 0 && 
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
                case WriteOffSource.PointsViaWarehouseAdjustment:

                    if(SKUWrittenOffFromPointsViaWarehouseAdjustment == 0) return 0;

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
            Trace.WriteLine($"{SKUNumber}, Сompensation in: {compensation}");

            SKUDeliveryDelayOrganization = LessMutualSettlementThanWaiting >= compensation ? 
                compensation :
                LessMutualSettlementThanWaiting;

            paidWriteOffRemainder = compensation - SKUDeliveryDelayOrganization; 

            Trace.WriteLine($"{SKUNumber}, SKUDeliveryDelayOrganization: {SKUDeliveryDelayOrganization}");
            Trace.WriteLine($"{SKUNumber}, Сompensation out: {paidWriteOffRemainder}");
        }

        #endregion
    }
}
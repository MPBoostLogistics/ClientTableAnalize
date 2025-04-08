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
        public int SKUWrittenOffFromPointsViaWarehouseShortage { get; set; } = 0;
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
            SKUWrittenOffFromPointsViaWarehouseShortage = writeOffBalance.SKUWriteOffPointsViaWarehouseShortage;
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
        #endregion
   
    }
}
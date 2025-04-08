namespace TableAnalizer.Objects.SKUItems.SKUForTargetReports 
{
    /// <summary>
    /// Analytical part of the "SKUForTargetReport" type.
    /// </summary>
    public partial class SKUForTargetReport 
    {
        #region Properties
        /// <summary>
        /// Parallel Clients SKU.
        /// </summary>
        private bool IsParallelClientsSKU { get; } = false; 

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

        /// <summary>
        /// Артикул нормализован.
        /// </summary>
        /// <remarks>
        /// <para>Ожидание == Приход == Взаиморасчет и Остаток на складе == 0</para> 
        /// <para>Ожидание == Приход и Взаиморасчет + Остаток на складе == Ожидание</para> 
        /// </remarks>
        public bool IsSkuNormalize => 
            SKURansomsWaiting == SKUReceiptTotal && SKUReceiptTotal == SKUSummarySettlement && SKURemaining == 0 || 
            SKURansomsWaiting == SKUReceiptTotal && SKUSummarySettlement + SKURemaining == SKURansomsWaiting;

        /// <summary>
        /// Долг по оплате компенсации организацией перед клиентом.
        /// </summary>
        public int SKUDebtToClientForCompensationSum => 
            SKUWriteOffFromPoints - SKUCompensatedTotalToClient > 0 ? 
            SKUWriteOffFromPoints - SKUCompensatedTotalToClient : 
            0;
        #endregion
    }
}
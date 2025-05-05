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
        public bool IsParallelClientsSKU { get; } = false; 

        /// <summary>
        /// Приход товаров нормализован (соответствует ожиданию).
        /// </summary>
        public bool IsReceiptNormalizedToWaiting => SKUReceiptTotal == SKURansomsWaiting;

        /// <summary>
        /// Склад: Приход от точек меньше ожидания
        /// </summary>
        public int UnderReceipt =>
            SKUReceiptTotal < SKURansomsWaiting ?
            SKURansomsWaiting - SKUReceiptTotal :
            0;
        
        /// <summary>
        /// Склад: Приход от точек больше ожидания (экстра-приход)
        /// </summary>
        public int OverReceipt =>
            SKUReceiptTotal > SKURansomsWaiting ?
            SKUReceiptTotal - SKURansomsWaiting :
            0;

        /// <summary>
        /// Общий взаиморасчет нормализован относительно прихода и остатка
        /// </summary>
        public bool IsSettlementNormalizedToReceipt => SKUReceiptTotal == SKUSummarySettlement + SKURemaining;

        /// <summary>
        /// Общий взаиморасчет нормализован относительно ожидания
        /// </summary>
        public bool IsSettlementNormalizedToWaiting  => SKUSummarySettlement + SKURemaining == SKURansomsWaiting;

        /// <summary>
        /// Общий взаиморасчет меньше ожидания клиента
        /// </summary>
        public int UnderMutualSettlement => 
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
        public int OverMutualSettlement =>
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
        public bool IsSkuNormalize => IsReceiptNormalizedToWaiting && 
            (IsSettlementNormalizedToReceipt || IsSettlementNormalizedToWaiting);

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
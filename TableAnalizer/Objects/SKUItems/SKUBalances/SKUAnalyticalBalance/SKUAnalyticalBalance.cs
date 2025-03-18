namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUAnalyticalBalance
{
    public class SKUAnalyticalBalance
    {
        public int SKUNumber { get; set; }
        public double SKUPrice { get; set; }
        public int SKURansomsWaiting { get; set; }
        public int SKUReceiptTotal { get; set; }
        /// <summary>
        /// Взаиморасчет с клиентом.
        /// </summary>
        public int SKUMutualSettlement { get; set; }
        /// <summary>
        /// Остаток  товаров на складе.
        /// </summary>
        public int SKURemaining { get; set; }

        public bool SKUNomalize { get; set; }

        // /// <summary>
        // /// Взаиморачет меньше ожидания. 
        // /// </summary>
        // /// <remarks><b>Примечание</b>Привет</remarks>
        // public int LessMutualSettlementThanWaiting => 
        // SKUMutualSettlement < SKURansomsWaiting ? 
        //     SKURemaining > 0 ? 
        //     SKURansomsWaiting - SKUMutualSettlement - SKURemaining >= 0 ?
        //         SKURansomsWaiting - SKUMutualSettlement - SKURemaining :
        //         0 : 
        //     SKURansomsWaiting - SKUMutualSettlement > 0 ? 
        //     SKURansomsWaiting - SKUMutualSettlement :
        //     0 : 
        // 0;
        // /*
    }
}
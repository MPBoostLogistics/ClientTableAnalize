namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    ///  Client's remaining balance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURemainingBalance : IEquatable<SKURemainingBalance>
    {
        #region Properties
        public int SKUNumber { get; private set; } = default;
        public string SKUBrandAndName { get; private set; } = string.Empty;
        public int SKUReceipt { get; private set; } = 0;   // Приход товаров
        public int SKUShipped { get; private set; } = 0;   // Расход товаров
        public int SKURemaining { get; private set; } = 0; // Остаток товаров 
        public bool IsOnlyShipped => SKUShipped > 0 && SKUReceipt == 0 && SKURemaining == 0;
        public bool IsOnlyReceiptAndRemaining  => SKUShipped == 0 && (SKUReceipt > 0 || SKURemaining > 0);
        #endregion

        #region Constructors
        // For create from SKURemainingBalanceDataUnit collection.
        public SKURemainingBalance(int skuNumber, string skuBrandName, List<SKURemainingBalanceDataUnit> skuRemainingUnits)
        {
            SKUNumber = skuNumber;
            SKUBrandAndName = skuBrandName;

            SKUReceipt = skuRemainingUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUReceipt);
            SKURemaining = skuRemainingUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKURemaining);
        }

        // For create from SKUShippedDataUnit collection.
        public SKURemainingBalance(int skuNumber, List<SKUShippedDataUnit> skuShippedDataUnits)
        {
            SKUNumber = skuNumber;
            SKUShipped = skuShippedDataUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUShippedCount);
        }

        // For create new balance from repo.
         public SKURemainingBalance(int number, string brandAndName, int receipt, int shipped, int remaining) 
         {
            SKUNumber = number;
            SKUBrandAndName = brandAndName;

            SKUReceipt = receipt;
            SKUShipped = shipped;
            SKURemaining = remaining;
         }

        #endregion Constructors

        #region Functionality

        public bool Equals(SKURemainingBalance? other) =>
            SKUNumber == other?.SKUNumber;

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((SKURansomsBalance)other);
        }

        public override int GetHashCode() => SKUNumber.GetHashCode();

        #endregion Functionality
    }
}
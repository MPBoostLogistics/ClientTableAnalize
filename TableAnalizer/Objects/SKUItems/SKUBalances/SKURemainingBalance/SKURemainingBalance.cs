namespace TableAnalizer.Objects.SKUItems.SKUBalances
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
        /// <summary>
        /// Number of goods received at the warehouse.
        /// </summary>
        public int SKUReceipt { get; private set; } = 0;  
        /// <summary>
        /// Number of goods delivered to the customer.
        /// </summary>
        public int SKUShipped { get; private set; } = 0;  
        /// <summary>
        /// Number of goods transferred to production.
        /// </summary>
        public int SKUTransfered { get; private set; } = 0;   
        /// <summary>
        /// Number of goods remaining in the warehouse.
        /// </summary>
        public int SKURemaining { get; private set; } = 0; 
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

        // For create from SKUTransferDataUnit collection.
        public SKURemainingBalance(int skuNumber, List<SKUTransferDataUnit> skuTransferedDataUnits)
        {
            SKUNumber = skuNumber;
            SKUTransfered = skuTransferedDataUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUTransferedCount);
        }

        // For create new balance from repo.
         public SKURemainingBalance(int number, string brandAndName, int receipt, int shipped, int transfered, int remaining) 
         {
            SKUNumber = number;
            SKUBrandAndName = brandAndName;

            SKUReceipt = receipt;
            SKUShipped = shipped;
            SKUTransfered = transfered;
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
            return Equals((SKURemainingBalance)other);
        }

        public override int GetHashCode() => SKUNumber.GetHashCode();

        #endregion Functionality
    }
}
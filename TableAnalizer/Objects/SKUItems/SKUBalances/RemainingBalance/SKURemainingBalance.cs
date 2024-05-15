using ClientTableAnalizer.Enums;

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
        public SKURemainingBalance(List<SKURemainingBalanceDataUnit> skuRemainingUnits)
        {
            Update(skuRemainingUnits);
        }

        public SKURemainingBalance(List<SKUShippedDataUnit> skuShippedDataUnits)
        {
            SKUShipped = skuShippedDataUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUShippedCount);
        }

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

        public void Update(List<SKURemainingBalanceDataUnit> skuRemainingUnits) 
        {
            if(SKUNumber == default) 
            {
                SKUNumber = skuRemainingUnits
                .Select(u => u.SKUNumber)
                .First();
            }
            
            if(string.IsNullOrEmpty(SKUBrandAndName)) 
            {
                SKUBrandAndName = skuRemainingUnits
                .Select(u => u.SKUBrandAndName)
                .First();
            } 
            
            SKUReceipt = skuRemainingUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUReceipt);

            SKURemaining = skuRemainingUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKURemaining);
        }

        public void Update(List<SKUShippedDataUnit> skuShippedDataUnits) 
        {
            SKUShipped = skuShippedDataUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUShippedCount);
        }

        public bool Equals(SKURemainingBalance? other) =>
            SKUNumber == other?.SKUNumber;

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((SKURansomsBalance)other);
        }

        public override int GetHashCode() => new Guid().GetHashCode();

        #endregion Functionality
    }
}
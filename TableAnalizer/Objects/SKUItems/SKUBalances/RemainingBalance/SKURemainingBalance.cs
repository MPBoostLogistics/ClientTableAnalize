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
        public int SKUNumber { get; private set; }   
        public int SKUReceipt { get; private set; }   // Приход товаров
        public int  SKURemaining { get; private set; } // Остаток товаров 
        #endregion

        #region Constructors
        public SKURemainingBalance(List<SKURemainingBalanceDataUnit> skuRemainingBalanceDataUnit)
        {
            // var groupsBySKUNumber = from unit in skuRemainingBalanceDataUnit
            //                         group unit by unit.SKUNumber
            //                         into nameGroup
            //                         orderby nameGroup.Key
            //                         select nameGroup;
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

        public override int GetHashCode() => new Guid().GetHashCode();
        
        #endregion Functionality
    }
}
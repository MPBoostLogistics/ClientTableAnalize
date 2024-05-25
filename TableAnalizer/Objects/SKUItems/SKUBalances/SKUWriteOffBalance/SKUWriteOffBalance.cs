namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUWriteOffBalance : IEquatable<SKUWriteOffBalance>
    {
        #region Properties
        public int SKUNumber { get; private set; } = default;
        public int SKUWriteOff { get; private set; } = 0;   // Количество списанного по недостачам
        #endregion Properties

        #region Constructors
        public SKUWriteOffBalance(int skuNumber, List<SKUWriteOffDataUnit> skuWriteOffDataUnit)
        {
            SKUNumber = skuNumber;
            SKUWriteOff = skuWriteOffDataUnit
                .Aggregate(0, (acc, unit) => acc + unit.SKUWriteOffCount);
        }

        // Constructor for repo 
        public SKUWriteOffBalance(int skuNumber, int skuWriteOff)
        {
            SKUNumber = skuNumber;
            SKUWriteOff = skuWriteOff;
        }
        #endregion Constructors

        #region 
        public bool Equals(SKUWriteOffBalance? other) =>
            SKUNumber == other?.SKUNumber && SKUWriteOff == other?.SKUWriteOff;

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((SKUWriteOffBalance)other);
        }

        public override int GetHashCode() => SKUNumber.GetHashCode();
        #endregion
    }
}
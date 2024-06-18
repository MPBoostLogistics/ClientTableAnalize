namespace TableAnalizer.Objects.SKUItems.SKUBalances  
{
    /// <summary>
    ///  Client's dummies balance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKUDummiesBalance : IEquatable<SKUDummiesBalance> 
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public int Count { get; private set; } = 0;
        #endregion

        public SKUDummiesBalance(int skuNumber, List<SKUDummiesDataUnit> skuDummiesDataUnits)
        {
            SKUNumber = skuNumber;
            foreach (var unit in skuDummiesDataUnits)
            {
                Update(unit.DummiesSKUCount);
            }
        }

        public void Update(int count) 
        {
            Count += count;
        }

        public bool Equals(SKUDummiesBalance? other) =>
            SKUNumber == other?.SKUNumber;

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((SKUDummiesBalance)other);
        }

        public override int GetHashCode() => SKUNumber.GetHashCode();
    }
}

/*




*/
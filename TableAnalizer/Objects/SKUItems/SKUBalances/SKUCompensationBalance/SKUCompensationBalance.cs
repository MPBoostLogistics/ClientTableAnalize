namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    ///  Client's compensation balance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    public class SKUCompensationBalance : IEquatable<SKUCompensationBalance>
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public int Count { get; private set; } = 0;
        public double Sum { get; private set; } = 0;
        public string? Comments { get; private set; } = null;
        #endregion

        #region Constructors 
        public SKUCompensationBalance(int skuNumber, List<SKUCompensationDataUnit> skuCompensationDataUnits)
        {
            SKUNumber = skuNumber;
            foreach (var unit in skuCompensationDataUnits)
            {
                Update(unit.CompensationCount, unit.CompensationSum, unit.CompensationComment);
            }
        }
        #endregion Constructors

        #region Functionality
        public void Update(int count, double sum, string? comment) 
        {
            Count += count;
            Sum += sum;
            if(!string.IsNullOrEmpty(comment)) 
            {
                Comments = !string.IsNullOrEmpty(Comments) ? string.Concat(Comments, ", ", comment.Trim().ToLower()) : comment.Trim().ToLower();
            }
        }

        public bool Equals(SKUCompensationBalance? other) =>
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
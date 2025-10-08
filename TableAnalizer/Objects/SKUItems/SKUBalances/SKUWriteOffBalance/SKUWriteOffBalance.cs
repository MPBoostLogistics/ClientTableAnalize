namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUWriteOffBalance : IEquatable<SKUWriteOffBalance>
    {
        #region Properties
        public int SKUNumber { get; private set; } = default;
        /// <summary>
        /// Списано по недостачам с точек
        /// </summary>
        public int SKUWriteOffPoints { get; private set; } = 0;
        /// <summary>
        /// Списано по недостачам с точек через склад
        /// </summary>
        public int SKUWriteOffPointsViaWarehouseShortage { get; private set; } = 0;
        /// <summary>
        /// Списано по корректировкам с точек через склад
        /// </summary>
        public int SKUWriteOffPointsViaWarehouseAdjustment { get; private set; } = 0;

        #endregion Properties

        #region Constructors
        public SKUWriteOffBalance(int skuNumber, List<SKUWriteOffDataUnit> skuWriteOffDataUnit, PermanentDataKey source)
        {
            SKUNumber = skuNumber;

            switch (source)
            {
                case PermanentDataKey.PointsAndCouriers:
                    SKUWriteOffPoints = skuWriteOffDataUnit
                        .Aggregate(0, (acc, unit) => acc + unit.SKUWriteOffCount);
                    break;
                case PermanentDataKey.PointsViaWarehouseShortage:
                    SKUWriteOffPointsViaWarehouseShortage = skuWriteOffDataUnit
                       .Aggregate(0, (acc, unit) => acc + unit.SKUWriteOffCount);
                    break;
                case PermanentDataKey.PointsViaWarehouseAdjustment:
                    SKUWriteOffPointsViaWarehouseAdjustment = skuWriteOffDataUnit
                       .Aggregate(0, (acc, unit) => acc + unit.SKUWriteOffCount);
                    break;
            }
        }

        // Constructor for sku repo 
        public SKUWriteOffBalance(int skuNumber, int skuWriteOffPoints, int skuWriteOffPointsViaWarehouseShortage, int skuWriteOffPointsViaWarehouseAdjustment)
        {
            SKUNumber = skuNumber;
            SKUWriteOffPoints = skuWriteOffPoints;
            SKUWriteOffPointsViaWarehouseShortage = skuWriteOffPointsViaWarehouseShortage;
            SKUWriteOffPointsViaWarehouseAdjustment = skuWriteOffPointsViaWarehouseAdjustment;

        }
        #endregion Constructors

        #region 
        public bool Equals(SKUWriteOffBalance? other) =>
            SKUNumber == other?.SKUNumber &&
            SKUWriteOffPoints == other?.SKUWriteOffPoints &&
            SKUWriteOffPointsViaWarehouseShortage == other?.SKUWriteOffPointsViaWarehouseShortage &&
            SKUWriteOffPointsViaWarehouseAdjustment == other?.SKUWriteOffPointsViaWarehouseAdjustment
            ;

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
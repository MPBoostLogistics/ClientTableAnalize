namespace TableAnalizer.Objects.SKUItems.SKUBalances  
{
    /// <summary>
    /// Client's ransoms balance.
    /// </summary>
    public class SKURansomsBalance : IEquatable<SKURansomsBalance>
    {
        #region Properties
        /// <summary>
        /// Общее количество покупок
        /// </summary>
        public int SKUTotalNumberPurchases => 
            SKUInTransitNumberPurchases + SKUCancelledNumberPurchases + SKUDeliveredToPointsNumberPurchases +
            SKUPickedupFromPointsNumberPurchases + SKUUnknownNumberPurchases;
        /// <summary>
        /// Общее количество покупок, ожидаемое к возврату. 
        /// </summary>
        public int SKUWaitingNumberPurchases =>
            SKUDeliveredToPointsNumberPurchases + SKUPickedupFromPointsNumberPurchases;
        /// <summary>
        /// Количество покупок в пути.
        /// </summary>
        public int SKUInTransitNumberPurchases { get; private set; } = 0;
        /// <summary>
        /// Количество отмененных (возвращенных) покупок.
        /// </summary>
        public int SKUCancelledNumberPurchases { get; private set; } = 0;
        /// <summary>
        /// Количество покупок, доставленных на точки.
        /// </summary>
        public int SKUDeliveredToPointsNumberPurchases { get; private set; } = 0; 
        /// <summary>
        /// Количество покупок, забранных с точек.
        /// </summary>          
        public int SKUPickedupFromPointsNumberPurchases { get; private set; } = 0;
        /// <summary>
        /// Количество покупок в неизвестном статусе.
        /// </summary>          
        public int SKUUnknownNumberPurchases { get; private set; } = 0;  
        #endregion

        #region Constructors
        public SKURansomsBalance(List<SKURansomsBalanceDataUnit> skuRansomsBalanceUnits)
        {
            var groupsBySKUNumber = from unit in skuRansomsBalanceUnits
                                    group unit by unit.SKUNumber
                                    into nameGroup
                                    orderby nameGroup.Key
                                    select nameGroup;

            foreach (var group in groupsBySKUNumber)
            {
                foreach (var groupItem in group)
                {
                    SKUInTransitNumberPurchases += groupItem.SKUInTransitNumberPurchases;
                    SKUCancelledNumberPurchases += groupItem.SKUCancelledNumberPurchases;
                    SKUDeliveredToPointsNumberPurchases += groupItem.SKUDeliveredToPointsNumberPurchases;
                    SKUPickedupFromPointsNumberPurchases += groupItem.SKUPickedupFromPointsNumberPurchases;
                    SKUUnknownNumberPurchases += groupItem.SKUUnknownNumberPurchases;
                }
            }
        }

        /// <summary>
        /// Constructor for tests.
        /// </summary>
        public SKURansomsBalance(int? inTransit, int? canceled, int? deliveredToPoints, int? pickedupFromPoint, int? unknown)
        {
            SKUInTransitNumberPurchases = inTransit ?? 0;
            SKUCancelledNumberPurchases = canceled ?? 0;
            SKUDeliveredToPointsNumberPurchases = deliveredToPoints ?? 0;
            SKUPickedupFromPointsNumberPurchases = pickedupFromPoint ?? 0;
            SKUUnknownNumberPurchases = unknown ?? 0;
        }
        #endregion Constructors

        #region Functionality

        public bool Equals(SKURansomsBalance? other) =>
            this.SKUTotalNumberPurchases == other?.SKUTotalNumberPurchases &&
            this.SKUWaitingNumberPurchases == other?.SKUWaitingNumberPurchases &&
            this.SKUInTransitNumberPurchases == other?.SKUInTransitNumberPurchases &&
            this.SKUCancelledNumberPurchases == other?.SKUCancelledNumberPurchases &&
            this.SKUDeliveredToPointsNumberPurchases == other?.SKUDeliveredToPointsNumberPurchases &&
            this.SKUPickedupFromPointsNumberPurchases == other?.SKUPickedupFromPointsNumberPurchases;

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != this.GetType()) return false;
            return Equals((SKURansomsBalance)other);
        }

        public override int GetHashCode()
        {
            return new Guid().GetHashCode();
        }
        #endregion Functionality
    }
}
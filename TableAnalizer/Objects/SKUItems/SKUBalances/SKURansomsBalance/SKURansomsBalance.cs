using TableAnalizer.Enums;

namespace TableAnalizer.Objects.SKUItems.SKUBalances  
{
    /// <summary>
    ///  Client's ransoms balance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURansomsBalance : IEquatable<SKURansomsBalance>
    {
        #region Properties
        public int SKURansomsTotal { get; private set; }      // Всего
        public int SKURansomsWaiting { get; private set; }    // Ожидание 
        public int SKURansomsOnWay { get; }                   // В пути
        public int SKURansomsCanceled { get; }                // Отменен 
        public int SKURansomsDelivered { get; }               // Доставлен на ПВЗ
        public int SKURansomsWithDrawn { get; }               // Забран с ПВЗ
        #endregion

        #region Constructors
        public SKURansomsBalance(List<SKURansomsBalanceDataUnit> skuRansomsBalanceUnits)
        {
            var groupsByStatus = from unit in skuRansomsBalanceUnits
                                 group unit by unit.SKURansomsStatus
                                 into nameGroup
                                 orderby nameGroup.Key
                                 select nameGroup;

            foreach (var group in groupsByStatus)
            {
                switch (group.Key)
                {
                    case SKURansomsStatus.Delivered:
                        SKURansomsDelivered = group.Count();
                        break;
                    case SKURansomsStatus.WithDrawn:
                        SKURansomsWithDrawn = group.Count();
                        break;
                    case SKURansomsStatus.Canceled:
                        SKURansomsCanceled = group.Count();
                        break;
                    case SKURansomsStatus.OnWay:
                        SKURansomsOnWay = group.Count();
                        break;
                    case null:
                        break;
                }
            }

            CalculateTotalAndWaiting();
        }

        /// <summary>
        /// Constructor for tests.
        /// </summary>
        public SKURansomsBalance(int? onWay, int? canceled, int? delivered, int? withDrawn)
        {
            SKURansomsOnWay = onWay ?? 0;
            SKURansomsCanceled = canceled ?? 0;
            SKURansomsDelivered = delivered ?? 0;
            SKURansomsWithDrawn = withDrawn ?? 0;

            CalculateTotalAndWaiting();
        }
        #endregion Constructors

        #region Functionality

        public override string ToString() => $"Ransoms: total: {SKURansomsTotal}, on way: {SKURansomsOnWay}, canceled: {SKURansomsCanceled}, delivered: {SKURansomsDelivered}, withDrawn: {SKURansomsWithDrawn} (waiting: {SKURansomsWaiting})";

        private void CalculateTotalAndWaiting()
        {
            SKURansomsTotal = SKURansomsDelivered + SKURansomsWithDrawn + SKURansomsCanceled + SKURansomsOnWay;
            SKURansomsWaiting = SKURansomsDelivered + SKURansomsWithDrawn;
        }

        public bool Equals(SKURansomsBalance? other) =>
            this.SKURansomsTotal == other?.SKURansomsTotal &&
            this.SKURansomsWaiting == other?.SKURansomsWaiting &&
            this.SKURansomsOnWay == other?.SKURansomsOnWay &&
            this.SKURansomsCanceled == other?.SKURansomsCanceled &&
            this.SKURansomsDelivered == other?.SKURansomsDelivered &&
            this.SKURansomsWithDrawn == other?.SKURansomsWithDrawn;

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
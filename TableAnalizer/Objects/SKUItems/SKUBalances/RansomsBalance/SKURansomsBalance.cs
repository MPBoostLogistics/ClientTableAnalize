using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    ///  Client's ransoms balance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURansomsBalance : IEquatable<SKURansomsBalance>
    {
        #region Properties
        public int Total { get; private set; }
        public int Waiting { get; private set; }
        public int OnWay { get; }
        public int Canceled { get; }
        public int Delivered { get; }
        public int WithDrawn { get; }
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
                        Delivered = group.Count();
                        break;
                    case SKURansomsStatus.WithDrawn:
                        WithDrawn = group.Count();
                        break;
                    case SKURansomsStatus.Canceled:
                        Canceled = group.Count();
                        break;
                    case SKURansomsStatus.OnWay:
                        OnWay = group.Count();
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
            OnWay = onWay ?? default;
            Canceled = canceled ?? default;
            Delivered = delivered ?? default;
            WithDrawn = withDrawn ?? default;

            CalculateTotalAndWaiting();
        }
        #endregion

        private void CalculateTotalAndWaiting()
        {
            Total = Delivered + WithDrawn + Canceled + OnWay;
            Waiting = Delivered + WithDrawn;
        }

        public bool Equals(SKURansomsBalance? other) =>
            this.Total == other?.Total &&
            this.Waiting == other?.Waiting &&
            this.OnWay == other?.OnWay &&
            this.Canceled == other?.Canceled &&
            this.Delivered == other?.Delivered &&
            this.WithDrawn == other?.WithDrawn;

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
    }
}
namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    ///  Client's remaining balance.
    /// </summary>
    public class SKURemainingBalance : IEquatable<SKURemainingBalance>
    {
        #region Properties
        public long SKUNumber { get; private set; } = default;
        public int SKUReceipt { get; private set; }
        public int SKUShipmentGoods { get; private set; } = 0;
        public int SKUTransfered { get; private set; } = 0;
        public int SKURemaining { get; private set; } = 0;
        #endregion

        #region Constructors
        // For create from SKURemainingBalanceDataUnit collection.
        public SKURemainingBalance(long skuNumber, List<SKURemainingDataRow> skuRemainingUnits)
        {
            SKUNumber = skuNumber;

            SKUReceipt = skuRemainingUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUReceipt);
            SKURemaining = skuRemainingUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKURemaining);

            if (SKUNumber == 539735448)
                Console.WriteLine(this.ToString());
        }

        // For create from SKUShippedDataUnit collection.
        public SKURemainingBalance(long skuNumber, List<SKUShippedDataUnit> skuShippedDataUnits)
        {
            SKUNumber = skuNumber;
            SKUShipmentGoods = skuShippedDataUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUShippedCount);
        }

        // For create from SKUTransferDataUnit collection.
        public SKURemainingBalance(long skuNumber, List<SKUTransferDataUnit> skuTransferedDataUnits)
        {
            SKUNumber = skuNumber;
            SKUTransfered = skuTransferedDataUnits
                .Aggregate(0, (acc, unit) => acc + unit.SKUTransferedCount);
        }

        // For create new balance from repo.
        public SKURemainingBalance(long number, int receipt, int shipment, int transfered, int remaining)
        {
            SKUNumber = number;

            SKUReceipt = receipt;
            SKUShipmentGoods = shipment;
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

        public override string ToString() => $"Баланс товаров по арт ({SKUNumber}):" +
            $" приход {SKUReceipt}, расход {SKUShipmentGoods}" +
            $" передано {SKUTransfered}, остаток {SKURemaining}";

        #endregion Functionality
    }
}
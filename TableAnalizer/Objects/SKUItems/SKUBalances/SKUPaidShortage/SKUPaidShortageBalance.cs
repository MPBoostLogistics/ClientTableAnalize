namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUPaidShortage
{
    public class SKUPaidShortageBalance
    {
        public long SKUNumber { get; private set; }

        public Dictionary<ClaimExecutor, (int sum, double amount)> PaidShortageItems { get; private set; } = [];

        public SKUPaidShortageBalance(long skuNumber, List<SKUPaidShortageDataUnit> units)
        {
            SKUNumber = skuNumber;
            foreach (var unit in units)
            {
                Update(unit.ClaimExecutor, unit.SKUSum, unit.SKUAmount);
            }
        }

        public void Update(Dictionary<ClaimExecutor,(int sum, double amount)> сompensationItems) 
        {
            foreach (var item in сompensationItems)
            {
                Update(item.Key, item.Value.sum, item.Value.amount);
            }
        }

        public void Update(ClaimExecutor claimExecutor, int sum, double amount) 
        {
            (int sum, double amount) newItem;
            if(PaidShortageItems.TryGetValue(claimExecutor, out (int sum, double amount) existingItem)) 
            {
                newItem = existingItem;
                newItem.sum += sum;
                newItem.amount += amount;
            } 
            else 
            {
                newItem = (sum, amount);
            }

            PaidShortageItems[claimExecutor] = newItem;
        }
    
        public bool Equals(SKUPaidShortageBalance? other) =>
            SKUNumber == other?.SKUNumber && 
            PaidShortageItems == other?.PaidShortageItems;

        public override bool Equals(object? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != GetType()) return false;
            return Equals((SKUPaidShortageBalance)other);
        }

        public override int GetHashCode() => SKUNumber.GetHashCode();
    
    }
}


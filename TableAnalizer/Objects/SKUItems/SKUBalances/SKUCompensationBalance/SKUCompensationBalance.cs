using System.Diagnostics;
using System.Net.WebSockets;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUCompensationBalance
{
    /// <summary>
    ///  Client's compensation balance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    public class SKUCompensationBalance : IEquatable<SKUCompensationBalance>
    {
        #region Variables and constanst
        private const string COMPENSATION_TARGET_WAREHOUSE_NAME = "Компенсация от склада";
        private const string COMPENSATION_TARGET_POINTS_NAME = "Компенсация от точек";
        #endregion

        #region Properties
        public int SKUNumber { get; private set; }
        public Dictionary<CompensationTarget,(int count, double sum)> CompensationItems { get; private set; } = [];
        #endregion

        #region Constructors 
        public SKUCompensationBalance(int skuNumber, List<SKUCompensationDataUnit> skuCompensationDataUnits)
        {
            SKUNumber = skuNumber;
            foreach (var unit in skuCompensationDataUnits)
            {
                if(string.IsNullOrEmpty(unit.CompensationComment)) 
                {
                    Trace.WriteLine("Compensation info must contain a comment. Skipping.");
                } 
                else 
                {
                    Update(unit.CompensationCount, unit.CompensationSum, unit.CompensationComment);
                }
            }
        }
        #endregion Constructors

        #region Functionality

        public void Update(Dictionary<CompensationTarget,(int count, double sum)> сompensationItems) 
        {
            foreach (var item in сompensationItems)
            {
                (int count, double sum) newItem;
                if(CompensationItems.TryGetValue(item.Key, out (int count, double sum) existingItem)) 
                {
                    newItem = existingItem;
                    if(newItem.count != item.Value.count || newItem.sum != item.Value.sum)
                    newItem.count += item.Value.count;
                    newItem.sum += item.Value.sum;
                } 
                else 
                {
                    newItem = (item.Value.count, item.Value.sum);
                }

                CompensationItems[item.Key] = newItem;
            }
        }

        public void Update(int count, double sum, string comment) 
        {
            CompensationTarget? target = null;
            switch(comment) 
            {
                case COMPENSATION_TARGET_WAREHOUSE_NAME:
                    target = CompensationTarget.Warehouse;
                    break;
                case COMPENSATION_TARGET_POINTS_NAME:
                    target = CompensationTarget.PickupPoint;
                    break;
                default:
                    break;
            }

            if(target == null) return;

            (int count, double sum) newItem;
            if(CompensationItems.TryGetValue((CompensationTarget)target, out (int count, double sum) existingItem)) 
            {
                newItem = existingItem;
                newItem.count += count;
                newItem.sum += sum;
            } 
            else 
            {
                newItem = (count, sum);
            }

            CompensationItems[(CompensationTarget)target] = newItem;
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
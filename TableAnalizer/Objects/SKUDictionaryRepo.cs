using System.Collections.Concurrent;
using ClientTableAnalizer.Interfaces;
using ClientTableAnalizer.Objects.SKUItems;

namespace ClientTableAnalizer.Objects
//https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items
{
    public class SKUDictionaryRepo : IRepository<int, SKU>
    {
        private ConcurrentDictionary<int,SKU> SKUs { get; } = new ConcurrentDictionary<int,SKU>();

        public int Count() => SKUs.Count;

        public bool RetrieveAndUpdateOrAddValue(int id, SKU item)
        {
            // If element exists
            if (SKUs.TryGetValue(id, out SKU? retrievedValue) && retrievedValue is not null) 
            {
                SKU? newValue = null; 

                if(retrievedValue.Marketplace.HasValue && retrievedValue.SKURansomsBalance is not null && item.SKURemainingBalance is not null) 
                {
                    newValue = new SKU(retrievedValue.Marketplace.Value, retrievedValue.SKUNumber, retrievedValue.SKURansomsBalance, retrievedValue.AveragePrice, retrievedValue.MaxRansomsDate);
                    newValue.Update(skuRemainingBalance: item.SKURemainingBalance);
                } 
                else if (retrievedValue.SKURemainingBalance is not null && item.Marketplace.HasValue && item.SKURansomsBalance is not null) 
                {
                    newValue = new SKU(retrievedValue.SKUNumber,retrievedValue.SKURemainingBalance);
                    newValue.Update(item.Marketplace.Value, item.SKURansomsBalance, item.AveragePrice, item.MaxRansomsDate);
                }

                if(newValue is null) 
                {
                    return false;
                }
                else 
                {
                    return SKUs.TryUpdate(id, newValue, retrievedValue);
                }
            } 
            else 
            {
                return SKUs.TryAdd(id, item);
            } 
        }

        public bool GetValue(int id, out SKU? item)  => SKUs.TryGetValue(id, out item);

        public IEnumerable<SKU> GetValuesList() => SKUs.Select(s => s.Value).AsEnumerable();

        public bool RemoveValue(int id, out SKU? item) => SKUs.TryRemove(id, out item);

        public void Dispose()
        {
            SKUs.Clear();
        }
    }
}
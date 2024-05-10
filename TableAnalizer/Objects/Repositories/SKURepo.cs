using ClientTableAnalizer.Objects.SKUItems;

namespace ClientTableAnalizer.Objects.Repositories
//https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items
{
    public class SKURepo : BaseRepo<int, SKU>
    {
        public override bool RetrieveAndUpdateOrAddValue(int id, SKU item)
        {
            if (Entiies.TryGetValue(id, out SKU? retrievedValue) && retrievedValue is not null) 
            {
                SKU? newValue = null; 

                if(retrievedValue.Marketplace.HasValue && retrievedValue.SKURansomsBalance is not null && item.SKURemainingBalance is not null) 
                {
                    newValue = new SKU(retrievedValue.Marketplace.Value, retrievedValue.SKUNumber, retrievedValue.SKURansomsBalance, retrievedValue.AveragePrice, retrievedValue.MaxRansomsDate);
                    newValue.Update(skuRemainingBalance: item.SKURemainingBalance);
                } 
                else if (retrievedValue.Marketplace.HasValue && retrievedValue.SKURemainingBalance is not null && item.Marketplace.HasValue && item.SKURansomsBalance is not null) 
                {
                    newValue = new SKU(retrievedValue.Marketplace.Value,retrievedValue.SKUNumber,retrievedValue.SKURemainingBalance);
                    newValue.Update(item.Marketplace.Value, item.SKURansomsBalance, item.AveragePrice, item.MaxRansomsDate);
                } 

                if(newValue is null) 
                {
                    return false;
                }
                else 
                {
                    return Entiies.TryUpdate(id, newValue, retrievedValue);
                }
            } 
            else 
            {
                return Entiies.TryAdd(id, item);
            } 
        }
    }
}
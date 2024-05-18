using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.SKUItems;
using ClientTableAnalizer.Objects.SKUItems.SKUBalances;

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

                Marketplace? skuMarketplace = retrievedValue.Marketplace;
                int skuNumber = retrievedValue.SKUNumber;
                double skuPrice = retrievedValue.AveragePrice != default ? retrievedValue.AveragePrice : item.AveragePrice != default ? item.AveragePrice : default;
                DateOnly skuDate =  retrievedValue.MaxRansomsDate != default ? retrievedValue.MaxRansomsDate : item.MaxRansomsDate != default ? item.MaxRansomsDate : default;

                bool isNewRansomsBalance = item.SKURansomsBalance is not null;
                bool isOldRansomsBalance = retrievedValue.SKURansomsBalance is not null;

                bool isNewRemainingBalance = item.SKURemainingBalance is not null;
                bool isOldRemainingBalance = retrievedValue.SKURemainingBalance is not null;

                bool isNewShortageBalance = item.SKUShortageBalance is not null;

                // Update by SKURansomsBalance
                if(isNewRansomsBalance && !isOldRansomsBalance && !isNewRemainingBalance) 
                {
                    newValue = new SKU(skuMarketplace, skuNumber, item.SKURansomsBalance!, skuPrice, skuDate);
                } 

                // Update by SKURemainingBalance
                else if (isNewRemainingBalance && !isNewRansomsBalance) 
                {
                    if (!isOldRemainingBalance)
                    {
                        newValue = new SKU(skuMarketplace, skuNumber, retrievedValue.SKURansomsBalance!, skuPrice, skuDate);
                        newValue.Update(item.SKURemainingBalance!);
                    }
                    else
                    {
                        var isOldBalanceOnlyShipped = retrievedValue.SKURemainingBalance!.IsOnlyShipped;
                        var isNewBalanceOnlyShipped = item.SKURemainingBalance!.IsOnlyShipped;
                        var isOldBalanceOnlyRemaining = retrievedValue.SKURemainingBalance!.IsOnlyReceiptAndRemaining;
                        var isNewBalanceOnlyRemainig = item.SKURemainingBalance!.IsOnlyReceiptAndRemaining;

                        SKURemainingBalance newRemainingBalance;

                        int number = default, receipt = default, shipped = default, remaining = default;
                        string brandAndName = string.Empty;

                        if (isOldBalanceOnlyShipped && isNewBalanceOnlyRemainig)
                        {
                            number = item.SKURemainingBalance.SKUNumber;
                            brandAndName = item.SKURemainingBalance.SKUBrandAndName;
                            receipt = item.SKURemainingBalance.SKUReceipt;
                            shipped = retrievedValue.SKURemainingBalance.SKUShipped;
                            remaining = item.SKURemainingBalance.SKURemaining;
                        }
                        else if (isNewBalanceOnlyShipped && isOldBalanceOnlyRemaining)
                        {
                            number = retrievedValue.SKURemainingBalance.SKUNumber;
                            brandAndName = retrievedValue.SKURemainingBalance.SKUBrandAndName;
                            receipt = retrievedValue.SKURemainingBalance.SKUReceipt;
                            shipped = item.SKURemainingBalance!.SKUShipped;
                            remaining = retrievedValue.SKURemainingBalance.SKURemaining;
                        }

                        newRemainingBalance = new(number, brandAndName, receipt, shipped, remaining);

           
                        if(isOldRemainingBalance) 
                        {
                            newValue = new SKU(skuMarketplace, skuNumber,retrievedValue.SKURansomsBalance! , skuPrice, skuDate);
                            newValue.Update(newRemainingBalance);
                        } 
                        else 
                        {
                            newValue = new SKU(skuMarketplace, skuNumber,newRemainingBalance);
                        }
                    }
                }

                // Update by SKUShortageBalance
                if(isNewShortageBalance) 
                {
                    newValue?.Update(item.SKUShortageBalance!);
                } 

                return newValue is not null && Entiies.TryUpdate(id, newValue, retrievedValue);
            }
            else
            {
                return Entiies.TryAdd(id, item);
            }
        }
    }
}
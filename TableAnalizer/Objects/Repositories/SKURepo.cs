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
                SKU? newValue = CreateNewSKU(retrievedValue, item);

                return newValue is not null && Entiies.TryUpdate(id, newValue, retrievedValue);
            }
            else
            {
                return Entiies.TryAdd(id, item);
            }
        }

        private static SKU CreateNewSKU(SKU retriveValue, SKU newValue) 
        {
            Marketplace? marketplace = retriveValue.Marketplace is not null ? 
                retriveValue.Marketplace : 
                newValue.Marketplace is not null ? 
                    newValue.Marketplace : 
                    null;

            int skuNumber = retriveValue.SKUNumber;

            string? skuBrand = retriveValue.SKUBrand is not null ? 
                retriveValue.SKUBrand : 
                newValue.SKUBrand is not null ? 
                    newValue.SKUBrand : 
                    null;

            string? skuName = retriveValue.SKUName is not null ? 
                retriveValue.SKUName : 
                newValue.SKUName is not null ? 
                    newValue.SKUName : 
                    null;
            
            // SKURansomsBalance
            SKURansomsBalance? skuRansomsBalance;
            if(retriveValue.SKURansomsBalance is not null) 
            {
                skuRansomsBalance = retriveValue.SKURansomsBalance;
                if(newValue.SKURansomsBalance is not null) 
                {
                    int? onWay = newValue.SKURansomsBalance.SKURansomsOnWay;
                    int? canceled = newValue.SKURansomsBalance.SKURansomsCanceled;
                    int? delivered = newValue.SKURansomsBalance.SKURansomsDelivered;
                    int? withDrawn = newValue.SKURansomsBalance.SKURansomsWithDrawn;

                    skuRansomsBalance.Update(onWay, canceled, delivered, withDrawn);
                }
            } 
            else if(newValue.SKURansomsBalance is not null) 
            {
                skuRansomsBalance = newValue.SKURansomsBalance;
            } 
            else 
            {
                skuRansomsBalance = null;
            }

            // SKURemainingBalance
            SKURemainingBalance? skuRemainingBalance;
            if(retriveValue.SKURemainingBalance is not null) 
            {
                skuRemainingBalance = retriveValue.SKURemainingBalance;
                if(newValue.SKURemainingBalance is not null) 
                {
                    int skuReceipt = newValue.SKURemainingBalance.SKUReceipt;
                    int skuShipped = newValue.SKURemainingBalance.SKUShipped;
                    int skuRemaining = newValue.SKURemainingBalance.SKURemaining;

                    skuRemainingBalance.Update(skuReceipt, skuShipped, skuRemaining);
                }
            } 
            else if(newValue.SKURemainingBalance is not null) 
            {
                skuRemainingBalance = newValue.SKURemainingBalance;
            } 
            else 
            {
                skuRemainingBalance = null;
            }

            // SKUCompensationBalance
            SKUCompensationBalance? skuCompensationBalance;
            if(retriveValue.SKUCompensationBalance is not null)  
            {
                skuCompensationBalance = retriveValue.SKUCompensationBalance;
                if(newValue.SKUCompensationBalance is not null)  
                {
                    int count = newValue.SKUCompensationBalance.Count;
                    double sum = newValue.SKUCompensationBalance.Sum;
                    string? comment = newValue.SKUCompensationBalance.Comments;

                    skuCompensationBalance.Update(count, sum, comment);
                }

            }
            else if(newValue.SKUCompensationBalance is not null) 
            {
                skuCompensationBalance = newValue.SKUCompensationBalance;
            } 
            else 
            {
                skuCompensationBalance = null;
            }
        
            // SKUResortBalance
            SKUResortBalance? skuResortBalance;
            if(retriveValue.SKUResortBalance is not null) 
            {
                skuResortBalance = retriveValue.SKUResortBalance;
                if(newValue.SKUResortBalance is not null) 
                {
                    var resortSKUsInNewValue = newValue.SKUResortBalance.ResortSKUs;
                    if(resortSKUsInNewValue is not null && resortSKUsInNewValue.Count > 0) 
                    {
                        skuResortBalance.Update(resortSKUsInNewValue);
                    }
                }
            } 
            else if(newValue.SKUResortBalance is not null) 
            {
                skuResortBalance = newValue.SKUResortBalance;
            }
            else 
            {
                skuResortBalance = null;
            }

            // AveragePrice
            double? averagePrice = retriveValue.AveragePrice is not (double)default ? 
                retriveValue.AveragePrice : 
                newValue.AveragePrice is not (double)default ? 
                    newValue.AveragePrice : 
                    null;

            // MaxRansomsDate
   
            DateOnly? maxRansomsDate = retriveValue.MaxRansomsDate != SKU.DATEONLY_DEFAULT ? 
                retriveValue.MaxRansomsDate : 
                newValue.MaxRansomsDate  != SKU.DATEONLY_DEFAULT ? 
                    newValue.MaxRansomsDate : 
                    null;

            return new SKU(marketplace, skuNumber, skuBrand, skuName, skuRansomsBalance, 
                skuRemainingBalance, skuCompensationBalance, skuResortBalance, averagePrice, maxRansomsDate);
        }
    }
}
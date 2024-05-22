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
            // Marketplace 
            Marketplace? marketplace = null;
            switch(retriveValue.Marketplace.HasValue, newValue.Marketplace.HasValue) 
            {
                case (false, true):
                    marketplace = newValue.Marketplace;
                    break;
                case (true, false): case (true, true):
                    marketplace = retriveValue.Marketplace;
                    break;
            };

            // SKUNumber  
            int skuNumber = retriveValue.SKUNumber;
            
            // SKUBrand
            bool isRetriveValueIncludesSKUBrand = !string.IsNullOrEmpty(retriveValue.SKUBrand);
            bool isNewValueIncludesSKUBrand = !string.IsNullOrEmpty(newValue.SKUBrand);
            string skuBrand = (isRetriveValueIncludesSKUBrand, isNewValueIncludesSKUBrand) switch
            {
                (false, true) => newValue.SKUBrand,
                (true, false) or (true, true) => retriveValue.SKUBrand,
                _ => string.Empty,
            };

            // SKUName
            bool isRetriveValueIncludesSKUName = !string.IsNullOrEmpty(retriveValue.SKUName);
            bool isNewValueIncludesSKUName = !string.IsNullOrEmpty(newValue.SKUName);
            string skuName = (isRetriveValueIncludesSKUName, isNewValueIncludesSKUName) switch
            {
                (false, true) => newValue.SKUName,
                (true, false) or (true, true) => retriveValue.SKUName,
                _ => string.Empty,
            };
            
            // SKURansomsBalance
            SKURansomsBalance? skuRansomsBalance = null;
            switch(retriveValue.SKURansomsBalance is not null, newValue.SKURansomsBalance is not null) 
            {
                case (false,true):
                    skuRansomsBalance = newValue.SKURansomsBalance;
                    break;

                case (true,true): case (true,false):

                    skuRansomsBalance = retriveValue.SKURansomsBalance;

                    if(newValue.SKURansomsBalance is not null) 
                    {
                        int? onWay =  retriveValue.SKURansomsBalance!.SKURansomsOnWay + newValue.SKURansomsBalance.SKURansomsOnWay;
                        int? canceled = retriveValue.SKURansomsBalance!.SKURansomsCanceled + newValue.SKURansomsBalance.SKURansomsCanceled;
                        int? delivered = retriveValue.SKURansomsBalance!.SKURansomsDelivered + newValue.SKURansomsBalance.SKURansomsDelivered;
                        int? withDrawn = retriveValue.SKURansomsBalance!.SKURansomsWithDrawn + newValue.SKURansomsBalance.SKURansomsWithDrawn;

                        skuRansomsBalance = new (onWay, canceled, delivered, withDrawn);
                    }

                    break;
            }

            // SKURemainingBalance
            SKURemainingBalance? skuRemainingBalance = null;
            switch(retriveValue.SKURemainingBalance is not null, newValue.SKURemainingBalance is not null) 
            {
                case (false,true):
                    skuRemainingBalance = newValue.SKURemainingBalance;
                    break;

                case (true,true): case (true,false):

                    skuRemainingBalance = retriveValue.SKURemainingBalance;

                    if(newValue.SKURemainingBalance is not null) 
                    {
                        string brandAndName = !string.IsNullOrEmpty(retriveValue.SKURemainingBalance!.SKUBrandAndName) ?
                            retriveValue.SKURemainingBalance!.SKUBrandAndName : 
                                !string.IsNullOrEmpty(newValue.SKURemainingBalance!.SKUBrandAndName) ? 
                                newValue.SKURemainingBalance!.SKUBrandAndName : 
                                string.Empty;

                        int skuReceipt = retriveValue.SKURemainingBalance!.SKUReceipt + newValue.SKURemainingBalance.SKUReceipt;
                        int skuShipped = retriveValue.SKURemainingBalance!.SKUShipped + newValue.SKURemainingBalance.SKUShipped;
                        int skuRemaining = retriveValue.SKURemainingBalance!.SKURemaining + newValue.SKURemainingBalance.SKURemaining;

                        skuRemainingBalance = new SKURemainingBalance(skuNumber, brandAndName, skuReceipt,skuShipped, skuRemaining);
                    }

                    break;
            }

            // SKUCompensationBalance
            SKUCompensationBalance? skuCompensationBalance = null;
            switch(retriveValue.SKUCompensationBalance is not null, newValue.SKUCompensationBalance is not null) 
            {
                case (false,true):
                    skuCompensationBalance = newValue.SKUCompensationBalance;
                    break;

                case (true,true): case (true,false):

                    skuCompensationBalance = retriveValue.SKUCompensationBalance;

                    if(newValue.SKUCompensationBalance is not null) 
                    {
                        int count = newValue.SKUCompensationBalance.Count;
                        double sum = newValue.SKUCompensationBalance.Sum;
                        string? comment = newValue.SKUCompensationBalance.Comments;

                        skuCompensationBalance!.Update(count, sum, comment);
                    }

                    break;
            }

            // SKUResortBalance
            SKUResortBalance? skuResortBalance = null;
            switch(retriveValue.SKUResortBalance is not null, newValue.SKUResortBalance is not null) 
            {
                case (false,true):
                    skuResortBalance = newValue.SKUResortBalance!;
                    break;
                case (true,true): case (true,false):
                    skuResortBalance = retriveValue.SKUResortBalance!;

                    if(newValue.SKUResortBalance is not null) 
                    {
                        var resortSKUsInNewValue = newValue.SKUResortBalance.ResortSKUs;
                        if(resortSKUsInNewValue is not null && resortSKUsInNewValue.Count > 0) 
                        {
                            skuResortBalance!.Update(resortSKUsInNewValue);
                        }
                    }
                    break;
            }

            // AveragePrice
            bool isRetriveValueIncludesPrice = !Equals(retriveValue.AveragePrice, SKU.AVERAGE_PRICE_DEFAULT);
            bool isNewValueIncludesPrice = !Equals(newValue.AveragePrice, SKU.AVERAGE_PRICE_DEFAULT);
            double? averagePrice = (isRetriveValueIncludesPrice, isNewValueIncludesPrice) switch
            {
                (false, true) => newValue.AveragePrice,
                (true, false) => retriveValue.AveragePrice,
                (true, true) => new List<double>(){retriveValue.AveragePrice, newValue.AveragePrice}.Min(),
                (false, false) => null
            };

            // MaxRansomsDate
            bool isRetriveValueIncludesDate = !Equals(retriveValue.MaxRansomsDate, SKU.DATEONLY_DEFAULT);
            bool isNewValueIncludesDate = !Equals(newValue.MaxRansomsDate, SKU.DATEONLY_DEFAULT);
            DateOnly? maxRansomsDate = (isRetriveValueIncludesDate, isNewValueIncludesDate) switch
            {
                (false, true) => newValue.MaxRansomsDate,
                (true, false) => retriveValue.MaxRansomsDate,
                (true, true) => new List<DateOnly>(){retriveValue.MaxRansomsDate, newValue.MaxRansomsDate}.Max(),
                (false, false) => null
            };

            // MaxRansomsDate
            bool isRetriveValueIncludesDeadLine = !Equals(retriveValue.DeadLineDays, SKU.DEADLINE_DAYS_DEFAULT);
            bool isNewValueIncludesDeadLine = !Equals(newValue.DeadLineDays, SKU.DEADLINE_DAYS_DEFAULT);
            int? deadlineDays = (isRetriveValueIncludesDeadLine, isNewValueIncludesDeadLine) switch
            {
                (false, true) => newValue.DeadLineDays,
                (true, false) => retriveValue.DeadLineDays,
                (true, true) => new List<int>(){retriveValue.DeadLineDays, newValue.DeadLineDays}.Max(),
                (false, false) => null
            };

            return new SKU(marketplace, skuNumber, skuBrand, skuName, skuRansomsBalance, 
                skuRemainingBalance, skuCompensationBalance, skuResortBalance!, averagePrice, maxRansomsDate, deadlineDays);
        }
    }
}
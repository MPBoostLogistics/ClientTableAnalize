using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalances;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUPaidShortage;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance;

namespace TableAnalizer.Objects.Repositories
//https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items
{
    public class SKURepo : BaseRepo<int, SKU>
    {
        public override bool RetrieveAndUpdateOrAddValue(int id, SKU item, bool isMulty = false)
        {
            if (Entiies.TryGetValue(id, out SKU? retrievedValue) && retrievedValue is not null)
            {
                SKU? newValue = CreateNewSKU(retrievedValue, item, isMulty);
                return newValue is not null && Entiies.TryUpdate(id, newValue, retrievedValue);
            }
            else
            {
                return Entiies.TryAdd(id, item);
            }
        }

        private static SKU CreateNewSKU(SKU retriveValue, SKU newValue, bool isSummary) 
        {
            // Logins
            List<string> clientLoginsList =  [.. retriveValue.ClientLogins
                        .Union(newValue.ClientLogins)
                        .OrderByDescending(el => el)];
             
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
            bool isRetriveValueIncludesSKUBrand = !string.IsNullOrEmpty(retriveValue.SKUBrand) && retriveValue.SKUBrand != SKU.DEFAULT_BRAND ;
            bool isNewValueIncludesSKUBrand = !string.IsNullOrEmpty(newValue.SKUBrand) && newValue.SKUBrand != SKU.DEFAULT_BRAND;
            string skuBrand = (isRetriveValueIncludesSKUBrand, isNewValueIncludesSKUBrand) switch
            {
                (false, true) => newValue.SKUBrand,
                (true, false) or (true, true) => retriveValue.SKUBrand,
                _ => SKU.DEFAULT_BRAND
            };

            // SKUName
            bool isRetriveValueIncludesSKUName = !string.IsNullOrEmpty(retriveValue.SKUName) && retriveValue.SKUName != SKU.DEFAULT_NAME;
            bool isNewValueIncludesSKUName = !string.IsNullOrEmpty(newValue.SKUName) && newValue.SKUName != SKU.DEFAULT_NAME;
            string skuName = (isRetriveValueIncludesSKUName, isNewValueIncludesSKUName) switch
            {
                (false, true) => newValue.SKUName,
                (true, false) or (true, true) => retriveValue.SKUName,
                 _ => SKU.DEFAULT_NAME
            };
            
            // SKURansomsBalance
            SKURansomsBalance? skuRansomsBalance = null;
            switch(retriveValue.SKURansomsBalance is not null, newValue.SKURansomsBalance is not null) 
            {
                case (true,false):
                    skuRansomsBalance = retriveValue.SKURansomsBalance;
                    break;

                case (false,true):
                    skuRansomsBalance = newValue.SKURansomsBalance;
                    break;
                
                case (true,true): 
                    int? inTransitNumberPurchases = CombineIntValues(retriveValue.SKURansomsBalance!.SKUInTransitNumberPurchases, newValue.SKURansomsBalance!.SKUInTransitNumberPurchases, isSummary);
                    int? cancelledNumberPurchases = CombineIntValues(retriveValue.SKURansomsBalance!.SKUCancelledNumberPurchases, newValue.SKURansomsBalance!.SKUCancelledNumberPurchases, isSummary);
                    int? deliveredToPointsNumberPurchases = CombineIntValues(retriveValue.SKURansomsBalance!.SKUDeliveredToPointsNumberPurchases, newValue.SKURansomsBalance!.SKUDeliveredToPointsNumberPurchases, isSummary);
                    int? pickedupFromPointsNumberPurchases = CombineIntValues(retriveValue.SKURansomsBalance!.SKUPickedupFromPointsNumberPurchases, newValue.SKURansomsBalance!.SKUPickedupFromPointsNumberPurchases, isSummary);
                    int? unknownNumberPurchases = CombineIntValues(retriveValue.SKURansomsBalance!.SKUPickedupFromPointsNumberPurchases, newValue.SKURansomsBalance!.SKUUnknownNumberPurchases, isSummary);
                    
                    skuRansomsBalance = new (inTransitNumberPurchases, cancelledNumberPurchases, deliveredToPointsNumberPurchases, pickedupFromPointsNumberPurchases, unknownNumberPurchases);
                    
                    break;
            }

            // SKURemainingBalance
            SKURemainingBalance? skuRemainingBalance;

            var skuReceiptRv = retriveValue.SKURemainingBalance?.SKUReceipt is not null && retriveValue.SKURemainingBalance?.SKUReceipt > 0;
            var skuReceiptNv = newValue.SKURemainingBalance?.SKUReceipt is not null && newValue.SKURemainingBalance?.SKUReceipt > 0;
            int? skuReceipt = (skuReceiptRv, skuReceiptNv) switch 
            {
                (true,false) or (true,true) => retriveValue.SKURemainingBalance?.SKUReceipt,
                (false,true) => newValue.SKURemainingBalance?.SKUReceipt,
                _ => 0
            };

            var skuOutgoingRv = retriveValue.SKURemainingBalance?.SKUOutgoingProducts is not null && retriveValue.SKURemainingBalance?.SKUOutgoingProducts > 0;
            var skuOutgoingNv = newValue.SKURemainingBalance?.SKUOutgoingProducts is not null && newValue.SKURemainingBalance?.SKUOutgoingProducts > 0;
            int? skuOutgoing = (skuOutgoingRv, skuOutgoingNv) switch 
            {
                (true,false) => retriveValue.SKURemainingBalance?.SKUOutgoingProducts,
                (true,true) => retriveValue.SKURemainingBalance?.SKUOutgoingProducts + newValue.SKURemainingBalance?.SKUOutgoingProducts,
                (false,true) => newValue.SKURemainingBalance?.SKUOutgoingProducts,
                _ => 0
            };

            var skuTransferedRv = retriveValue.SKURemainingBalance?.SKUTransfered is not null && retriveValue.SKURemainingBalance?.SKUTransfered > 0;
            var skuTransferedNv = newValue.SKURemainingBalance?.SKUTransfered is not null && newValue.SKURemainingBalance?.SKUTransfered > 0;
            int? skuTransfered = (skuTransferedRv, skuTransferedNv) switch 
            {
                (true,false) => retriveValue.SKURemainingBalance?.SKUTransfered,
                (true,true) => retriveValue.SKURemainingBalance?.SKUTransfered + newValue.SKURemainingBalance?.SKUTransfered,
                (false,true) => newValue.SKURemainingBalance?.SKUTransfered,
                _ => 0
            };

            var skuRemainingRv = retriveValue.SKURemainingBalance?.SKURemaining is not null && retriveValue.SKURemainingBalance?.SKURemaining > 0;
            var skuRemainingNv = newValue.SKURemainingBalance?.SKURemaining is not null && newValue.SKURemainingBalance?.SKURemaining > 0;
            int? skuRemaining = (skuRemainingRv, skuRemainingNv) switch 
            {
                (true,false) or (true,true) => retriveValue.SKURemainingBalance?.SKURemaining,
                (false,true) => newValue.SKURemainingBalance?.SKURemaining,
                _ => 0
            };

            skuRemainingBalance = new(skuNumber, skuReceipt ?? 0, skuOutgoing ?? 0, skuTransfered ?? 0, skuRemaining ?? 0);

            // SKUWriteOffBalance
            SKUWriteOffBalance? skuWriteOffBalance = null;
            switch(retriveValue.SKUWriteOffBalance is not null, newValue.SKUWriteOffBalance is not null) 
            {
                case (false,true):
                    skuWriteOffBalance = newValue.SKUWriteOffBalance;
                    break;

                case (true,true): case (true,false):

                    skuWriteOffBalance = retriveValue.SKUWriteOffBalance;

                    if(newValue.SKUWriteOffBalance is not null) 
                    {
                        int skuWriteOffPoints = retriveValue.SKUWriteOffBalance!.SKUWriteOffPoints + newValue.SKUWriteOffBalance.SKUWriteOffPoints;
                        int shortage = retriveValue.SKUWriteOffBalance!.SKUWriteOffPointsViaWarehouseShortage + newValue.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseShortage;
                        int adjustment = retriveValue.SKUWriteOffBalance!.SKUWriteOffPointsViaWarehouseAdjustment + newValue.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseAdjustment;
                        skuWriteOffBalance = new SKUWriteOffBalance(skuNumber, skuWriteOffPoints, shortage, adjustment);
                    }

                    break;
            }

            // SKUPaidShortageBalance
            SKUPaidShortageBalance? skuPaidShortageBalance = null;
            switch(retriveValue.SKUPaidShortageBalance is not null, newValue.SKUPaidShortageBalance is not null) 
            {
                 case (true,false):
                    skuPaidShortageBalance = retriveValue.SKUPaidShortageBalance;
                    break;

                case (false,true):
                    skuPaidShortageBalance = newValue.SKUPaidShortageBalance;
                    break;

                case (true,true):
                    skuPaidShortageBalance!.Update(newValue.SKUPaidShortageBalance!.PaidShortageItems);
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
                        var resortSKUsInNewValue = newValue.SKUResortBalance.SKUsToResort;
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
                (true, true) => double.Min(retriveValue.AveragePrice, newValue.AveragePrice),
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

            // DeadLine
            bool isRetriveValueIncludesDeadLine = !Equals(retriveValue.DeadLineDays, SKU.DEADLINE_DAYS_DEFAULT);
            bool isNewValueIncludesDeadLine = !Equals(newValue.DeadLineDays, SKU.DEADLINE_DAYS_DEFAULT);
            int? deadlineDays = (isRetriveValueIncludesDeadLine, isNewValueIncludesDeadLine) switch
            {
                (false, true) => newValue.DeadLineDays,
                (true, false) => retriveValue.DeadLineDays,
                (true, true) => new List<int>(){retriveValue.DeadLineDays, newValue.DeadLineDays}.Max(),
                (false, false) => null
            };

            // isParallel
            bool isParallel = retriveValue.IsParallelClientsSKU || newValue.IsParallelClientsSKU;

            return new SKU(clientLoginsList, marketplace, skuNumber, skuBrand, skuName, skuRansomsBalance, 
                skuRemainingBalance, skuWriteOffBalance, skuPaidShortageBalance, skuResortBalance!, averagePrice, maxRansomsDate, deadlineDays, isParallel);
        }

        private static int CombineIntValues(int retriveValue, int newValue, bool isSummary)
        {
            return isSummary && retriveValue == newValue ?
                retriveValue :
                retriveValue != newValue ?
                    retriveValue + newValue :
                    retriveValue;
        }
    }
}
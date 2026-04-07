using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizer.Objects.Repositories
//https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-add-and-remove-items
{
    public class SKURepo : BaseRepo<long, SKU>
    {
        public override bool RetrieveAndUpdateOrAddValue(long id, SKU item)
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

        private static SKU CreateNewSKU(SKU oldValue, SKU newValue)
        {
            // Logins
            List<string> clientLoginsList = [.. oldValue.ClientLogins
                        .Union(newValue.ClientLogins)
                        .OrderByDescending(el => el)];

            // Marketplace 
            Marketplace? marketplace = null;
            switch (oldValue.Marketplace.HasValue, newValue.Marketplace.HasValue)
            {
                case (false, true):
                    marketplace = newValue.Marketplace;
                    break;
                case (true, false):
                case (true, true):
                    marketplace = oldValue.Marketplace;
                    break;
            }
            ;

            // SKUNumber  
            long skuNumber = oldValue.SKUNumber;

            // SKUBrand
            bool isRetriveValueIncludesSKUBrand = !string.IsNullOrEmpty(oldValue.SKUBrand) && oldValue.SKUBrand != SKU.DEFAULT_BRAND;
            bool isNewValueIncludesSKUBrand = !string.IsNullOrEmpty(newValue.SKUBrand) && newValue.SKUBrand != SKU.DEFAULT_BRAND;
            string skuBrand = (isRetriveValueIncludesSKUBrand, isNewValueIncludesSKUBrand) switch
            {
                (false, true) => newValue.SKUBrand,
                (true, false) or (true, true) => oldValue.SKUBrand,
                _ => SKU.DEFAULT_BRAND
            };

            // SKUName
            bool isRetriveValueIncludesSKUName = !string.IsNullOrEmpty(oldValue.SKUName) && oldValue.SKUName != SKU.DEFAULT_NAME;
            bool isNewValueIncludesSKUName = !string.IsNullOrEmpty(newValue.SKUName) && newValue.SKUName != SKU.DEFAULT_NAME;
            string skuName = (isRetriveValueIncludesSKUName, isNewValueIncludesSKUName) switch
            {
                (false, true) => newValue.SKUName,
                (true, false) or (true, true) => oldValue.SKUName,
                _ => SKU.DEFAULT_NAME
            };

            // SKURansomsBalance
            SKURansomsBalance? skuRansomsBalance = null;
            switch (oldValue.SKURansomsBalance is not null, newValue.SKURansomsBalance is not null)
            {
                case (true, false):
                    skuRansomsBalance = oldValue.SKURansomsBalance;
                    break;

                case (false, true):
                    skuRansomsBalance = newValue.SKURansomsBalance;
                    break;

                case (true, true):
                    int? inTransitNumberPurchases = CombineIntValues(oldValue.SKURansomsBalance!.SKUInTransitNumberPurchases, newValue.SKURansomsBalance!.SKUInTransitNumberPurchases);
                    int? cancelledNumberPurchases = CombineIntValues(oldValue.SKURansomsBalance!.SKUCancelledNumberPurchases, newValue.SKURansomsBalance!.SKUCancelledNumberPurchases);
                    int? deliveredToPointsNumberPurchases = CombineIntValues(oldValue.SKURansomsBalance!.SKUDeliveredToPointsNumberPurchases, newValue.SKURansomsBalance!.SKUDeliveredToPointsNumberPurchases);
                    int? pickedupFromPointsNumberPurchases = CombineIntValues(oldValue.SKURansomsBalance!.SKUPickedupFromPointsNumberPurchases, newValue.SKURansomsBalance!.SKUPickedupFromPointsNumberPurchases);
                    int? unknownNumberPurchases = CombineIntValues(oldValue.SKURansomsBalance!.SKUPickedupFromPointsNumberPurchases, newValue.SKURansomsBalance!.SKUUnknownNumberPurchases);

                    skuRansomsBalance = new(inTransitNumberPurchases, cancelledNumberPurchases, deliveredToPointsNumberPurchases, pickedupFromPointsNumberPurchases, unknownNumberPurchases);

                    break;
            }

            // SKURemainingBalance
            int skuReceipt = CombineIntValues(oldValue.SKURemainingBalance?.SKUReceipt ?? 0, newValue.SKURemainingBalance?.SKUReceipt ?? 0);
            int skuShipmentGoods = CombineIntValues(oldValue.SKURemainingBalance?.SKUShipmentGoods ?? 0, newValue.SKURemainingBalance?.SKUShipmentGoods ?? 0);
            int skuTransfered = CombineIntValues(oldValue.SKURemainingBalance?.SKUTransfered ?? 0, newValue.SKURemainingBalance?.SKUTransfered ?? 0);
            int skuRemaining = CombineIntValues(oldValue.SKURemainingBalance?.SKURemaining ?? 0, newValue.SKURemainingBalance?.SKURemaining ?? 0);
            SKURemainingBalance skuRemainingBalance = new(number: skuNumber, receipt: skuReceipt, shipment: skuShipmentGoods, transfered: skuTransfered, remaining: skuRemaining);

            // SKUWriteOffBalance
            SKUWriteOffBalance? skuWriteOffBalance = null;
            switch (oldValue.SKUWriteOffBalance is not null, newValue.SKUWriteOffBalance is not null)
            {
                case (false, true):
                    skuWriteOffBalance = newValue.SKUWriteOffBalance;
                    break;

                case (true, true):
                case (true, false):

                    skuWriteOffBalance = oldValue.SKUWriteOffBalance;

                    if (newValue.SKUWriteOffBalance is not null)
                    {
                        int skuWriteOffPoints = oldValue.SKUWriteOffBalance!.SKUWriteOffPoints + newValue.SKUWriteOffBalance.SKUWriteOffPoints;
                        int shortage = oldValue.SKUWriteOffBalance!.SKUWriteOffPointsViaWarehouseShortage + newValue.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseShortage;
                        int adjustment = oldValue.SKUWriteOffBalance!.SKUWriteOffPointsViaWarehouseAdjustment + newValue.SKUWriteOffBalance.SKUWriteOffPointsViaWarehouseAdjustment;
                        skuWriteOffBalance = new SKUWriteOffBalance(skuNumber, skuWriteOffPoints, shortage, adjustment);
                    }

                    break;
            }

            // SKUPaidShortageBalance
            SKUPaidShortageBalance? skuPaidShortageBalance = null;
            switch (oldValue.SKUPaidShortageBalance is not null, newValue.SKUPaidShortageBalance is not null)
            {
                case (true, false):
                    skuPaidShortageBalance = oldValue.SKUPaidShortageBalance;
                    break;

                case (false, true):
                    skuPaidShortageBalance = newValue.SKUPaidShortageBalance;
                    break;

                case (true, true):
                    skuPaidShortageBalance!.Update(newValue.SKUPaidShortageBalance!.PaidShortageItems);
                    break;
            }

            // SKUResortBalance
            SKUResortBalance? skuResortBalance = null;
            switch (oldValue.SKUResortBalance is not null, newValue.SKUResortBalance is not null)
            {
                case (false, true):
                    skuResortBalance = newValue.SKUResortBalance!;
                    break;
                case (true, true):
                case (true, false):
                    skuResortBalance = oldValue.SKUResortBalance!;

                    if (newValue.SKUResortBalance is not null)
                    {
                        var resortSKUsInNewValue = newValue.SKUResortBalance.SKUsToResort;
                        if (resortSKUsInNewValue is not null && resortSKUsInNewValue.Count > 0)
                        {
                            skuResortBalance!.Update(resortSKUsInNewValue);
                        }
                    }
                    break;
            }

            // AveragePrice
            bool isRetriveValueIncludesPrice = !Equals(oldValue.AveragePrice, SKU.AVERAGE_PRICE_DEFAULT);
            bool isNewValueIncludesPrice = !Equals(newValue.AveragePrice, SKU.AVERAGE_PRICE_DEFAULT);
            double? averagePrice = (isRetriveValueIncludesPrice, isNewValueIncludesPrice) switch
            {
                (false, true) => newValue.AveragePrice,
                (true, false) => oldValue.AveragePrice,
                (true, true) => double.Min(oldValue.AveragePrice, newValue.AveragePrice),
                (false, false) => null
            };

            // MaxRansomsDate
            bool isRetriveValueIncludesDate = !Equals(oldValue.MaxRansomsDate, SKU.DATEONLY_DEFAULT);
            bool isNewValueIncludesDate = !Equals(newValue.MaxRansomsDate, SKU.DATEONLY_DEFAULT);
            DateOnly? maxRansomsDate = (isRetriveValueIncludesDate, isNewValueIncludesDate) switch
            {
                (false, true) => newValue.MaxRansomsDate,
                (true, false) => oldValue.MaxRansomsDate,
                (true, true) => new List<DateOnly>() { oldValue.MaxRansomsDate, newValue.MaxRansomsDate }.Max(),
                (false, false) => null
            };

            // DeadLine
            bool isRetriveValueIncludesDeadLine = !Equals(oldValue.DeadLineDays, SKU.DEADLINE_DAYS_DEFAULT);
            bool isNewValueIncludesDeadLine = !Equals(newValue.DeadLineDays, SKU.DEADLINE_DAYS_DEFAULT);
            int? deadlineDays = (isRetriveValueIncludesDeadLine, isNewValueIncludesDeadLine) switch
            {
                (false, true) => newValue.DeadLineDays,
                (true, false) => oldValue.DeadLineDays,
                (true, true) => new List<int>() { oldValue.DeadLineDays, newValue.DeadLineDays }.Max(),
                (false, false) => null
            };

            // isParallel
            bool isParallel = oldValue.IsParallelClientsSKU || newValue.IsParallelClientsSKU;

            return new SKU(clientLoginsList, marketplace, skuNumber, skuBrand, skuName, skuRansomsBalance,
                skuRemainingBalance, skuWriteOffBalance, skuPaidShortageBalance, skuResortBalance!, averagePrice, maxRansomsDate, deadlineDays, isParallel);
        }

        private static int CombineIntValues(int retriveValue, int newValue)
        {
            return (retriveValue == newValue) switch
            {
                true => retriveValue,
                false => retriveValue + newValue
            };

            // return retriveValue == newValue ?
            //     retriveValue :
            //     retriveValue != newValue ?
            //         retriveValue + newValue :
            //         retriveValue;
        }
    }
}
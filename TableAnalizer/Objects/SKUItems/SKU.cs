using TableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizer.Objects.SKUItems
{
    public class SKU
    {
        #region  Fields 
        public static readonly DateOnly DATEONLY_DEFAULT = new(0001,01,01);
        public static readonly double AVERAGE_PRICE_DEFAULT = 0.00;
        public static readonly int DEADLINE_DAYS_DEFAULT = 30;

        public const string DEFAULT_BRAND = "No brand";
        public const string DEFAULT_NAME = "No brand";
        #endregion Variables and constants

        #region Properties
        public List<string> ClientLogins { get; private set; } = [];
        public Marketplace? Marketplace { get; private set; }
        public long SKUNumber { get; }
        public string SKUBrand { get; private set; } = DEFAULT_BRAND;
        public string SKUName { get; private set; } = DEFAULT_NAME;
        public SKURansomsBalance? SKURansomsBalance { get; private set;}
        public SKURemainingBalance? SKURemainingBalance { get; private set;}
        public SKUWriteOffBalance? SKUWriteOffBalance { get; private set;}
        public SKUPaidShortageBalance? SKUPaidShortageBalance { get; private set; }
        public SKUResortBalance? SKUResortBalance { get; private set; }
        public double AveragePrice { get; private set;} = AVERAGE_PRICE_DEFAULT;
        public DateOnly MaxRansomsDate { get; private set;} = DATEONLY_DEFAULT;
        public int DeadLineDays { get;  set; } = DEADLINE_DAYS_DEFAULT;
        public bool IsParallelClientsSKU {get;set;} = false;

        #endregion

        #region Constructors && destructors

        // Constructor for ransoms
        public SKU(Marketplace? marketplace, string? clientLogin, long skuNumber, string? skuBrandAndName, SKURansomsBalance skuRansomsBalance, double averagePrice, DateOnly maxRansomsDate, int deadlineDays, bool isRemoveBrandFromName)
        {
            if(!string.IsNullOrEmpty(clientLogin))
                ClientLogins.Add(clientLogin);

            Marketplace = marketplace;
            SKUNumber = skuNumber;

            if(!string.IsNullOrEmpty(skuBrandAndName))
                ParsingSKUBrandAndName(skuBrandAndName, isRemoveBrandFromName);

            SKURansomsBalance = skuRansomsBalance;
            AveragePrice = averagePrice;
            MaxRansomsDate = maxRansomsDate;

            DeadLineDays = deadlineDays;
        }

        // Constructor for remainingBalance 
        public SKU(long skuNumber, SKURemainingBalance skuRemainingBalance)
        {
            SKUNumber = skuNumber;
            SKURemainingBalance = skuRemainingBalance;
        }

        private void ParsingSKUBrandAndName(string skuBrandAndNameString, bool isRemoveBrandFromName)
        {
            // Если есть разделитель /
            // biidi / Говорящая детские книга для малышей - Алфавит и цифры, Общая характеристика товара			
            if (skuBrandAndNameString.Contains('/'))
            {
                string[]? skuBrandAndName = skuBrandAndNameString.Split('/');

                switch (skuBrandAndName.Length)
                {
                    case 1:
                        SKUName = isRemoveBrandFromName ? skuBrandAndName[0].Trim() : skuBrandAndNameString.Trim();
                        break;
                    case 2: case 3: case 4:
                        SKUBrand = skuBrandAndName[0].Trim();
                        SKUName = isRemoveBrandFromName ? skuBrandAndName[1].Trim() : skuBrandAndNameString.Trim();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                SKUName = skuBrandAndNameString.Trim();
            }

            // Remove ", Общая характеристика товара"
            string[]? SKUNameWithOtherInfо = SKUName.Split(',');
            if (SKUNameWithOtherInfо.Last() == " Общая характеристика товара")
            {
                SKUNameWithOtherInfо = [.. SKUNameWithOtherInfо.Take(SKUNameWithOtherInfо.Length - 1)];
                SKUName = string.Join("", SKUNameWithOtherInfо);
            }
        }

        // Constructor for generating a new entity from repository
        public SKU(List<string> clientLogins, Marketplace? marketplace,  long skuNumber, string skuBrand, string skuName, 
            SKURansomsBalance? skuRansomsBalance, SKURemainingBalance? skuRemainingBalance, 
            SKUWriteOffBalance? skuWriteOffBalance, SKUPaidShortageBalance? skuPaidShortageBalance, 
            SKUResortBalance skuResortBalance, double? averagePrice, 
            DateOnly? maxRansomsDate, int? deadLineDays, bool isParallel) 
       {
            ClientLogins = clientLogins;
            Marketplace = marketplace;
            SKUNumber = skuNumber;
            SKUBrand = skuBrand ?? string.Empty;
            SKUName = skuName ?? string.Empty;
            SKURansomsBalance = skuRansomsBalance;
            SKURemainingBalance = skuRemainingBalance;
            SKUWriteOffBalance = skuWriteOffBalance;
            SKUPaidShortageBalance = skuPaidShortageBalance;
            SKUResortBalance = skuResortBalance;

            if(averagePrice.HasValue && !Equals(averagePrice, AVERAGE_PRICE_DEFAULT)) 
                AveragePrice = averagePrice.Value;
            
            if(maxRansomsDate.HasValue && !Equals(maxRansomsDate, DATEONLY_DEFAULT)) 
                MaxRansomsDate = maxRansomsDate.Value;
            
            if( deadLineDays.HasValue && !Equals(deadLineDays, DEADLINE_DAYS_DEFAULT)) 
                DeadLineDays = deadLineDays.Value;
            

            IsParallelClientsSKU = isParallel;
        }

        // Constructor for paid shortage balance 
        public SKU(long skuNumber, SKUPaidShortageBalance skuPaidShortageBalance) 
        {
            SKUNumber = skuNumber;
            SKUPaidShortageBalance = skuPaidShortageBalance;
        }

        // Constructor for writeOffBalance 
        public SKU(long skuNumber, SKUWriteOffBalance writeOffBalance) 
        {
            SKUNumber = skuNumber;
            SKUWriteOffBalance = writeOffBalance;
        }

        // Constructor for resortBalance 
        public SKU(long skuNumber, SKUResortBalance skuResortBalance) 
        {
            SKUNumber = skuNumber;
            SKUResortBalance = skuResortBalance;
        }

         // Constructor for parallel clients SKU 
        public SKU(long skuNumber, bool isParallelClientsSKU) 
        {
            SKUNumber = skuNumber;
            IsParallelClientsSKU = isParallelClientsSKU;
        }
     
        #endregion
    }
}
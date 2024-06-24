using TableAnalizer.Enums;
using TableAnalizer.Objects.SKUItems.SKUBalances;
using TableAnalizer.Objects.SKUItems.SKUBalances.SKUDummiesBalance;

namespace TableAnalizer.Objects.SKUItems
{
    /// <remarks><i>New version.</i></remarks>
    public class SKU
    {
        #region  Variables and constants 
        public static readonly DateOnly DATEONLY_DEFAULT = new(0001,01,01);
        public static readonly double AVERAGE_PRICE_DEFAULT = 0.00;
        public static readonly int DEADLINE_DAYS_DEFAULT = 30;
        #endregion Variables and constants

        #region Properties
        public Marketplace? Marketplace { get; private set; }
        public int SKUNumber { get; }
        public string SKUBrand { get; private set; } = string.Empty;
        public string SKUName { get; private set; } = string.Empty;
        public SKURansomsBalance? SKURansomsBalance { get; private set;}
        public SKURemainingBalance? SKURemainingBalance { get; private set;}
        public SKUWriteOffBalance? SKUWriteOffBalance { get; private set;}
        public SKUCompensationBalance? SKUCompensationBalance { get; private set; }
        public SKUDummiesBalance? SKUDummiesBalance { get; private set; }
        public SKUResortBalance? SKUResortBalance { get; private set; }
        public double AveragePrice { get; private set;} = AVERAGE_PRICE_DEFAULT;
        public DateOnly MaxRansomsDate { get; private set;} = DATEONLY_DEFAULT;
        public int DeadLineDays { get;  set; } = DEADLINE_DAYS_DEFAULT;

        #endregion

        #region Constructors && destructors

        // Constructor for ransoms
        public SKU(Marketplace? marketplace, int skuNumber, SKURansomsBalance skuRansomsBalance, double averagePrice, DateOnly maxRansomsDate, int deadlineDays)
        {
            SKUNumber = skuNumber;
            SKUBrand = string.Empty; SKUName = string.Empty;

            Marketplace = marketplace;
            SKURansomsBalance = skuRansomsBalance;
            AveragePrice = averagePrice;
            MaxRansomsDate = maxRansomsDate;

            DeadLineDays = deadlineDays;
        }

        // Constructor for remainingBalance 
        public SKU(Marketplace? marketplace, int skuNumber, SKURemainingBalance skuRemainingBalance)
        {
            Marketplace = marketplace;
            SKUNumber = skuNumber;

            SKURemainingBalance = skuRemainingBalance;

            var skuBrandAndName = skuRemainingBalance.SKUBrandAndName.Split('/');

            switch(skuBrandAndName.Length) 
            {
                case 1:
                    SKUName = skuBrandAndName[0].Trim();
                    break;
                case 2: case 3: case 4:
                    SKUBrand = skuBrandAndName[0].Trim();
                    SKUName = skuBrandAndName[1].Trim(); 
                    break;
                default:
                    break;
            }
        }

        // Constructor for generating a new entity from repository
        public SKU(Marketplace? marketplace, int skuNumber, string skuBrand, string skuName, 
            SKURansomsBalance? skuRansomsBalance, SKURemainingBalance? skuRemainingBalance, 
            SKUWriteOffBalance? skuWriteOffBalance, SKUCompensationBalance? skuCompensationBalance, 
            SKUResortBalance skuResortBalance, SKUDummiesBalance? skuDummiesBalance, double? averagePrice, 
            DateOnly? maxRansomsDate, int? deadLineDays) 
       {
            Marketplace = marketplace;
            SKUNumber = skuNumber;
            SKUBrand = skuBrand ?? string.Empty;
            SKUName = skuName ?? string.Empty;
            SKURansomsBalance = skuRansomsBalance;
            SKURemainingBalance = skuRemainingBalance;
            SKUWriteOffBalance = skuWriteOffBalance;
            SKUCompensationBalance = skuCompensationBalance;
            SKUDummiesBalance = skuDummiesBalance;
            SKUResortBalance = skuResortBalance;

            if(averagePrice.HasValue && !Equals(averagePrice, AVERAGE_PRICE_DEFAULT)) 
            {
                AveragePrice = averagePrice.Value;
            }

            if(maxRansomsDate.HasValue && !Equals(maxRansomsDate, DATEONLY_DEFAULT)) 
            {
                MaxRansomsDate = maxRansomsDate.Value;
            }

            if( deadLineDays.HasValue && !Equals(deadLineDays, DEADLINE_DAYS_DEFAULT)) 
            {
                DeadLineDays = deadLineDays.Value;
            }
        }

        // Constructor for compensationBalance 
        public SKU(int skuNumber, SKUCompensationBalance skuCompensationBalance) 
        {
            SKUNumber = skuNumber;
            SKUCompensationBalance = skuCompensationBalance;
        }

        // Constructor for dummiesBalance 
        public SKU(int skuNumber, SKUDummiesBalance skuDummiesBalance) 
        {
            SKUNumber = skuNumber;
            SKUDummiesBalance = skuDummiesBalance;
        }

        // Constructor for writeOffBalance 
        public SKU(int skuNumber, Marketplace marketplace, SKUWriteOffBalance writeOffBalance) 
        {
            SKUNumber = skuNumber;
            Marketplace = marketplace;
            SKUWriteOffBalance = writeOffBalance;
        }

        // Constructor for resortBalance 
        public SKU(int skuNumber, SKUResortBalance skuResortBalance) 
        {
            SKUNumber = skuNumber;
            SKUResortBalance = skuResortBalance;
        }

        public override string ToString() =>
            $"{SKUNumber} ({Marketplace}): '{SKUBrand} / {SKUName}', {AveragePrice}. Date: {MaxRansomsDate}\n{SKURansomsBalance}";

        #endregion

        #region Functionality

        public void Update(SKUCompensationBalance skuCompensationBalance) 
        {
            if(skuCompensationBalance is null) 
            {
                SKUCompensationBalance = skuCompensationBalance;
            } 
            else 
            {
                skuCompensationBalance.Update(skuCompensationBalance.Count, skuCompensationBalance.Sum, skuCompensationBalance.Comments);
            }
        }


        #endregion
    }
}
using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems
{
    public struct SKURedempionBalanceUnit 
    {
        #region Properties
        public SKURedemptionStatus Status {get; set;}
        public string Describtion {get; set;}
        public int Count {get; set;}
        #endregion

        #region Constructors
        public SKURedempionBalanceUnit(SKURedemptionStatus status, string describtion, int count)
        {
            Status = status; Describtion = describtion; Count = count;
        }
        #endregion
    }
}
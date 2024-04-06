namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    public struct SKUStorageBalanceUnit
    {
        #region Properties
        public string Describtion { get; set; }
        public int Count { get; set; }
        #endregion

        #region Constructors
        public SKUStorageBalanceUnit(string describtion, int count)
        {
            Describtion = describtion; Count = count;
        }
        #endregion
    }
}
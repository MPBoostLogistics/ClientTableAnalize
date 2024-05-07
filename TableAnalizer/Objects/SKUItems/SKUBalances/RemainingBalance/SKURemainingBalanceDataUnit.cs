using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Data unit for forming SKURemainingBalance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURemainingBalanceDataUnit : ParsingEntity
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public string SKUName { get; private set; } = String.Empty;
        public int SKUReceipt { get; private set; }   // Приход товаров
        public int SKURemaining { get; private set; } // Остаток товаров 
        #endregion

        #region  Constructors   
        public SKURemainingBalanceDataUnit(in Dictionary<string, ParsingItem> parsingElements) :
            base(parsingElements) {}
        override public string ToString() => $"number: {SKUNumber}, name: {SKUName}, receipt: {SKUReceipt}, remaining: {SKURemaining}";
        #endregion
    }
}
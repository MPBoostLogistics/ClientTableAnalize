using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Data unit for forming SKURemainingBalance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURemainingBalanceDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public string SKUBrandAndName { get; private set; } = String.Empty;
        public int SKUReceipt { get; private set; }   // Приход товаров
        public int SKURemaining { get; private set; } // Остаток товаров 
        #endregion

        #region  Constructors   
        public SKURemainingBalanceDataUnit(in Dictionary<string, ItemForParsing> parsingElements) :
            base(parsingElements) {}
        override public string ToString() => $"number: {SKUNumber}, brand and name: {SKUBrandAndName}, receipt: {SKUReceipt}, remaining: {SKURemaining}";
        #endregion
    }
}
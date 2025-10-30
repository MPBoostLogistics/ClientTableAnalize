using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Data unit for forming SKURemainingBalance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURemainingBalanceDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUNumber { get; private set; }
        public string SKUBrandAndName { get; private set; } = string.Empty;
        public int SKUReceipt { get; private set; }   // Приход товаров
        public int SKURemaining { get; private set; } // Остаток товаров 
        #endregion
    }
}
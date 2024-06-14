using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Shipped data unit for forming SKURemainingBalance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    public class SKUShippedDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public int SKUShippedCount { get; private set; }    // Расход товаров
        #endregion

        #region  Constructors   
        public SKUShippedDataUnit(in Dictionary<string, ItemForParsing> parsingElements) :
            base(parsingElements) {}
        override public string ToString() => $"number: {SKUNumber}, shipped: {SKUShippedCount}";
        #endregion
    }
}
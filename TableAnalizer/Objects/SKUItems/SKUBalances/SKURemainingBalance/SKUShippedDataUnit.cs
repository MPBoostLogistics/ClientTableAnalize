using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Shipped data unit for forming SKURemainingBalance.
    /// </summary>
    public class SKUShippedDataUnit(in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUNumber { get; private set; }
        public int SKUShippedCount { get; private set; }

        #endregion
    }
}
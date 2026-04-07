using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUWriteOffDataUnit(in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUNumber { get; private set; }
        public int SKUWriteOffCount { get; private set; }
        #endregion
    }
}
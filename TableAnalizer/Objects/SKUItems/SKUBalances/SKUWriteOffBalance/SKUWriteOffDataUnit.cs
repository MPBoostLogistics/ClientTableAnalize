using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUWriteOffDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public int SKUWriteOffCount { get; private set; }
        #endregion
    }
}
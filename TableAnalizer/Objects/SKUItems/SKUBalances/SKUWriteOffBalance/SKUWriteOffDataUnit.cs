using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUWriteOffDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public int SKUWriteOffCount { get; private set; }    // Списано недостачами
        #endregion

        #region Constructors
        public SKUWriteOffDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements){}
        #endregion
    }
}
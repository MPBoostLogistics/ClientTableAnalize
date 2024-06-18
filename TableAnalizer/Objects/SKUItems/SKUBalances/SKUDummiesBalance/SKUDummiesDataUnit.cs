using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUDummiesDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; }
        public int DummiesSKUCount { get; }
        public string? DummiesComment { get; }
        #endregion Properties

        #region Constructors
        public SKUDummiesDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements){}
        #endregion Constructors

        #region Functionality
        public override string ToString()
        {
            return $"{SKUNumber}, {DummiesSKUCount}, {DummiesComment}";
        }
        #endregion
    }
}
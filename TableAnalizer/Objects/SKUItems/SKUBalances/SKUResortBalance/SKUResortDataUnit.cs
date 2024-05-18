using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances 
{
    /// <summary>
    /// Resort data unit for forming SKUResortBalance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    class SKUResortDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; }
        public int ResortSKUNumber { get; }
        public int ResortSKUCount { get; }
        public string? ResortComment { get; }
        #endregion Properties

        #region Constructors
        public SKUResortDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements){}
        #endregion Constructors
    }
}
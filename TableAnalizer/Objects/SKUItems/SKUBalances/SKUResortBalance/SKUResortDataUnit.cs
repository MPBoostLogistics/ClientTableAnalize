using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances 
{
    /// <summary>
    /// Resort data unit for forming SKUResortBalance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    public class SKUResortDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; set;}
        public int ResortSKUNumber { get; set;}
        public int ResortSKUCount { get; set; }
        public string? ResortComment { get; set;}
        #endregion Properties

        #region Constructors
        public SKUResortDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements){}
        #endregion Constructors
    }
}
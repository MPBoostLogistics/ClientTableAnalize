using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance 
{
    /// <summary>
    /// Resort data unit for forming SKUResortBalance.
    /// </summary>
    public class SKUResortDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public int SKUFromResortNumber { get; set;}
        public int SKUToResortNumber { get; set;}
        public int SKUToResortNumberCount { get; set; }
        public string? ResortComment { get; set;}
        #endregion Properties
    }
}
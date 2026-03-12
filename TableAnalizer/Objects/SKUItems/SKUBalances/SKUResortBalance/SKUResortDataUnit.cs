using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance
{
    /// <summary>
    /// Resort data unit for forming SKUResortBalance.
    /// </summary>
    public class SKUResortDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUNumber { get; set; }
        public int SKUResortCount { get; set; }
        public string? ResortComment { get; set; }
        #endregion Properties

        #region Functionality
        public override string ToString()
        {
            return $"Resort: SKU {SKUNumber}, count: {SKUResortCount}, comment: {ResortComment}";
        }
        #endregion
    }
}
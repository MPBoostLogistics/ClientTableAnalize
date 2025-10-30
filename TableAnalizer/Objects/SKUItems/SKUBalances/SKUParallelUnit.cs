using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Parallel data unit.
    /// </summary>
    public class SKUParallelUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        /// <summary>
        /// Артикул товара
        /// </summary>
        public long SKUNumber { get; set; }
        #endregion Properties


        #region Functionality
        public override string ToString()
        {
            return $"SKUParallelUnit: {SKUNumber}";
        }
        #endregion
    }
}
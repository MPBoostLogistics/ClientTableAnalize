using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Parallel data unit.
    /// </summary>
    /// <remarks><i>New version</i></remarks>

    public class SKUParallelUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; set; }               // Артикул товара

        #endregion Properties

        #region Constructors
        public SKUParallelUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements){}
        #endregion Constructors

        #region Functionality
        public override string ToString()
        {
            return $"{SKUNumber}";
        }
        #endregion
    }
}
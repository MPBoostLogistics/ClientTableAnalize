using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUDummiesBalance
{
    /// <summary>
    /// Dummies data unit for forming SKUDummiesBalance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>

    public class SKUDummiesDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; set; }               // Артикул товара
        public int DummiesSKUCount { get; set; }         // Количество товаров в пустышках
        public string? DummiesComment { get; set; }      // Комментарий
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
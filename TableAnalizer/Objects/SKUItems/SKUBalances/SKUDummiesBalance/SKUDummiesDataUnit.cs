using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUDummiesDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; }               // Артикул товара
        public int DummiesSKUCount { get; }         // Количество товаров в пустышках
        public string? DummiesComment { get; }      // Комментарий
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
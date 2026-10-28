using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUResortBalance
{
    /// <summary>
    /// Сущность для ручной пересортицы расхода товаров.
    /// </summary>
    /// <param name="parsingElements"></param>
    public class SKUResortUnit(in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties

        /// <summary>
        /// Артикул списываемого товара.
        /// </summary>
        public long SKUItemWrittenOffNumber { get; set; }
        /// <summary>
        /// Артикул приходуемого товара.
        /// </summary>
        public long SKUItemAddedNumber { get; set; }
        /// <summary>
        ///  Количество товаров в пересортице, шт
        /// </summary>
        public int SKUResortSum { get; set; }
        /// <summary>
        /// Комментарий к пересортице.
        /// </summary>
        public string? SKUResortComment { get; set; }
        #endregion Properties
    }
}
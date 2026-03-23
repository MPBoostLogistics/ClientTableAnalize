using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems
{
    /// <summary>
    /// Сущность для хранения единиц экстра-приходов (перемещения, пересорты)
    /// </summary>
    public class SKUExtraReceiptDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUNumber { get; set; }
        public int SKUExtraReceiptCount { get; set; }
        public string? ResortComment { get; set; }
        #endregion Properties

        #region Functionality
        public override string ToString()
        {
            return $"Resort: SKU {SKUNumber}, count: {SKUExtraReceiptCount}, comment: {ResortComment}";
        }
        #endregion
    }
}
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances.SKUCompensationBalance
{
    /// <summary>
    /// Compensation data unit for forming SKUCompensationBalance.
    /// </summary>
    public class SKUCompensationDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public int SKUNumber { get; set;}                  // Артикул товара
        public int CompensationCount { get; set;}          // Количество товаров в недостаче  
        public double CompensationSum { get; set;}         // Сумма недостачи  
        public string? CompensationComment { get; set;}    // Комментарий (опционально)
        #endregion
    }
}

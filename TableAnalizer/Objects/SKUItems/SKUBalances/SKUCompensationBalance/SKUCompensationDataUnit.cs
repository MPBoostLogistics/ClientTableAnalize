using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Compensation data unit for forming SKUCompensationBalance.
    /// </summary>
    /// <remarks><i>New version</i></remarks>
    public class SKUCompensationDataUnit : ParsingRowEntity
    {
        #region Properties
        public int SKUNumber { get; set;}                  // Артикул товара
        public int CompensationCount { get; set;}          // Количество товаров в недостаче  
        public double CompensationSum { get; set;}         // Сумма недостачи  
        public string? CompensationComment { get; set;}    // Комментарий (опционально)
        #endregion

        #region Constructors
        public SKUCompensationDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements) {}
        #endregion Constructors
    }
}

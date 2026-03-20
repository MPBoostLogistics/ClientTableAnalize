using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Сущность для корректировок расхода по данным от клиента
    /// </summary>
    /// <param name="parsingElements"></param>
    public class SKUAdjustmentShipmentUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUSourceNumber { get; set; }
        public int SKUAdjustmentSum { get; set; }
        public string? SKUAdjustmentComment { get; set; }
        #endregion Properties

        #region Functionality
        public override string ToString()
        {
            return $"SKUAdjustment: {SKUSourceNumber} - {SKUAdjustmentSum}, comment: {SKUAdjustmentComment}";
        }

        #endregion
    }
}
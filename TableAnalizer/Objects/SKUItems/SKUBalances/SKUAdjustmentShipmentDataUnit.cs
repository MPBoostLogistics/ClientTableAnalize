using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Сущность для корректировок расхода по данным от клиента
    /// </summary>
    /// <param name="parsingElements"></param>
    public class SKUAdjustmentShipmentUnit(in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUSourceNumber { get; set; }
        public int SKUAdjustmentSum { get; set; }
        public string? SKUAdjustmentComment { get; set; }
        #endregion Properties

        #region Functionality


        #endregion
    }
}
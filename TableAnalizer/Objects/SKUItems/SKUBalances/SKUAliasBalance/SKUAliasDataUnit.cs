using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalance
{
    public class SKUAliasDataUnit(in Dictionary<string, TableParsingCell> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUSourceNumber { get; set; }
        public List<long> SKUAliasNumbers { get; set; } = [];
        public string? AliasComment { get; set; }
        #endregion Properties
    }
}
using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalance
{
    public class SKUAliasDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUSourceNumber { get; set; }
        public List<long> SKUAliasNumbers { get; set; } = [];
        public string? AliasComment { get; set; }
        #endregion Properties

        #region Functionality
        public override string ToString()
        {
            string combinedString = string.Join(",", SKUAliasNumbers.ToArray());
            return $"SKU: {SKUSourceNumber} - {combinedString}, comment: {AliasComment}";
        }

        #endregion
    }
}
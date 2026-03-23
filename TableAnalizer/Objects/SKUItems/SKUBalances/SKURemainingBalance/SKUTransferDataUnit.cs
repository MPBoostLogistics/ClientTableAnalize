using TableAnalizer.Objects.Managers.ExcelSheetParsing;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Transfer data unit for forming SKURemainingBalance.
    /// </summary>
    public class SKUTransferDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public long SKUNumber { get; private set; }
        public int SKUTransferedCount { get; private set; }  

        #endregion
        #region  Constructors   
        override public string ToString() => $"number: {SKUNumber}, transfered: {SKUTransferedCount}";
        #endregion
    }
}
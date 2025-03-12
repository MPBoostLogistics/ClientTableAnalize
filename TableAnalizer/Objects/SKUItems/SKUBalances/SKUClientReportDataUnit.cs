using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUClientReportDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements) 
    {
        #region Properties
        public Marketplace SKUMp { get; set; } 
        public int SKUNumber { get; set; } 
        public string? SKUBrand { get; set; } 
        public string? SKUName { get; set; } 
        public double? SKUPrice { get; set; } 
        public DateOnly SKURansomDate { get; set; } 
        public int SKURansomsTotal { get; set; } 
        public int SKURansomsCanceled { get; set; } 
        public int SKURansomsOnWay { get; set; } 
        public int SKURansomsDelivered { get; set; } 
        public int SKURansomsWithDrawn { get; set; } 
        public int SKUReceiptGoods { get; set; } 
        public int SKUPaidPoints { get; set; }
        public int SKUOutgoingProducts { get; set; } 
        public int SKUTransferDataUnit { get; set; } 
        public int SKUPaidWarehouse { get; set; }
        public int SKURemaining { get; set; }
        #endregion Properties
    }
}
    
   
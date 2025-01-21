using TableAnalizer.Objects.Enums;
using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    public class SKUClientReportDataUnit : ParsingRowEntity 
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
        public int SKUReceipt { get; set; } 
        public int SKUShipped { get; set; } 
        public int SKURemaining { get; set; } 
        #endregion Properties

        #region Constructors
        public SKUClientReportDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : base(parsingElements) {}
        #endregion Constructors

    }
}
    
   
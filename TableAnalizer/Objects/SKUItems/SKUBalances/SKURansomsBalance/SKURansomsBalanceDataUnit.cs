using TableAnalizer.Objects.Parsing;

namespace TableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Data unit for forming SKURansomsBalance.
    /// </summary>
    public class SKURansomsBalanceDataUnit(in Dictionary<string, ItemForParsing> parsingElements) : ParsingRowEntity(parsingElements)
    {
        #region Properties
        public int ClientLogin { get; set; }
        public Marketplace SKUMarketPlace { get; set; }
        public int SKUNumber { get;  set; }
        public string? SKUBrandAndName { get; set; }
        public double SKUAveragePrice { get;  set; }
        public DateOnly SKULastPurchaseDate { get;  set; }
        public int SKUTotalNumberPurchases { get; set; }
        public int SKUInTransitNumberPurchases { get; set; }
        public int SKUCancelledNumberPurchases { get; set; }
        public int SKUDeliveredToPointsNumberPurchases { get; set; }
        public int SKUPickedupFromPointsNumberPurchases { get; set; }
        public int SKUUnknownNumberPurchases { get; set; }

        #endregion
    }
}
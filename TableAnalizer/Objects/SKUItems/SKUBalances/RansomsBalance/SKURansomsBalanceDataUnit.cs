using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances
{
    /// <summary>
    /// Data unit for forming SKURansomsBalance.
    /// </summary>
    /// <remarks><i>New version, tested</i></remarks>
    public class SKURansomsBalanceDataUnit : ParsingEntity
    {
        #region Properties
        public int SKUNumber { get; private set; }
        public SKURansomsStatus? SKURansomsStatus { get; private set; }
        public double SKUCost { get; private set; }
        public DateOnly SKURansomDate { get; private set; }
        #endregion

        #region  Constructors   
        public SKURansomsBalanceDataUnit(in Dictionary<string, (string value, ParsingTypeCode parsingTypeCode)> parsingElements) :
            base(parsingElements) {}
        override public string ToString() =>
            $"number: {SKUNumber}, ransoms status: {SKURansomsStatus}, cost: {SKUCost}";
        
        #endregion

    }
}
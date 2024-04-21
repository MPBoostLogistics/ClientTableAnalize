using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances.Redemtpion
{
    public class SKURansomsBalanceUnit : ParsingEntity
    {
        #region Values and constants
        private const string SKU_RANSOM_STATUS= "SKURansomsStatus";
        #endregion

        #region Properties
        public int SKUNumber { get; private set; }
        public SKURansomsStatus Status { get; set; }
        public double SKUCost { get; private set; }
        #endregion

        #region  Constructors
        public SKURansomsBalanceUnit(in Dictionary<string, (string value, Type type)> parsingElements, string dateOnlyFormat = "dd.MM.yyyy") :
            base(parsingElements, dateOnlyFormat)
        {
            var statusStringValue = parsingElements.SingleOrDefault(el => el.Key == SKU_RANSOM_STATUS);
            if(statusStringValue.Value == default) 
                ThrowExceptionOnArgument(SKU_RANSOM_STATUS);
            
            MatchString(statusStringValue.Value.value);
        }

        private void MatchString(string value)
        {
            Status = value switch
            {
                "Доставлен на ПВЗ" => SKURansomsStatus.Delivered,
                "Забран с ПВЗ" => SKURansomsStatus.WithDrawn,
                "Отменен" => SKURansomsStatus.Canceled,
                "В пути" => SKURansomsStatus.OnWay,
                _ => SKURansomsStatus.Unknown,
            };
        }

        override public string ToString() =>
            $"number: {SKUNumber}, ransoms status: {Status}, cost: {SKUCost}";
        
        #endregion

    }
}
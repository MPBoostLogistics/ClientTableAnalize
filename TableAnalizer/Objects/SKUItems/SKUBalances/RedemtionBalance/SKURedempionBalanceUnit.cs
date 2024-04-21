using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.Parsing;

namespace ClientTableAnalizer.Objects.SKUItems.SKUBalances.Redemtpion
{
    // Артикул / Статус / Стоимость
    public class SKURedempionBalanceUnit : ParsingEntity
    {
        #region Values and constants
        private const string SKU_REDEMPION_STATUS= "SKURedemptionStatus";
        #endregion

        #region Properties
        public int SKUNumber { get; private set; }
        public SKURedemptionStatus Status { get; set; }
        public double SKUCost { get; private set; }
        #endregion

        #region  Constructors
        public SKURedempionBalanceUnit(in Dictionary<string, (string value, Type type)> parsingElements, string dateOnlyFormat = "dd.MM.yyyy") :
            base(parsingElements, dateOnlyFormat)
        {
            var statusStringValue = parsingElements.SingleOrDefault(el => el.Key == SKU_REDEMPION_STATUS);
            if(statusStringValue.Value == default) 
                ThrowExceptionOnArgument(SKU_REDEMPION_STATUS);
            
            MatchString(statusStringValue.Value.value);
        }

        private void MatchString(string value)
        {
            Status = value switch
            {
                "Доставлен на ПВЗ" => SKURedemptionStatus.Delivered,
                "Забран с ПВЗ" => SKURedemptionStatus.WithDrawn,
                "Отменен" => SKURedemptionStatus.Canceled,
                "В пути" => SKURedemptionStatus.OnWay,
                _ => SKURedemptionStatus.Unknown,
            };
        }

        override public string ToString() =>
            $"number: {SKUNumber}, redemtion status: {Status}, cost: {SKUCost}";
        
        #endregion

    }
}
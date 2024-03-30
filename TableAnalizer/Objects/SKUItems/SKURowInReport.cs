using System.Globalization;
using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.SKUItems
{
    public class SKURowInReport
    {
        #region Variables && constants
        private const string DATE_TIME_FORMAT = "dd.MM.yyyy HH:mm";
        private readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");
        #endregion

        #region Properties 
        public int SKUNumber { get; }
        public DateTime RedemptionDate { get; }             
        public SKURedemptionStatus ItemStatus { get; }
        public double Price { get; }
        #endregion
        
        #region Constructors
        public SKURowInReport(int skuNumber,  string redemptionDateString, string itemStatusString, string priceString)
        {
            SKUNumber = skuNumber;

            if (DateTime.TryParseExact(redemptionDateString, DATE_TIME_FORMAT, Culture, DateTimeStyles.None, out DateTime dtv)) 
            {
            RedemptionDate = dtv;
            } 
            else 
            {
                throw new Exception("Невозможно преобразовать строку в дату для ");
            }

            ItemStatus = SetItemStatus(itemStatusString);

            if(double.TryParse(priceString.Split(" ")[0], out double price))
                Price = price;
        }
        #endregion

        #region Functionality
        private static SKURedemptionStatus SetItemStatus(string statusText)
        {
            return statusText switch
            {
                "Доставлен на ПВЗ" => SKURedemptionStatus.Delivered,
                "Забран с ПВЗ" => SKURedemptionStatus.WithDrawn,
                "Отменен" => SKURedemptionStatus.Canceled,
                "В пути" => SKURedemptionStatus.OnWay,
                _ => SKURedemptionStatus.Unknown
            };
        }

        public override string ToString()
        {
            return $"{SKUNumber}: {RedemptionDate.ToShortDateString()}, {ItemStatus}, {Price}";
        }

        #endregion
    }
}
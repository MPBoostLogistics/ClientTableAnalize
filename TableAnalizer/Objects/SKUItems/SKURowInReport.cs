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
        public ArticleRedemptionStatus ItemStatus { get; }
        public double Price { get; }
        #endregion
        
        #region Constructors
        public SKURowInReport(int articleNumber,  string redemptionDateString, string itemStatusString, string priceString)
        {
            SKUNumber = articleNumber;

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
        private static ArticleRedemptionStatus SetItemStatus(string statusText)
        {
            return statusText switch
            {
                "Доставлен на ПВЗ" => ArticleRedemptionStatus.Delivered,
                "Забран с ПВЗ" => ArticleRedemptionStatus.WithDrawn,
                "Отменен" => ArticleRedemptionStatus.Canceled,
                "В пути" => ArticleRedemptionStatus.OnWay,
                _ => ArticleRedemptionStatus.Unknown
            };
        }

        public override string ToString()
        {
            return $"{SKUNumber}: {RedemptionDate.ToShortDateString()}, {ItemStatus}, {Price}";
        }

        #endregion
    }
}
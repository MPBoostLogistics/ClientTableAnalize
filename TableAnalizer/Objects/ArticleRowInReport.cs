using System.Globalization;
using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects
{
    public class ArticleRowInReport
    {
        #region Variables && constants
        private const string DATE_TIME_FORMAT = "dd.MM.yyyy HH:mm";
        private readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");
        #endregion

        #region Properties 
        public DateTime RedemptionDate { get; }             
        public ArticleRedemptionStatus ItemStatus { get; }
        public double Price { get; }
        #endregion
        
        #region Constructors
        public ArticleRowInReport(string redemptionDateString, string itemStatusString, string priceString)
        {
            RedemptionDate = DateTime.ParseExact(redemptionDateString, DATE_TIME_FORMAT, Culture);
            
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
            return $"{RedemptionDate.ToShortDateString()}, {ItemStatus}";
        }

        #endregion
    }
}
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
        public RedemptionStatus ItemStatus { get; }
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
        private static RedemptionStatus SetItemStatus(string statusText)
        {
            return statusText switch
            {
                "Доставлен на ПВЗ" => RedemptionStatus.Delivered,
                "Забран с ПВЗ" => RedemptionStatus.WithDrawn,
                "Отменен" => RedemptionStatus.Canceled,
                "В пути" => RedemptionStatus.OnWay,
                _ => RedemptionStatus.Unknown
            };
        }

        #endregion
    }
}
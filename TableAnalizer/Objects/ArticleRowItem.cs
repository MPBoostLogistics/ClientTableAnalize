using System.Globalization;

namespace ClientTableAnalizer.Objects
{
    public class ArticleRowItem
    {
        private const string DATE_TIME_FORMAT = "dd.MM.yyyy HH:mm";
        private readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

        public DateTime RedemptionDate { get; }             
        public RedemptionStatus ItemStatus { get; }
        public double Price { get; }

        public ArticleRowItem(string redemptionDate, string itemStatus, string priceString)
        {
            RedemptionDate = DateTime.ParseExact(redemptionDate, DATE_TIME_FORMAT, Culture);
            ItemStatus = SetItemStatus(itemStatus);

            var priceStringValue = priceString.Split(" ")[0];
            if(double.TryParse(priceStringValue, out double price))
            {
                Price = price;
            } 
        }

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

        public override string ToString()
        {
            return $"{RedemptionDate}, {ItemStatus}";
        }
    }
}
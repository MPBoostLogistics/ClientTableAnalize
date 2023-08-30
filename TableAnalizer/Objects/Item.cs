using System.Globalization;

namespace TableAnalizer.Objects
{
    internal class Item
    {
        private const string DATE_TIME_FORMAT = "dd.MM.yyyy HH:mm";

        public DateTime RedemptionDate { get; }            // 8
        public RedemptionStatus ItemStatus { get; }         // 10

        public Item(string redemptionDate, string itemStatus)
        {
            this.RedemptionDate = DateTime.ParseExact(redemptionDate, DATE_TIME_FORMAT, CultureInfo.GetCultureInfo("ru-RU"));
            this.ItemStatus = SetItemStatus(itemStatus);
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
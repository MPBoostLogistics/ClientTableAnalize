namespace ClientTableAnalizer.Enums
{
    public enum Marketplace { WB, OZ }

    public enum PathKey { Documents, MPBoostClients }

    public enum ShortagesPlace { Warehouse, PickupPoint }

    public enum ArticleStatus
    {
        FullSet,            // Полный расчет
        InWork,             // В работе
        OnSearch,           // На поиске
        OnSearchWarehouse,  // На поиске склада 
        OnSearchPoint,      // На поиске ПВЗ
        Unknown             // Неопределенный
    }

    public enum ItemBalanceIndex
    {
        Coming,             // Приход 
        Consumption,        // Расход
        FinBalance,         // Конечный остаток
        WriteoffShortages   // Принято на склад и списано по недостачам
    }


    public enum RedemptionStatus
    {
        Delivered,  // Доставлен на ПВЗ
        WithDrawn,  // Забран с ПВЗ
        Canceled,   // Отменен 
        OnWay,      // В пути
        Unknown     // Неизвестный
    }
}
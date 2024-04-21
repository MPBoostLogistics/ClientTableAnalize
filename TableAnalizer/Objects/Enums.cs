namespace ClientTableAnalizer.Enums
{
    public enum Marketplace { WB, OZ }

    public enum PathKey { Documents, MPBoostClients }

    public enum ShortagesPlace { Warehouse, PickupPoint }

    public enum ArticleSellmentStatus
    {
        FullSet,                    // Полный расчет
        InWork,                     // В работе
        OnSearchWarehouseAndPoints, // На поиске склада и ПВЗ
        OnSearchWarehouse,          // На поиске склада 
        OnSearchPoint,              // На поиске ПВЗ
        Unknown                     // Неопределенный
    }

    public enum ArticeStorageIndex
    {
        GoodsExpected,      // Ожидаемое количество товаров на складе
        GoodsReceived,      // Принято товаров
        GoodsShipped,       // Отгружено товарами
        GoodsRemaining,     // Остаток товаров на складе
        WriteoffShortages   // Принято на склад и списано по недостачам
    }

    public enum SourceSKUReportFileKey 
        {   
            RedemtionWB,        // выкупы WB
            RedemtionOZ,        // выкупы OZ
            RemainingWB,        // приходы и остатки WB
            RemainingOZ,        // приходы и остатки OZ
            Shortages,          // недостачи 
            WriteOffShortages,  // списание нелдостач общее 
            ReGrades,           // пересорты
            Shipped             // расходы товаром 
        }

    public enum SKURansomsStatus
    {
        Delivered,  // Доставлен на ПВЗ
        WithDrawn,  // Забран с ПВЗ
        Canceled,   // Отменен 
        OnWay,      // В пути
        Unknown     // Неизвестный
    }
}
namespace TableAnalizer.Enums
{
    public enum Marketplace { WB, OZ }

    public enum PathKey { Documents, MPBoostClients }

    public enum ShortagesPlace { Warehouse, PickupPoint }

    public enum SKUSellmentStatus
    {
        FullSet,                    // Полный расчет
        InWork,                     // В работе
        OnSearchWarehouseAndPoints, // На поиске склада и ПВЗ
        OnSearchWarehouse,          // На поиске склада 
        OnSearchPoints,              // На поиске ПВЗ
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
        RansomsWB,          // выкупы WB
        RansomsOZ,          // выкупы OZ
        RemainingWB,        // приходы и остатки WB
        RemainingOZ,        // приходы и остатки OZ
        WriteOffShortages,  // списание недростач общее 
        Additional,          // недостачи, пересорты и пустышки
        Shipped,             // расходы товаром 
        Transfer,            // перемещение (на фулфилмент) 
        Client               // клиентский отчет
    }

    public enum SourceSKUReportKey
    {
        RansomsWB,          // выкупы WB
        RansomsOZ,          // выкупы OZ
        RemainingWB,        // приходы и остатки WB
        RemainingOZ,        // приходы и остатки OZ
        Shipped,            // расходы товаром 
        WriteOffShortages,  // списание недростач общее 
        Compensation,       // оплаченные недостачи
        Resort,             // пересорты
        Dummies             // пустышки  
    }

    public enum SKURansomsStatus
    {
        Delivered,  // Доставлен на ПВЗ
        WithDrawn,  // Забран с ПВЗ
        Canceled,   // Отменен 
        OnWay      // В пути
    }

    public enum ParsingTypeCode
    {
        IntTypeCode,                // 0
        DoubleTypeCode,             // 1
        DataOnlyTypeCode,           // 2
        StringTypeCode,             // 3
        SKURansomsStatus,           // 4
        Marketplace,                // 5
        SellmentStatus              // 6
    }

    enum SourceCellsType 
    {
        Required,
        Filtration,
        NonFiltration
    }

}
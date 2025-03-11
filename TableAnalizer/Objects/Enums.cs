namespace TableAnalizer.Objects
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
        OnSearchPoints,             // На поиске ПВЗ
        Unknown                     // Неопределенный
    }

    public enum SourceSKUReportFileKey
    {
        /// <summary>
        /// Выкупы клиента
        /// </summary>
        Ransoms,
        /// <summary>
        /// Баланс (приходы и остатки)
        /// </summary>
        Balance,      
        /// <summary>
        /// Списано недостач с точек
        /// </summary>   
        WriteOffShortages,
        /// <summary>
        /// Недостачи и пересорты
        /// </summary>
        Additional,    
        /// <summary>
        /// Расходы товаром. 
        /// </summary>
        OutgoingProducts,      
        /// <summary>
        /// Перемещния на фулфилмент
        /// </summary>       
        Transfered            
    }

    public enum SKURansomsStatus
    {
        /// <summary>
        /// Покупки доставлены на точки
        /// </summary>
        DeliveredToPoints, 
        /// <summary>
        /// Покупки забраны с точек 
        /// </summary>
        PickedupFromPoints,  
        /// <summary>
        /// Покупки отменены
        /// </summary>
        Canceled,  
        /// <summary>
        /// Покупки в пути
        /// </summary>
        InTransit,
        /// <summary>
        /// Неизвестный статус покупок
        /// </summary>
        Unknown
        
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

    public enum SourceCellsType 
    {
        Required,
        Filtration,
        NonFiltration
    }

    public enum SKUResortBalanceTarget 
    {
        /// <summary>
        /// Пересорттица товарами
        /// </summary>
        ResortingConsumption
    }

    public enum CompensationTarget 
    {
        /// <summary>
        /// Компенсация от склада
        /// </summary>
        Warehouse, 
        /// <summary>
        /// Компенсация от точек
        /// </summary>
        PickupPoint
    }

    public enum TargetReportType
    {
        Main, Delay, Overruns, Analytics
    }

    public enum TypesFormulas 
    {
        Sum
    }

    public enum TargetReportRecipientType 
    {
        /// <summary>
        /// Внутренний отчет для работы.
        /// </summary>
        Internal, 
        /// <summary>
        /// Итоговый отчет для клтента.
        /// </summary>
        External
    }
}
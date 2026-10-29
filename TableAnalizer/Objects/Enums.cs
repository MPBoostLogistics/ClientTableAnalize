namespace TableAnalizer.Objects
{
    /// <summary>
    /// Тип первичных данных для отчетов.
    /// </summary>
    public enum PrimaryDataType
    {
        /// <summary>
        /// Постоянные данные (например, сведения об оплатах или списаниях, актуальные для всех Клиентов)
        /// </summary>
        Permanent,
        /// <summary>
        /// Переменные данные (например, заказы, приходы и расходы по конкретному Клиенту)
        /// </summary>
        Variable
    }

    public enum ConstantDataSource
    {
        Directory
    }

    public enum Marketplace { WB, OZ }

    public enum PathKey { Documents, MPBoostClients }

    public enum ShortagesPlace { Warehouse, PickupPoint }

    public enum SKUSellmentStatus
    {
        /// <summary>
        /// Полный расчет
        /// </summary>
        FullSet,
        /// <summary>
        /// В работе
        /// </summary>
        InWork,
        /// <summary>
        /// Задержка от точек и склада.
        /// </summary>
        DelayFromPointsAndWarehouse,
        /// <summary>
        /// Задержка от склада.
        /// </summary>
        DelayFromWarehouse,
        /// <summary>
        /// Задержка от организации и склада.
        /// </summary>
        DelayFromOrganizationAndWarehouse,
        /// <summary>
        /// Задержка от организации.
        /// </summary>
        DelayFromOrganization,
        /// <summary>
        /// Задержка довоза от точек.
        /// </summary>
        DelayDeliveryFromPoints,
        /// <summary>
        /// Неопределенный
        /// </summary>
        Unknown
    }

    public enum SourceReportItemKey
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
        /// Списание недостач с точек через склад
        /// Прошлый алгоритм списания точек через склад.
        /// Write-off - points through the warehouse shortages.xlsx
        /// </summary>
        WriteOffPointsViaWarehouseShortage,
        /// <summary>
        /// Списание корректировок с точек через склад
        /// </summary>
        WriteOffPointsViaWarehouseAdjustment,
        /// <summary>
        /// Списание недостач с точек напрямую
        /// </summary>
        WriteOffPoints,
        /// <summary>
        /// Оплаченные недостачи
        /// </summary>
        Additional,
        /// <summary>
        /// Расходы товаром. 
        /// </summary>
        OutgoingProducts,
        /// <summary>
        /// Перемещния на фулфилмент
        /// </summary>       
        Transfered,
        /// <summary>
        /// Недостачи, оплаченые Клиенту организацией и склодом наппрямую
        /// </summary>
        /// <remarks>
        /// Копируется из Google Sheets "Учет клиентов логистики", лист "Перетензии"
        /// </remarks>
        ShortagesPaid,
        /// <summary>
        /// Списание недостач с точек и курьеров по решению организации
        /// </summary>
        /// <remarks>
        /// 1С: Склад и доставка – Излишки, недостачи, порчи – Складские акты
        /// Настройки списка: 
        /// - Склад не в списке: Основной склад, Производства (2)
        ///- Тип: Списание недостач товаров
        /// - Комментарий: содержит «Списание оплаченных недовозов»
        /// Выделить все, Печать: Акт о списании товаров
        /// Имя файла: Write-off - points and couriers.xlsx
        /// </remarks>
        WriteOffPointsAndCouriers,
        /// <summary>
        /// Списание недостач с точек через склад
        /// </summary>
        /// <remarks>
        /// 1С: Склад и доставка – Излишки, недостачи, порчи 
        /// Тип документа: Ордер на отражение недостач товаров
        /// Комментарий: Содердит "ПВЗ или курьеры - недостачи -"
        /// Выделить все, Печать: Ордеры на отражение недостач товаров
        /// Имя файла: Write-off - points through the warehouse shortages
        /// </remarks>
        WriteOffPointsViaWarehouse
    }

    public enum SKURansomsStatus
    {
        /// <summary>
        /// Заказы доставлены на точки
        /// </summary>
        DeliveredToPoints,
        /// <summary>
        /// Заказы забраны с точек 
        /// </summary>
        PickedupFromPoints,
        /// <summary>
        /// Заказы отменены
        /// </summary>
        Canceled,
        /// <summary>
        /// Заказы в пути
        /// </summary>
        InTransit,
        /// <summary>
        /// Неизвестный статус заказов
        /// </summary>
        Unknown

    }

    public enum ParsingTypeCode
    {
        IntTypeCode,
        LongTypeCode,
        LongTypeArrayCode,
        DoubleTypeCode,
        DataOnlyTypeCode,
        DateTimeTypeCode,
        StringTypeCode,
        SKURansomsStatus,
        Marketplace,
        SellmentStatus,
        BoolTypeCode,
        SubjectClaim,
        ClaimExecutor,
        ClaimResolutionStatus,
        DirectoryTypeCode,
    }

    /// <summary>
    /// Предмет претензии
    /// </summary>
    public enum SubjectClaim
    {
        RefundAccrual,          // Возврат начисления
        ShortDeliveryGoods      // Недовоз товаров
    }

    /// <summary>
    /// Исполнитель претензии
    /// </summary>
    public enum ClaimExecutor
    {
        Organization,
        Warehouse,
        OrganizationAndWarehouse
    }

    /// <summary>
    /// Статус решения претензии.
    /// </summary>
    public enum ClaimResolutionStatus
    {
        NotPaid,              // Не оплачено
        IndependentPurchase,  // Докупаем сами
        Paid,                 // Оплачено
        ProductFound,         // Товар найден
        ReturnedToBalance     // Возвращено на баланс
    }

    public enum SourceCellsType
    {
        /// <summary>
        /// Обязательные ячейки
        /// </summary>
        Required,
        /// <summary>
        /// Ячейки для фильтрации
        /// </summary>
        Filtration,
        /// <summary>
        /// Ячейки без фильрации
        /// </summary>
        NonFiltration
    }

    /// <summary>
    /// Цель пересортицы по расходу товаров.
    /// </summary>
    public enum ResortConsumptionTarget
    {
        /// <summary>
        /// Пересортица расходов на расход товаров
        /// </summary>
        ResortingConsumptionByOutgoing,

        /// <summary>
        /// Пересортица расходов на перемещение (фулфилмент)
        /// </summary>
        ResortingConsumptionByTransfer,

        /// <summary>
        /// Пересортица приходов
        /// </summary>
        ResortingReceipt
    }

    public enum SKUResortDirection
    {
        /// <summary>
        /// Входящий пересорт.
        /// </summary>
        Incoming,
        /// <summary>
        /// Исходящий пересорт.
        /// </summary>
        Outgoing,
        /// <summary>
        /// Внутренний пересорт.
        /// </summary>
        Internal,
        /// <summary>
        /// Внутренний пересорт (параллельный клиент).
        /// </summary>
        InternalParallel
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
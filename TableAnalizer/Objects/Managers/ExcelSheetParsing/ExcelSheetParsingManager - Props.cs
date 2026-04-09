using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalance;
using TableAnalizer.Objects.SKUItems.SKUBalances;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    /// <summary>
    /// Менеджер для парсинга данных с таблиц листов Excel
    /// - свойства
    /// </summary>
    public partial class ExcelSheetParsingManager : BaseManager
    {
        #region
        /// <summary>
        /// Набор соответствия предмета претензии ее строковому представлению в отчете. 
        /// </summary>
        private static Dictionary<SubjectClaim, string> SubjectClaimStrings { get; } = new()
        {
            { SubjectClaim.RefundAccrual, "Возврат начисления" },
            { SubjectClaim.ShortDeliveryGoods, "Недовоз товаров" },
        };

        /// <summary>
        /// Набор соответствия статуса решения претензии ее строковому представлению в отчете. 
        /// </summary>
        private static Dictionary<ClaimResolutionStatus, string> ClaimResolutionStatusStrings { get; } = new()
        {
            { ClaimResolutionStatus.IndependentPurchase, "Докупаем сами" },
            { ClaimResolutionStatus.NotPaid, "Не оплачено" },
            { ClaimResolutionStatus.Paid, "Оплачено" },
            { ClaimResolutionStatus.ProductFound, "Товар найден" },
            { ClaimResolutionStatus.ReturnedToBalance, "Возвращено на баланс" },
        };

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по выкупам).
        /// </summary>
        public static List<SourceReportCell> RansomsSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUClientLogin),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 2,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUMarketPlace),
                typeCodeKeyRow: ParsingTypeCode.Marketplace,
                isRequired: true, filterStrings: null),
            new(columnNumber: 3,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 4,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUBrandAndName),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 5,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUAveragePrice),
                typeCodeKeyRow: ParsingTypeCode.DoubleTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 6,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKULastPurchaseDate),
                typeCodeKeyRow: ParsingTypeCode.DataOnlyTypeCode,
                isRequired: true, filterStrings: null),

            new(columnNumber: 7,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUTotalNumberPurchases),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 8,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUInTransitNumberPurchases),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 9,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUCancelledNumberPurchases),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 10,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUDeliveredToPointsNumberPurchases),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 11,
                nameOfFutureProperty:  nameof(SKURansomsBalanceDataUnit.SKUPickedupFromPointsNumberPurchases),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 12,
                nameOfFutureProperty: nameof(SKURansomsBalanceDataUnit.SKUUnknownNumberPurchases),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по приходам, 
        /// расх и остаткам на складе - 1 помещение).
        /// </summary>
        public static List<SourceReportCell> RemainingFirstRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 6,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKUBrandAndName),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            // Приход товаров - получается как композитные данные дочерних строк
            new(columnNumber: 15,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKUReceipt),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null, isGetCompositeDataForCell: true),
            new(columnNumber: 16,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKUWriteOffOnResortOrders),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null, isGetCompositeDataForCell: true),
            new(columnNumber: 17,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKURemaining),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по остаткам на складе - 2-5 помещение).
        /// </summary>
        public static List<SourceReportCell> RemainingFromSecondToFifthRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 6,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKUBrandAndName),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 17,
                nameOfFutureProperty: nameof(SKURemainingDataRow.SKURemaining),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по расходам товаров со склада).
        /// </summary>
        public static List<SourceReportCell> ShippedSourceCells { get; } =
        [
            new(columnNumber: 4,
                nameOfFutureProperty: nameof(SKUShippedDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 28,
                nameOfFutureProperty: nameof(SKUShippedDataUnit.SKUShippedCount),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по перемещению на ФФ - старые склады).
        /// </summary>
        public static List<SourceReportCell> TransferSourceCells { get; } =
        [
            new(columnNumber: 4,
                nameOfFutureProperty: nameof(SKUTransferDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 34,
                nameOfFutureProperty: nameof(SKUTransferDataUnit.SKUTransferedCount),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - экстра-приходы по перещениям и пересорту).
        /// </summary>
        public static List<SourceReportCell> ExtraReceiptSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKUExtraReceiptDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 2,
                nameOfFutureProperty: nameof(SKUExtraReceiptDataUnit.SKUExtraReceiptCount),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 3,
                nameOfFutureProperty: nameof(SKUExtraReceiptDataUnit.ResortComment),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - алиасы).
        /// </summary>
        public static List<SourceReportCell> AliasesSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKUAliasDataUnit.SKUSourceNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 2,
                nameOfFutureProperty: nameof(SKUAliasDataUnit.SKUAliasNumbers),
                typeCodeKeyRow: ParsingTypeCode.LongTypeArrayCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 3,
                nameOfFutureProperty: nameof(SKUAliasDataUnit.AliasComment),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - корретировки расходов от Клиента).
        /// </summary>
        public static List<SourceReportCell> AdjustmentShipmentSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKUAdjustmentShipmentUnit.SKUSourceNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 2,
                nameOfFutureProperty: nameof(SKUAdjustmentShipmentUnit.SKUAdjustmentSum),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 3,
                nameOfFutureProperty: nameof(SKUAdjustmentShipmentUnit.SKUAdjustmentComment),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - параллельные клиенты).
        /// </summary>
        public static List<SourceReportCell> ParallelSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKUParallelUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по списанию с точек и курьеров).
        /// </summary>
        public static List<SourceReportCell> WriteOffPointsAndCouriersSourceCells { get; } =
        [
            new(columnNumber: 4,
                nameOfFutureProperty: nameof(SKUWriteOffDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 27,
                nameOfFutureProperty: nameof(SKUWriteOffDataUnit.SKUWriteOffCount),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по списанию с точек и через склад - старые данные).
        /// </summary>
        public static List<SourceReportCell> WriteOffPointsViaWarehouseSourceCells { get; } =
        [
            new(columnNumber: 5,
                nameOfFutureProperty: nameof(SKUWriteOffDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 39,
                nameOfFutureProperty: nameof(SKUWriteOffDataUnit.SKUWriteOffCount),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по оплаченным недовозам).
        /// </summary>
        public static List<SourceReportCell> PaidShortagesSourceCells { get; } =
        [
            new(columnNumber: 3,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.ClientLogin),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 4,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.SubjectClaim),
                typeCodeKeyRow: ParsingTypeCode.SubjectClaim,
                isRequired: true,
                filterStrings: [SubjectClaimStrings[SubjectClaim.ShortDeliveryGoods]]
        ),
            new(columnNumber: 5,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: false, filterStrings: null),
            new(columnNumber: 10,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.SKUSum),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 12,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.SKUAmount),
                typeCodeKeyRow: ParsingTypeCode.DoubleTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 13,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.ClaimExecutor),
                typeCodeKeyRow: ParsingTypeCode.ClaimExecutor,
                isRequired: true, filterStrings: null),
            new(columnNumber: 18,
                nameOfFutureProperty: nameof(SKUPaidShortageDataUnit.ClaimResolutionStatus),
                typeCodeKeyRow: ParsingTypeCode.ClaimResolutionStatus,
                isRequired: true,
                filterStrings: [ClaimResolutionStatusStrings[ClaimResolutionStatus.Paid],
                    ClaimResolutionStatusStrings[ClaimResolutionStatus.ReturnedToBalance]]
        ),
        ];
    }
    #endregion
}
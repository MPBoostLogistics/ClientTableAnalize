using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalance;
using TableAnalizer.Objects.SKUItems.SKUBalances;
using TableAnalizer.Utils;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    /// <summary>
    /// Менеджер для парсинга данных с таблиц листов Excel.
    /// </summary>
    public class ExcelSheetParsingManager : BaseManager
    {
        #region Fields
        public const int DEFAULT_INERATE_STEP = 1;
        public const string FIRST_ROOM_NAME = "1 - Помещение приемки и сортировки";
        public const string SECOND_ROOM_NAME = "2 - Помещение длительного хранения";
        public const string THIRD_ROOM_NAME = "3 - Помещение упаковки и отгрузки";
        public const string FOUTH_ROOM_NAME = "4 - Участок приемки и ожидания (производство)";
        public const string FIFTH_ROOM_NAME = "5 - Участок длительного хранения (производство)";
        public const string LAST_MARKER_KEY = "Итого товаров в единицах хранения";
        public const int LAST_MARKER_KEY_OFFSET = 2;
        public const string ADDREPORT_EXTRA_RECEIPT_WORKSHEET_NAME = "Экстра-приходы";
        public const string ADDREPORT_ALIASES_WORKSHEET_NAME = "Алиасы";
        public const string ADDREPORT_WRITEOFF_ADJUSTMENTS_WORKSHEET_NAME = "Корректировки расхода";

        /// <summary>
        /// Паттерны регулярных выражений для проверки строк на наличие цифр (для артикулов).
        /// </summary>
        private static readonly List<string> digitsRegeхPatterns = [@"^\d+$"];
        /// <summary>
        /// Регулярные выражения для проверки строк на наличие цифр (для артикулов).
        /// </summary>
        private static readonly Regex[] digitsRegexes = digitsRegeхPatterns.CreateRegexArray();
        /// <summary>
        /// Паттерны регулярных выражений для проверки строк прихода (по приходным ордерам).
        /// </summary>
        private static readonly List<string> receivingRegeхPatterns = [@"^Приходный\sордер\sна\sтовары\s\S+\sот\s\S+\s\S+$"];
        /// <summary>
        /// Регулярные выражения для проверки строк на наличие цифр (для артикулов).
        /// </summary>
        private static readonly Regex[] receivingRegeхes = receivingRegeхPatterns.CreateRegexArray();

        /// <summary>
        /// Дополительные правила (баланс, "1 - Помещение приемки и сортировки")
        /// </summary>
        private static readonly AdditionalRangeRules AdditionalRangeRulesReceiving =
            new(keyStringColumn: 1, keyStringsRegex: digitsRegexes, isGetDataByKeyString: false, childStringColumn: 1, childStringRegexes: receivingRegeхes);


        /// <summary>
        /// Набор соответствия предмета претензии ее строковому представлению в отчете. 
        /// </summary>
        private static readonly Dictionary<SubjectClaim, string> SubjectClaimStrings = new()
        {
            { SubjectClaim.RefundAccrual, "Возврат начисления" },
            { SubjectClaim.ShortDeliveryGoods, "Недовоз товаров" },
        };

        /// <summary>
        /// Набор соответствия статуса решения претензии ее строковому представлению в отчете. 
        /// </summary>
        private static readonly Dictionary<ClaimResolutionStatus, string> ClaimResolutionStatusStrings = new()
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
        /// Набор ячеек для парсинга листа Excel (отчет по приходам и остаткам на складе - 1 помещение).
        /// </summary>
        public static List<SourceReportCell> RemainingFirstRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 6,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUBrandAndName),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 15,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUReceipt),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null),
            new(columnNumber: 17,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKURemaining),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по остаткам на складе - 2 помещение).
        /// </summary>
        public static List<SourceReportCell> RemainingSecondRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 6,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUBrandAndName),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 17,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKURemaining),
                typeCodeKeyRow: ParsingTypeCode.IntTypeCode,
                isRequired: false, filterStrings: null)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по остаткам на складе - 3 помещение).
        /// </summary>
        public static List<SourceReportCell> RemainingThirdRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUNumber),
                typeCodeKeyRow: ParsingTypeCode.LongTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 6,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKUBrandAndName),
                typeCodeKeyRow: ParsingTypeCode.StringTypeCode,
                isRequired: true, filterStrings: null),
            new(columnNumber: 17,
                nameOfFutureProperty: nameof(SKURemainingBalanceDataUnit.SKURemaining),
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

        #endregion

        #region Constructors && destructors
        public ExcelSheetParsingManager(IConfiguration config) : base(config)
        {
            ExcelPackage.License.SetNonCommercialPersonal("vegiwoo");
        }
        #endregion

        #region Functionality
        public async static Task<(double, IEnumerable<Dictionary<string, ItemForParsing>>?)> GetDataFromExcelSheet(SourceReportItemKey reportFileKey, ExcelPackage package, string sheetName)
        {
            // 0. Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // 1. Получение целевого листа из книги Excel.
            if (!GetSheetFromExcelWorkbookByName(in package, sheetName, out ExcelWorksheet? sheet))
                return (StopFuctionExecuting(ref stopwatch), null); // Сообщение в лог, что-то еще вернуть!

            // 2. Получение коллекции диапазонов из целевого листа по ключу отчета.
            Task<List<SourceReportRange>> setTargetTableColumnsTask = SetSourceReportRangeByKey(reportFileKey, sheetName);
            await Task.Run(() => setTargetTableColumnsTask);
            setTargetTableColumnsTask.Wait();
            List<SourceReportRange> rowsRanges = setTargetTableColumnsTask.Result;

            if (rowsRanges.Count == 0)
                return (StopFuctionExecuting(ref stopwatch), null); // Сообщение в лог, что-то еще вернуть!

            // 3. Определение стартовых индексов и колчества строк с данными для диапазонов.
            Task getRangeSourceReportRowsTask = GetSourceReportRange(worksheet: in sheet!, rangeRows: ref rowsRanges);
            await Task.Run(() => getRangeSourceReportRowsTask);
            getRangeSourceReportRowsTask.Wait();

            // 3а. Если есть вложенные правила получения значений для диапазонов 
            var rowsRangesWithRules = rowsRanges.Where(rr => rr.AdditionalRangeRules.HasValue).ToList();
            if (rowsRangesWithRules != null)
            {
                for (int i = 0; i < rowsRangesWithRules.Count; i++)
                {
                    var current = rowsRangesWithRules[i];
                    var index = rowsRanges.IndexOf(current);
                    ApplyAdditionalRulesToRange(in sheet!, ref current);
                    rowsRanges[index] = current;
                }
            }

            // 4. Получение "сырых данных" из строк.
            Task getRawDataTask = GetRawDataFromRangeRows(in sheet!, ref rowsRanges);
            await Task.Run(() => getRawDataTask);
            getRawDataTask.Wait();

            // Union collections
            var items = from sourceReportRowsRange in rowsRanges
                        from item in sourceReportRowsRange.RowsData
                        select item;

            // Stop and return
            return (StopFuctionExecuting(ref stopwatch), items);
        }

        /// <summary>
        /// Получает лист из книги Excel по имени.
        /// </summary>
        /// <param name="package">Пакет (книга Excel).</param>
        /// <param name="sheetName">Имя листа для поиска.</param>
        /// <param name="worksheet">Найденный лист книги Excel или null.</param>
        /// <returns>Флаг уппешного завершения операции (worksheet не равен null).</returns>
        private static bool GetSheetFromExcelWorkbookByName(in ExcelPackage package, string sheetName, out ExcelWorksheet? worksheet)
        {
            worksheet = package.Workbook.Worksheets.SingleOrDefault(ws => ws.Name == sheetName);
            return worksheet is not null;
        }

        /// <summary>
        /// Определение диапазонов в таблицах исходных отчетов по их ключу в заданном листе книги Excel.
        /// </summary>
        /// <param name="key">Ключу исходного отчета.</param>
        /// <param name="sheetName">Имя листа книги.</param>
        /// <param name="rangeSourceReportRows">Коллекции наборов ячеек диапазона.</param>
        /// <returns>Флаг определения (коллекция набора ячеек диапазона не пуста.)</returns>
        private static Task<List<SourceReportRange>> SetSourceReportRangeByKey(SourceReportItemKey key, string sheetName)
        {
            List<SourceReportRange> result = key switch
            {
                SourceReportItemKey.Ransoms => [new(RansomsSourceCells)],
                SourceReportItemKey.Balance =>
                [
                    new(RemainingFirstRoomSourceCells, FIRST_ROOM_NAME, additionalRangeRules: AdditionalRangeRulesReceiving),
                    new(RemainingSecondRoomSourceCells, SECOND_ROOM_NAME),
                    new(RemainingSecondRoomSourceCells, THIRD_ROOM_NAME),
                    new(RemainingSecondRoomSourceCells, FOUTH_ROOM_NAME),
                    new(RemainingThirdRoomSourceCells, FIFTH_ROOM_NAME, (LAST_MARKER_KEY, LAST_MARKER_KEY_OFFSET))
                ],
                SourceReportItemKey.WriteOffPoints => [new(WriteOffPointsAndCouriersSourceCells)],
                SourceReportItemKey.WriteOffPointsViaWarehouseShortage => [new(WriteOffPointsViaWarehouseSourceCells)],
                SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment => [new(WriteOffPointsViaWarehouseSourceCells)],
                SourceReportItemKey.OutgoingProducts => [new(ShippedSourceCells)],
                SourceReportItemKey.Transfered => [new(TransferSourceCells)],
                SourceReportItemKey.Additional => sheetName switch
                {
                    ADDREPORT_EXTRA_RECEIPT_WORKSHEET_NAME => [new(ExtraReceiptSourceCells)],
                    ADDREPORT_ALIASES_WORKSHEET_NAME => [new(AliasesSourceCells)],
                    ADDREPORT_WRITEOFF_ADJUSTMENTS_WORKSHEET_NAME => [new(AdjustmentShipmentSourceCells)],
                    _ => [],
                },
                SourceReportItemKey.ShortagesPaid => [new(PaidShortagesSourceCells)],
                _ => [],
            };

            return Task.Run(() => result);
        }

        /// <summary>
        /// Определяет дипазоны на листе Excel по заданным шаблонам.
        /// </summary>
        /// <param name="worksheet">Целевой лист Excel.</param>
        /// <param name="rangeRows">Коллекция диапазонов строк таблицы.</param>
        /// <param name="startRangeRowIndex">Начальный индекс строки таблицы.</param>
        public static Task GetSourceReportRange(in ExcelWorksheet worksheet, ref List<SourceReportRange> rangeRows, int startRangeRowIndex = 1)
        {
            // Поиск дипазонов с названиями
            IEnumerable<SourceReportRange>? rangesWithKeys = rangeRows
                .Where(rs => !string.IsNullOrEmpty(rs.StringRowKey));

            // Поиск дипазонов с разделителями
            SourceReportRange? rangeWithTermination = rangeRows
                .SingleOrDefault(rs => rs.TerminatingStringValue.HasValue);

            // Если один диапазон без ключей и без разделителя
            bool isOneRangeWithoutKeysAndTerminations = rangeRows.Count == 1 &&
                (rangesWithKeys is null || !rangesWithKeys.Any()) &&
                rangeWithTermination is null;

            // Если есть один диапазон без ключей и без разделителей (например, выкупы, отгрузки и так далее)
            if (isOneRangeWithoutKeysAndTerminations)
            {
                var start = startRangeRowIndex + DEFAULT_INERATE_STEP;
                var count = worksheet.Dimension.End.Row - DEFAULT_INERATE_STEP;
                rangeRows.First().SetStartRowIndexAndNumberRowsOfData(start, count);
            }
            // Если есть несколько диапазонов
            else if (rangeRows.Count > 1)
            {
                SetRowRange(in worksheet, ref rangeRows, rangeWithTermination?.TerminatingStringValue);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Определяет диапазон строк 
        /// </summary>
        /// <param name="sheet">Лист книги Excel.</param>
        /// <param name="rangesRows"></param>
        /// <param name="DEFAULT_STEP"></param>
        /// <param name="termination"></param>
        /// <exception cref="ArgumentException"></exception>
        private static bool SetRowRange(in ExcelWorksheet sheet, ref List<SourceReportRange> rangesRows, (string rowStringKey, int offset)? termination)
        {
            // Получение коллекции кортежей по ключевым строкам (строковые ключи и индексы) для поиска диапазонов.
            Task getKeystringsTask = GetKeyStringsOfRanges(sheet: sheet, rangeSourceReportRows: rangesRows, out List<(string key, int index)>? keyStringsRange);
            Task.Run(() => getKeystringsTask);
            getKeystringsTask.Wait();
            if (keyStringsRange is null) return false; // TODO: Сообщение в лог? Ошибка? 

            // Поиск начальных и количества строк с данными диапазонов.
            Task setRowsTask = SetStartRowsIndexesAndNumbersRowsOfDataInRanges(in sheet, ref rangesRows, in termination, in keyStringsRange);
            Task.Run(() => setRowsTask);
            setRowsTask.Wait();

            return rangesRows.All(rr => rr.RowsIndexes != null);
        }

        /// <summary>
        /// Устанавливает индексы начальных и количество строк с данными в диапазонах. 
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowRanges">Коллекция заготовок-дипазонов.</param>
        /// <param name="termination">Разделители.</param>
        /// <param name="inputRange">Коллекция кортежей - стрововых ключей и их индексов в таблице.</param>
        private static Task SetStartRowsIndexesAndNumbersRowsOfDataInRanges(in ExcelWorksheet sheet, ref List<SourceReportRange> rowRanges,
            in (string rowStringKey, int offset)? termination, in List<(string key, int index)> inputRange)
        {
            List<SourceReportRange> resultRanges = [];

            // 1. Создание связанного списка из существующих элементов таблицы.
            LinkedList<(string key, int index)> existingElementsLinkedList = new(inputRange);

            int? count = null, start = null;

            // 2. Итерация по связанному списку
            var currentNode = existingElementsLinkedList.First;

            while (currentNode != null)
            {
                (string key, int index) = currentNode.Value;

                start = index + DEFAULT_INERATE_STEP;

                if (currentNode.Next != null)
                {
                    (string nextKey, int nextIndex) = currentNode.Next!.Value;
                    count = nextIndex - start.Value;
                }
                else
                {
                    if (termination is not null)
                    {
                        var terminationKey = termination.Value.rowStringKey;
                        var terminationOffset = termination.Value.offset;

                        if (!string.IsNullOrEmpty(terminationKey) &&
                           GetIndexRowByStringKey(worksheet: in sheet, stringKey: terminationKey, out int? terminationIndex) &&
                           terminationIndex.HasValue)
                        {
                            count = terminationIndex - start.Value - terminationOffset;
                        }
                        else
                        {
                            throw new ArgumentException("Cannot get index of termination row.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Multiple ranges must contain a termination row.");
                    }
                }

                // Set range
                var targetTemplate = rowRanges
                    .SingleOrDefault(el => string.Equals(el.StringRowKey, key));

                if (targetTemplate is not null &&
                   start.HasValue &&
                   count.HasValue)
                {
                    targetTemplate.SetStartRowIndexAndNumberRowsOfData(start.Value, count.Value);
                    resultRanges.Add(targetTemplate);
                }

                // Next step
                currentNode = currentNode!.Next;
            }

            rowRanges = resultRanges;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Получение коллекции названий и индексов строк с ключами (ключевых строк диапазонов).
        /// </summary>
        /// <param name="sheet">Лист книги Excel с таблицей.</param>
        /// <param name="rangeSourceReportRows">Коллекция шаблонов диапазонов строк.</param>
        /// <param name="keyStringsRange">Коллекция названий и индексов ключевых строк диапазонов.</param>
        private static Task GetKeyStringsOfRanges(in ExcelWorksheet sheet,
            in List<SourceReportRange> rangeSourceReportRows, out List<(string key, int index)>? keyStringsRange)
        {
            // Получение существующих ключей из шаблонов.
            List<string>? keysInTemplateList = [.. rangeSourceReportRows
                .Where(el => !string.IsNullOrEmpty(el.StringRowKey))
                .Select(el => el.StringRowKey!)];

            // Получение индексов строк с ключами.
            keyStringsRange = [];
            for (int i = 0; i < keysInTemplateList.Count; i++)
            {
                var key = keysInTemplateList[i];
                if (!string.IsNullOrEmpty(key) &&
                    GetIndexRowByStringKey(worksheet: in sheet, stringKey: key, out int? rowIndex) &&
                    rowIndex.HasValue)
                {
                    keyStringsRange.Add((key, rowIndex.Value));
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Возвращает индекс строки из таблицы книги Excel по строковому значению (ключу).
        /// </summary>
        /// <param name="worksheet">Лист рабочей книги для поиска.</param>
        /// <param name="stringKey">Строковый ключ для поиска строки.</param>
        /// <returns>Целочисленный индекс строки или  NULL.</returns>
        private static bool GetIndexRowByStringKey(in ExcelWorksheet worksheet, string stringKey, out int? rowInex)
        {
            rowInex = worksheet.Cells.FirstOrDefault(c => c.Value != null &&
                c.Value.ToString()?.Trim().ToLower() == stringKey.Trim().ToLower())?.Start.Row;
            return rowInex.HasValue;
        }

        /// <summary>
        /// Получает "сырые" данные из строк диапазона таблицы Excel.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowRanges">Коллекция заготовок-дипазонов.</param>
        /// <returns>Флаг выполнения операции.</returns>
        public static Task GetRawDataFromRangeRows(in ExcelWorksheet sheet, ref List<SourceReportRange> rowsRanges)
        {
            // 1. Итерация по коолекции диапазонов.
            for (int i = 0; i < rowsRanges.Count; i++)
            {
                SourceReportRange? rowsRange = rowsRanges[i];

                if (rowsRange is null || rowsRange.RowsIndexes is null) continue;

                // 2. Итерация по строкам диапазона.
                // 2а. Определяем диапазон - если есть доп правила и получаем индексы ключевых строк,
                // то берем их, иначе берем список всех полученных индексов.
                List<int> rowsIndexRange = rowsRange.AdditionalRangeRules.HasValue &&
                    rowsRange.AdditionalRangeRules.Value.ValidKeyRowIndices.Count > 0 ?
                    rowsRange.AdditionalRangeRules.Value.ValidKeyRowIndices :
                    [.. rowsRange.RowsIndexes];

                var currentSourceCells = rowsRange.SourceReportCells;

                for (int j = 0; j < rowsIndexRange.Count; j++)
                {
                    var rowIndex = rowsIndexRange[j];

                    var getValuesFromRowTask = GetValuesFromRow(in sheet, rowIndex, in currentSourceCells, out Dictionary<string, ItemForParsing>? values);
                    Task.Run(() => getValuesFromRowTask);
                    getValuesFromRowTask.Wait();

                    var getValuesFromRowTaskSuccess = getValuesFromRowTask.Result;

                    if (getValuesFromRowTaskSuccess && values is not null)
                    {
                        rowsRange.SetRowsData(values);
                        rowsRanges![i] = rowsRange;
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Получает значения из строки таблицы.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowIndex">Индекс строки.</param>
        /// <param name="sourceCells">Коллекция ячеек-заготовок диапазона таблицы.</param>
        /// <param name="values">Словарь, где ключ - имя значения, а значение - полученные данные. </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Возникает когда не обнаружено обязательное значение в ячейке.</exception>
        public static Task<bool> GetValuesFromRow(in ExcelWorksheet sheet, int rowIndex, in List<SourceReportCell> sourceCells, out Dictionary<string, ItemForParsing>? values)
        {
            values = null;

            if (!CheckPresenceAddressesInRow(in sheet, rowIndex, in sourceCells) ||
                !СheckingValuesInRequiredCells(in sheet, rowIndex, in sourceCells) ||
                !FilterMatchingCheck(in sheet, rowIndex, in sourceCells, out int filterCellsCount) ||
                !FindSorceCellsByType(in sourceCells, SourceCellsType.NonFiltration, out List<SourceReportCell>? dataСellsNonFilters) ||
                dataСellsNonFilters is null ||
                dataСellsNonFilters!.Count == 0)
            {
                return Task.Run(() => false);
            }

            // Getting values
            values = [];
            foreach (var dataCell in dataСellsNonFilters)
            {
                // Получение значения ячейки.
                var colNumber = dataCell.ColumnNumber;
                var value = sheet.Cells[rowIndex, colNumber].Value;

                // Если значение ячейки пустое, то проверяем на обязательность заполнения.
                if (value is null)
                {
                    if (dataCell.IsRequired)
                        throw new ArgumentException($"A table cell does not contain a <b>required value</b> for name: '{dataCell.NameOfFutureProperty}'");

                    // Если значение необязательное, то устанавливаем пустую строку.
                    value = string.Empty;
                }

                var valueToString = value.ToString();

                if (valueToString is not null)
                    values.Add(dataCell.NameOfFutureProperty, new(valueToString, dataCell.TypeCodeKeyRow, !dataCell.IsRequired));
            }

            var success = values?.Count == sourceCells.Count - filterCellsCount;
            return Task.Run(() => success);
        }

        /// <summary>
        /// Проверяет ячейку по адресу на наличие значения.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowIndex">Индекс строки.</param>
        /// <param name="sourceCells">Коллекция ячеек-заготовок диапазона таблицы.</param>
        /// <returns>Флаг успеха проверки ячейки на наличии значения.</returns>
        /// <remarks><b>Точка парсинга данных</b></remarks>
        /// <exception cref="Exception">Ошибка если ячейка по указанному адресу не найдена.</exception>
        private static bool CheckPresenceAddressesInRow(in ExcelWorksheet sheet, int rowIndex, in List<SourceReportCell> sourceCells)
        {
            // 1. Итерация по ячейкам-заготовкам диапазона.
            foreach (var sourceCell in sourceCells)
            {
                // 2. Получение адреса ячейки.
                ExcelRange? address = sheet.Cells[rowIndex, sourceCell.ColumnNumber] ??
                    throw new Exception($"ExcelRange (address) at row {rowIndex} and column {sourceCell.ColumnNumber} is null.");

                // 3. Получение значения ячейки.
                var addressValue = address.Value;
                if (addressValue is null || string.IsNullOrEmpty(addressValue.ToString()))
                    return false; // TODO: Сообщение в лог? Ошибка?

                // 4. Парсинг значения ячейки по заданному типу.
                return ParsingManager.Parse(addressValue.ToString()!, sourceCell.TypeCodeKeyRow, !sourceCell.IsRequired, out object? value) &&
                    value is not null;
            }

            return true;
        }

        /// <summary>
        /// Проверяет соответствие значений на фильтрацию в ячейках диапазонов.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowIndex">Индекс строки.</param>
        /// <param name="sourceCells">Коллекция ячеек-заготовок диапазона таблицы.</param>
        /// <param name="filterSourceCellsCount">Количество ячеек-заготовок фильтрации.</param>
        /// <returns></returns>
        private static bool FilterMatchingCheck(in ExcelWorksheet sheet, int rowIndex, in List<SourceReportCell> sourceCells, out int filterSourceCellsCount)
        {
            filterSourceCellsCount = 0;

            // Если нет фильтрации, то сразу возвращаем true.
            if (!FindSorceCellsByType(in sourceCells, SourceCellsType.Filtration, out List<SourceReportCell>? filterSourceCells))
            {
                return true;
            }
            // Если есть фильтрация, то проверяем на соответствие.
            else
            {
                filterSourceCellsCount = filterSourceCells!.Count;
                foreach (var filterSourceCell in filterSourceCells)
                {
                    if (!IsFilterMatch(filterSourceCell.FilterStrings!, sheet.Cells[rowIndex, filterSourceCell.ColumnNumber].Value))
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Проверка обязательных значений в ячейках диапазона.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowIndex">Индекс строки.</param>
        /// <param name="sourceCells">Коллекция ячеек-заготовок диапазона таблицы.</param>
        /// <returns>Флаг проверки ячеек.</returns>
        private static bool СheckingValuesInRequiredCells(in ExcelWorksheet sheet, int rowIndex, in List<SourceReportCell> sourceCells)
        {
            if (!FindSorceCellsByType(in sourceCells, SourceCellsType.Required, out List<SourceReportCell>? requiredCells) &&
                requiredCells is null ||
                requiredCells?.Count == 0)
            {
                return true;
            }
            else
            {
                var requiredCellValues = new List<object>();
                foreach (var sourceCell in requiredCells!)
                {
                    var cell = sheet.Cells[rowIndex, sourceCell.ColumnNumber];
                    var value = cell.Value;
                    if (cell is null || value is null)
                    {
                        return false;
                    }
                    else
                    {
                        requiredCellValues.Add(value);
                    }
                }

                return requiredCellValues.All(v => v is not null);
            }
        }

        /// <summary>
        /// Поиск ячеек-заготовок диапазона таблицы по типу.
        /// </summary>
        /// <param name="sourceCells">Ячейки-заготовки.</param>
        /// <param name="sourceCellsType">Типа ячеек-заготовок.</param>
        /// <param name="filteredСellsByType">Ячейки-заготовки, отфилтрованные по типу.</param>
        /// <returns></returns>
        private static bool FindSorceCellsByType(in List<SourceReportCell> sourceCells, SourceCellsType sourceCellsType, out List<SourceReportCell>? filteredСellsByType)
        {
            filteredСellsByType = sourceCellsType switch
            {
                SourceCellsType.Required => [.. sourceCells.Where(sc => sc.IsRequired)],
                SourceCellsType.Filtration => [.. sourceCells.Where(sc => sc.FilterStrings != null && sc.FilterStrings.Any())],
                SourceCellsType.NonFiltration => [.. sourceCells.Where(sc => sc.FilterStrings == null || !sc.FilterStrings.Any())],
                _ => null,
            };

            return filteredСellsByType is not null && filteredСellsByType.Any();
        }

        /// <summary>
        /// Проверяет соответствие значения ячейки фильтруемому значению.
        /// </summary>
        /// <param name="filters">Строковые значения-фильтры.</param>
        /// <param name="value">Проверяемое значение.</param>
        /// <returns>Флаг проверки.</returns>
        private static bool IsFilterMatch(string[] filters, object value) =>
          value is not null &&
          !string.IsNullOrEmpty(value!.ToString()) &&
          filters.Contains(value!.ToString());

        private static void ApplyAdditionalRulesToRange(in ExcelWorksheet sheet, ref SourceReportRange range)
        {
            if (range.RowsIndexes == null || !range.AdditionalRangeRules.HasValue) return;

            // Получаем индексы валидных ключевых строк.
            SearchValidRowIndexesOfKeyRows(in sheet, ref range);

            // Если получаем данные по ключевым строкам, но ничего большще не требуется.
            // Если получаем данные по дочерним строкам к ключевой:
            if (!range.AdditionalRangeRules!.Value.IsGetDataByKeyString)
            {
                // Заполняем словарь индексов (ключевая строка, дочерние строки)
                GetIntermediateRowIndices(ref range);

                // Оставляем в индексах дочерних строк только соответствующие регуляным выражениям.
                RemoveIndexesInvalidChildRows(in sheet, ref range);

                foreach (KeyValuePair<int, List<int>> entry in range.AdditionalRangeRules!.Value.ValidChildRowIndices)
                {
                    Console.WriteLine($"a: {entry.Key}, ");
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        Console.Write($"{entry.Value[i]}, ");
                    }
                    Console.WriteLine();
                }
            }
        }

        /// <summary>
        /// Ищет валидные индексы для ключевых строк.
        /// </summary>
        /// <param name="sheet">Рабочий лист Excel.</param>
        /// <param name="range">Дипазон таблицы рабочего листа.</param>
        private static void SearchValidRowIndexesOfKeyRows(in ExcelWorksheet sheet, ref SourceReportRange range)
        {
            // Исходный список индексов строк.
            var indexes = range.RowsIndexes!.ToList();

            for (int i = 0; i < indexes.Count; i++)
            {
                var rowIndex = indexes[i];
                var columnIndex = range.AdditionalRangeRules!.Value.KeyStringColumn;
                if (GetValueFromTableCell(in sheet, (rowIndex, columnIndex), out string? stringValue) && !string.IsNullOrEmpty(stringValue))
                {
                    // Итераций по правилам регулярных выражений для поиска ключевых строк.
                    var keyStringRegexRules = range.AdditionalRangeRules!.Value.KeyStringRegex;

                    for (int j = 0; j < keyStringRegexRules.Length; j++)
                    {
                        var regexRule = keyStringRegexRules[j];
                        if (regexRule.IsMatch(stringValue))
                        {
                            // Запоминаем индекс валидной ключевой строки.
                            range.AdditionalRangeRules.Value.SetValidKeyRowIndices(indexes[i]);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Возвращает все индексы строк, находящиеся между ключевыми строками (включительно или исключительно — по необходимости).
        /// </summary>
        /// <param name="validKeyRowIndices">Список индексов ключевых строк.</param>
        /// <returns>Список промежуточных индексов строк.</returns>
        public static void GetIntermediateRowIndices(ref SourceReportRange range)
        {
            // Входная проверка 
            if (range.RowsIndexes == null || !range.RowsIndexes.Any() || !range.AdditionalRangeRules.HasValue)
                return;

            // Проверка на наличие ключевых строк.
            var keyRowIndices = range.AdditionalRangeRules.Value.ValidKeyRowIndices;
            if (keyRowIndices.Count == 0) return;

            var lastRowIndex = range.RowsIndexes.Last();

            var validChildRowIndices = range.AdditionalRangeRules.Value.ValidChildRowIndices;

            for (int i = 0; i < keyRowIndices.Count; i++)
            {
                int startKeyRow = keyRowIndices[i];
                int endKeyRow = (i + 1 < keyRowIndices.Count) ? keyRowIndices[i + 1] : lastRowIndex + 1;

                var intermediateIndices = new List<int>();
                for (int j = startKeyRow + 1; j < endKeyRow; j++)
                {
                    intermediateIndices.Add(j);
                }

                validChildRowIndices[startKeyRow] = intermediateIndices;
            }
        }

        /// <summary>
        /// Получает строковое значение из ячейки таблицы.
        /// </summary>
        /// <param name="sheet">Рабочий лист Excel.</param>
        /// <param name="address">Адрес ячейки в таблице рабочего листа (номер строки, номер столбца).</param>
        /// <param name="stringValue">строковое значение из ячейки таблицы (опционально).</param>
        /// <returns>Флаг - строковое значение не null и не пустая строка.</returns>
        private static bool GetValueFromTableCell(in ExcelWorksheet sheet, (int rowNumber, int colNumber) address, out string? stringValue)
        {
            var val = sheet.Cells[address.rowNumber, address.colNumber].Value;
            stringValue = val is not null && !string.IsNullOrEmpty(val.ToString()) ? val.ToString() : null;
            return stringValue is not null;
        }

        /// <summary>
        /// Удаляет индексы инвалидных дочерних строк (не соответсвуют резулярным выражениям).
        /// </summary>
        /// <param name="sheet">Лист рабочей книги Excel.</param>
        /// <param name="range">Диапазон таблицы рабочей книги.</param>
        private static void RemoveIndexesInvalidChildRows(in ExcelWorksheet sheet, ref SourceReportRange range)
        {
            if (range.RowsIndexes == null ||
                !range.AdditionalRangeRules.HasValue ||
                range.AdditionalRangeRules.Value.ValidChildRowIndices.Count == 0 ||
                range.AdditionalRangeRules.Value.ChildStringColumn is null ||
                range.AdditionalRangeRules.Value.ChildStringRegexes is null)
                return;

            // Перебираем все пары (ключ, список дочерних индексов)
            foreach (var entry in range.AdditionalRangeRules.Value.ValidChildRowIndices)
            {
                var childRowIndices = entry.Value;
                int j = childRowIndices.Count - 1;

                // Итерация с конца, чтобы избежать проблем с индексами при удалении
                while (j >= 0)
                {
                    int rowIndex = childRowIndices[j];
                    if (!GetValueFromTableCell(in sheet, (rowIndex, (int)range.AdditionalRangeRules.Value.ChildStringColumn), out string? val) ||
                        string.IsNullOrEmpty(val))
                    {
                        j--;
                        continue;
                    }

                    bool isValid = false;
                    // Проверяем, соответствует ли значение хотя бы одному из регулярных выражений
                    for (int k = 0; k < range.AdditionalRangeRules.Value.ChildStringRegexes.Length; k++)
                    {
                        if (range.AdditionalRangeRules.Value.ChildStringRegexes[k].IsMatch(val))
                        {
                            isValid = true;
                            break;
                        }
                    }

                    // Если ни одно выражение не подошло — удаляем индекс
                    if (!isValid)
                    {
                        childRowIndices.RemoveAt(j);
                    }

                    j--;
                }

            }
        }
        #endregion
    }
}
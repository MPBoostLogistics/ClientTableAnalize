using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using TableAnalizer.Objects.SKUItems;
using TableAnalizer.Objects.SKUItems.SKUBalance;
using TableAnalizer.Objects.SKUItems.SKUBalances;

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
        public static List<SourceCell> RansomsSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUClientLogin),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: true),
            new(columnNumber: 2,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUMarketPlace),
                parsingTypeCode: ParsingTypeCode.Marketplace,
                isRequired: true),
            new(columnNumber: 3,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 4,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUBrandAndName),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: true),
            new(columnNumber: 5,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUAveragePrice),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode,
                isRequired: true),
            new(columnNumber: 6,
                cellName: nameof(SKURansomsBalanceDataUnit.SKULastPurchaseDate),
                parsingTypeCode: ParsingTypeCode.DataOnlyTypeCode,
                isRequired: true),

            new(columnNumber: 7,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUTotalNumberPurchases),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 8,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUInTransitNumberPurchases),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 9,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUCancelledNumberPurchases),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 10,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUDeliveredToPointsNumberPurchases),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 11,
                cellName:  nameof(SKURansomsBalanceDataUnit.SKUPickedupFromPointsNumberPurchases),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 12,
                cellName: nameof(SKURansomsBalanceDataUnit.SKUUnknownNumberPurchases),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по приходам и остаткам на складе - 1 помещение).
        /// </summary>
        public static List<SourceCell> RemainingFirstRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKURemainingBalanceDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 6,
                cellName: nameof(SKURemainingBalanceDataUnit.SKUBrandAndName),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: true),
            new(columnNumber: 15,
                cellName: nameof(SKURemainingBalanceDataUnit.SKUReceipt),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: false),
            new(columnNumber: 17,
                cellName: nameof(SKURemainingBalanceDataUnit.SKURemaining),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: false)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по остаткам на складе - 2 помещение).
        /// </summary>
        public static List<SourceCell> RemainingSecondRoomSourceCells { get; } =
        [
            new(columnNumber: 1, nameof(SKURemainingBalanceDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 6, nameof(SKURemainingBalanceDataUnit.SKUBrandAndName),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: true),
            new(columnNumber: 17, nameof(SKURemainingBalanceDataUnit.SKURemaining),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: false)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по остаткам на складе - 3 помещение).
        /// </summary>
        public static List<SourceCell> RemainingThirdRoomSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKURemainingBalanceDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 6,
                cellName: nameof(SKURemainingBalanceDataUnit.SKUBrandAndName),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: true),
            new(columnNumber: 17,
                cellName: nameof(SKURemainingBalanceDataUnit.SKURemaining),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: false)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по расходам товаров со склада).
        /// </summary>
        public static List<SourceCell> ShippedSourceCells { get; } =
        [
            new(columnNumber: 4,
                cellName: nameof(SKUShippedDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 28,
                cellName: nameof(SKUShippedDataUnit.SKUShippedCount),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по перемещению на ФФ - старые склады).
        /// </summary>
        public static List<SourceCell> TransferSourceCells { get; } =
        [
            new(columnNumber: 4,
                cellName: nameof(SKUTransferDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 34,
                cellName: nameof(SKUTransferDataUnit.SKUTransferedCount),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - экстра-приходы по перещениям и пересорту).
        /// </summary>
        public static List<SourceCell> ExtraReceiptSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKUExtraReceiptDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 2,
                cellName: nameof(SKUExtraReceiptDataUnit.SKUExtraReceiptCount),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 3,
                cellName: nameof(SKUExtraReceiptDataUnit.ResortComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: false)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - алиасы).
        /// </summary>
        public static List<SourceCell> AliasesSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKUAliasDataUnit.SKUSourceNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 2,
                cellName: nameof(SKUAliasDataUnit.SKUAliasNumbers),
                parsingTypeCode: ParsingTypeCode.LongTypeArrayCode,
                isRequired: true),
            new(columnNumber: 3,
                cellName: nameof(SKUAliasDataUnit.AliasComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: false)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - корретировки расходов от Клиента).
        /// </summary>
        public static List<SourceCell> AdjustmentShipmentSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKUAdjustmentShipmentUnit.SKUSourceNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 2,
                cellName: nameof(SKUAdjustmentShipmentUnit.SKUAdjustmentSum),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 3,
                cellName: nameof(SKUAdjustmentShipmentUnit.SKUAdjustmentComment),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: false)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (доп отчет - параллельные клиенты).
        /// </summary>
        public static List<SourceCell> ParallelSourceCells { get; } =
        [
            new(columnNumber: 1,
                cellName: nameof(SKUParallelUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по списанию с точек и курьеров).
        /// </summary>
        public static List<SourceCell> WriteOffPointsAndCouriersSourceCells { get; } =
        [
            new(columnNumber: 4,
                cellName: nameof(SKUWriteOffDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode, true),
            new(columnNumber: 27,
                cellName: nameof(SKUWriteOffDataUnit.SKUWriteOffCount),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по списанию с точек и через склад - старые данные).
        /// </summary>
        public static List<SourceCell> WriteOffPointsViaWarehouseSourceCells { get; } =
        [
            new(columnNumber: 5,
                cellName: nameof(SKUWriteOffDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: true),
            new(columnNumber: 39,
                cellName: nameof(SKUWriteOffDataUnit.SKUWriteOffCount),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true)
        ];

        /// <summary>
        /// Набор ячеек для парсинга листа Excel (отчет по оплаченным недовозам).
        /// </summary>
        public static List<SourceCell> PaidShortagesSourceCells { get; } =
        [
            new(columnNumber: 3,
                cellName: nameof(SKUPaidShortageDataUnit.ClientLogin),
                parsingTypeCode: ParsingTypeCode.StringTypeCode,
                isRequired: true),
            new(columnNumber: 4,
                cellName: nameof(SKUPaidShortageDataUnit.SubjectClaim),
                parsingTypeCode: ParsingTypeCode.SubjectClaim,
                isRequired: true,
                filterStrings: [SubjectClaimStrings[SubjectClaim.ShortDeliveryGoods]]),
            new(columnNumber: 5,
                cellName: nameof(SKUPaidShortageDataUnit.SKUNumber),
                parsingTypeCode: ParsingTypeCode.LongTypeCode,
                isRequired: false),
            new(columnNumber: 10,
                cellName: nameof(SKUPaidShortageDataUnit.SKUSum),
                parsingTypeCode: ParsingTypeCode.IntTypeCode,
                isRequired: true),
            new(columnNumber: 12,
                cellName: nameof(SKUPaidShortageDataUnit.SKUAmount),
                parsingTypeCode: ParsingTypeCode.DoubleTypeCode,
                isRequired: true),
            new(columnNumber: 13,
                cellName: nameof(SKUPaidShortageDataUnit.ClaimExecutor),
                parsingTypeCode: ParsingTypeCode.ClaimExecutor,
                isRequired: true),
            new(columnNumber: 18,
                cellName: nameof(SKUPaidShortageDataUnit.ClaimResolutionStatus),
                parsingTypeCode: ParsingTypeCode.ClaimResolutionStatus,
                isRequired: true,
                filterStrings: [ClaimResolutionStatusStrings[ClaimResolutionStatus.Paid], ClaimResolutionStatusStrings[ClaimResolutionStatus.ReturnedToBalance]]),
        ];


        #endregion

        #region Constructors && destructors
        public ExcelSheetParsingManager(IConfiguration config) : base(config)
        {
            ExcelPackage.License.SetNonCommercialPersonal("vegiwoo");
        }
        #endregion

        #region Functionality
        public async static Task<(double, IEnumerable<Dictionary<string, ItemForParsing>>?)> GetDataFromExcelSheet(SourceReportItemKey reportFileKey, ExcelPackage package, string workSheetName)
        {
            // 0. Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // 1. Получение целевого листа из книги Excel.
            if (!GetSheetFromExcelWorkbookByName(in package, workSheetName, out ExcelWorksheet? sheet))
                return (StopFuctionExecuting(ref stopwatch), null); // Сообщение в лог, что-то еще вернуть!

            // 2. Получение коллекции диапазонов из целевого листа по ключу отчета.
            Task<List<RangeRowsWithDataInSourceReport>> setTargetTableColumnsTask = SetTargetTableColumns(reportFileKey, workSheetName);
            await Task.Run(() => setTargetTableColumnsTask);
            setTargetTableColumnsTask.Wait();
            List<RangeRowsWithDataInSourceReport> rowsRanges = setTargetTableColumnsTask.Result;

            if (rowsRanges.Count == 0)
                return (StopFuctionExecuting(ref stopwatch), null); // Сообщение в лог, что-то еще вернуть!

            // 3. Определение стартовых индексов и колчества строк с данными для диапазово 
            Task getRangeSourceReportRowsTask = GetRangeSourceReportRows(worksheet: in sheet!, rangeRows: ref rowsRanges);
            await Task.Run(() => getRangeSourceReportRowsTask);
            getRangeSourceReportRowsTask.Wait();

            // 4. Получение "сырых данных" из строк.
            Task getRawDataTask = GetRawDataFromRangeRows(in sheet!, ref rowsRanges);
            await Task.Run(() => getRawDataTask);
            getRawDataTask.Wait();

            // Union collections
            var items = from sourceReportRowsRange in rowsRanges
                        from item in sourceReportRowsRange.DataOnReadRowsCollection
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
        /// Опеределения набора ячеек диапазона по ключу исходного отчета в заданном лисеа книги Excel.
        /// </summary>
        /// <param name="key">Ключу исходного отчета.</param>
        /// <param name="sheetName">Имя листа книги.</param>
        /// <param name="rangeSourceReportRows">Коллекции наборов ячеек диапазона.</param>
        /// <returns>Флаг определения (коллекция набора ячеек диапазона не пуста.)</returns>
        private static Task<List<RangeRowsWithDataInSourceReport>> SetTargetTableColumns(SourceReportItemKey key, string sheetName)
        {
            List<RangeRowsWithDataInSourceReport> result = key switch
            {
                SourceReportItemKey.Ransoms => [new(RansomsSourceCells)],
                SourceReportItemKey.Balance =>
                [
                    new(RemainingFirstRoomSourceCells, FIRST_ROOM_NAME),
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
        public static Task GetRangeSourceReportRows(in ExcelWorksheet worksheet, ref List<RangeRowsWithDataInSourceReport> rangeRows, int startRangeRowIndex = 1)
        {
            IEnumerable<RangeRowsWithDataInSourceReport>? rangesWithKeys = rangeRows
                .Where(rs => !string.IsNullOrEmpty(rs.StringRowKey));

            RangeRowsWithDataInSourceReport? rangeWithTermination = rangeRows
                .SingleOrDefault(rs => rs.TerminatingStringValue.HasValue);

            bool isOneRangeWithoutKeysAndTerminations = rangeRows.Count == 1 &&
                (rangesWithKeys is null || !rangesWithKeys.Any()) &&
                rangeWithTermination is null;

            // Range is one, no key and no termination value (e.g. Ransoms, Shipped, Compenaton, Resorts etc)
            if (isOneRangeWithoutKeysAndTerminations)
            {
                var start = startRangeRowIndex + DEFAULT_INERATE_STEP;
                var count = worksheet.Dimension.End.Row - DEFAULT_INERATE_STEP;
                rangeRows.First().SetStartRowIndexAndNumberRowsOfData(start, count);
            }
            // There is more than one range (e.g. Remainig)
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
        private static bool SetRowRange(in ExcelWorksheet sheet, ref List<RangeRowsWithDataInSourceReport> rangesRows, (string rowStringKey, int offset)? termination)
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

            return rangesRows.All(rr => rr.RangeRowIndexesСollection != null);
        }

        /// <summary>
        /// Устанавливает индексы начальных и количество строк с данными в диапазонах. 
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowRanges">Коллекция заготовок-дипазонов.</param>
        /// <param name="termination">Разделители.</param>
        /// <param name="inputRange">Коллекция кортежей - стрововых ключей и их индексов в таблице.</param>
        private static Task SetStartRowsIndexesAndNumbersRowsOfDataInRanges(in ExcelWorksheet sheet, ref List<RangeRowsWithDataInSourceReport> rowRanges,
            in (string rowStringKey, int offset)? termination, in List<(string key, int index)> inputRange)
        {
            List<RangeRowsWithDataInSourceReport> resultRanges = [];

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
            in List<RangeRowsWithDataInSourceReport> rangeSourceReportRows, out List<(string key, int index)>? keyStringsRange)
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
        public static Task GetRawDataFromRangeRows(in ExcelWorksheet sheet, ref List<RangeRowsWithDataInSourceReport> rowsRanges)
        {
            // 1. Итерация по коолекции диапазонов.
            for (int i = 0; i < rowsRanges.Count; i++)
            {
                RangeRowsWithDataInSourceReport? rowsRange = rowsRanges[i];

                if (rowsRange is null || rowsRange.RangeRowIndexesСollection is null) continue;

                // 2. Итерация по строкам диапазона.
                var rowsIndexRange = rowsRange.RangeRowIndexesСollection.ToList();
                var currentSourceCells = rowsRange.SourceColumns;

                for (int j = 0; j < rowsIndexRange.Count; j++)
                {
                    var rowIndex = rowsIndexRange[j];

                    var getValuesFromRowTask = GetValuesFromRow(in sheet, rowIndex, in currentSourceCells, out Dictionary<string, ItemForParsing>? values);
                    Task.Run(() => getValuesFromRowTask);
                    getValuesFromRowTask.Wait();

                    var getValuesFromRowTaskSuccess = getValuesFromRowTask.Result;

                    if (getValuesFromRowTaskSuccess && values is not null)
                    {
                        rowsRange.SetDataOnReadRowsCollection(values);
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
        public static Task<bool> GetValuesFromRow(in ExcelWorksheet sheet, int rowIndex, in List<SourceCell> sourceCells, out Dictionary<string, ItemForParsing>? values)
        {
            values = null;

            if (!CheckPresenceAddressesInRow(in sheet, rowIndex, in sourceCells) ||
                !СheckingValuesInRequiredCells(in sheet, rowIndex, in sourceCells) ||
                !FilterMatchingCheck(in sheet, rowIndex, in sourceCells, out int filterCellsCount) ||
                !FindSorceCellsByType(in sourceCells, SourceCellsType.NonFiltration, out List<SourceCell>? dataСellsNonFilters) ||
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
                var value = sheet.Cells[rowIndex, dataCell.ColumnNumber].Value;

                // Если значение ячейки пустое, то проверяем на обязательность заполнения.
                if (value is null)
                {
                    if (dataCell.IsRequired)
                        throw new ArgumentException($"A table cell does not contain a <b>required value</b> for name: '{dataCell.CellName}'");
                    
                    // Если значение необязательное, то устанавливаем пустуюб строку.
                    value = string.Empty;
                }

                var valueToString = value.ToString();

                if (valueToString is not null)
                    values.Add(dataCell.CellName, new(valueToString, dataCell.ParsingTypeCode, !dataCell.IsRequired));
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
        private static bool CheckPresenceAddressesInRow(in ExcelWorksheet sheet, int rowIndex, in List<SourceCell> sourceCells)
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
                return ParsingManager.Parse(addressValue.ToString()!, sourceCell.ParsingTypeCode, !sourceCell.IsRequired, out object? value) &&
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
        private static bool FilterMatchingCheck(in ExcelWorksheet sheet, int rowIndex, in List<SourceCell> sourceCells, out int filterSourceCellsCount)
        {
            filterSourceCellsCount = 0;

            // Если нет фильтрации, то сразу возвращаем true.
            if (!FindSorceCellsByType(in sourceCells, SourceCellsType.Filtration, out List<SourceCell>? filterSourceCells))
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
        private static bool СheckingValuesInRequiredCells(in ExcelWorksheet sheet, int rowIndex, in List<SourceCell> sourceCells)
        {
            if (!FindSorceCellsByType(in sourceCells, SourceCellsType.Required, out List<SourceCell>? requiredCells) &&
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
        private static bool FindSorceCellsByType(in List<SourceCell> sourceCells, SourceCellsType sourceCellsType, out List<SourceCell>? filteredСellsByType)
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

        #endregion
    }
}
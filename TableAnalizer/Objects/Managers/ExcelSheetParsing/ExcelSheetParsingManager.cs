using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using Microsoft.Extensions.Logging;
using TableAnalizer.Utils;
using Microsoft.Extensions.Options;


namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    public interface IExcelSheetParsingManager
    {
        public Task<(double, IEnumerable<RowData>?)> GetDataFromExcelSheet(SourceReportItemKey reportFileKey, ExcelPackage package, string sheetName);
    }


    /// <summary>
    /// Менеджер для парсинга данных из таблиц Excel.
    /// </summary>
    public partial class ExcelSheetParsingManager : BaseManager, IExcelSheetParsingManager
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
        public const string ADDREPORT_ALIASES_WORKSHEET_NAME = "Алиасы";
        public const string ADDREPORT_RESORTS_WORKSHEET_NAME = "Пересорты расхода";
        // public const string ADDREPORT_WRITEOFF_ADJUSTMENTS_WORKSHEET_NAME = "Корректировки расхода";

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

        private static readonly List<string> RESORT_ORDER_PATTERN = [@"^Ордер\sна\sотражение\sпересортицы\sтоваров\s\S+\sот\s\S+\s\S+$"];

        /// Паттерны регулярных выражения для прихода товарами (приходные ордера, ордера пересортицы)
        private static readonly List<string> receivingRegeхPatterns = [
            @"^Приходный\sордер\sна\sтовары\s\S+\sот\s\S+\s\S+$",
            RESORT_ORDER_PATTERN.First()
        ];

        /// <summary>
        /// Регулярные выражения для проверки строк на наличие цифр (для артикулов).
        /// </summary>
        private static readonly Regex[] receivingRegeхes = receivingRegeхPatterns.CreateRegexArray();

        private static readonly Regex[] writeOffByRegex = RESORT_ORDER_PATTERN.CreateRegexArray();


        /// <summary>
        /// Дополнительные правила (баланс, "1 - Помещение приемки и сортировки")
        /// </summary>
        private static readonly AdditionalRangeRules additionalRangeRulesReceiving01 =
            new(keyStringColumn: 1, keyStringsRegexes: digitsRegexes, isGetDataByKeyString: false,
            childStringColumn: 1, childStringRegexes: receivingRegeхes);

        /// <summary>
        /// Дополительные правила (баланс, помещения 2-5)
        /// </summary>
        private static readonly AdditionalRangeRules additionalRangeRulesReceiving0205 =
            new(keyStringColumn: 1, keyStringsRegexes: digitsRegexes, isGetDataByKeyString: true);

        #endregion

        #region Constructors && destructors
        public ExcelSheetParsingManager(IConfiguration config, ILogger<ExcelSheetParsingManager> logger, IOptions<ProgramOptions> options) : base(config)
        {
            ExcelPackage.License.SetNonCommercialPersonal("vegiwoo");
        }
        #endregion

        #region Functionality
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
        /// Определение заготовок диапазонов (для данных баланса) в таблицах исходных отчетов по их ключу в заданном листе книги Excel.
        /// </summary>
        /// <param name="sheet">Лист рабочей книги Excel.</param>
        /// <returns>Коллекция заготовок диапазонов для баланса.</returns>
        private static List<SourceReportRange> SetBalanceSourceReportRangeByKey(in ExcelWorksheet sheet)
        {
            List<SourceReportRange> result = [];

            var terminating = (LAST_MARKER_KEY, LAST_MARKER_KEY_OFFSET);

            GetIndexRowByStringKey(in sheet, FOUTH_ROOM_NAME, out int? fouthRoomIndex);
            GetIndexRowByStringKey(in sheet, FIFTH_ROOM_NAME, out int? fifthRoomIndex);

            result.Add(new(RemainingFirstRoomSourceCells, FIRST_ROOM_NAME, additionalRangeRules: additionalRangeRulesReceiving01));
            result.Add(new(RemainingFromSecondToFifthRoomSourceCells, SECOND_ROOM_NAME, additionalRangeRules: additionalRangeRulesReceiving0205));
            result.Add(new(RemainingFromSecondToFifthRoomSourceCells, THIRD_ROOM_NAME,
                terminatingStringValue: !fouthRoomIndex.HasValue || fifthRoomIndex.HasValue ? terminating : null,
                additionalRangeRules: additionalRangeRulesReceiving0205));

            if (fouthRoomIndex.HasValue)
                result.Add(new(RemainingFromSecondToFifthRoomSourceCells, FOUTH_ROOM_NAME,
                terminatingStringValue: !fifthRoomIndex.HasValue ? terminating : null,
                additionalRangeRules: additionalRangeRulesReceiving0205));

            if (fifthRoomIndex.HasValue)
                result.Add(new(RemainingFromSecondToFifthRoomSourceCells, FIFTH_ROOM_NAME,
                terminatingStringValue: terminating,
                additionalRangeRules: additionalRangeRulesReceiving0205));

            return result;
        }

        /// <summary>
        /// Определение заготовок диапазонов в таблицах исходных отчетов по их ключу в заданном листе книги Excel.
        /// </summary>
        /// <param name="key">Ключу исходного отчета.</param>
        /// <param name="sheetName">Имя листа книги.</param>
        /// <returns>Флаг определения (коллекция набора ячеек диапазона не пуста.)</returns>
        private static Task<List<SourceReportRange>> SetSourceReportRangeByKey(in ExcelWorksheet sheet, SourceReportItemKey key, string sheetName)
        {
            List<SourceReportRange> result = key switch
            {
                SourceReportItemKey.Ransoms => [new(RansomsSourceCells)],
                SourceReportItemKey.Balance => SetBalanceSourceReportRangeByKey(in sheet),
                SourceReportItemKey.WriteOffPoints => [new(WriteOffPointsAndCouriersSourceCells)],
                SourceReportItemKey.WriteOffPointsViaWarehouseShortage => [new(WriteOffPointsViaWarehouseSourceCells)],
                SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment => [new(WriteOffPointsViaWarehouseSourceCells)],
                SourceReportItemKey.OutgoingProducts => [new(ShippedSourceCells)],
                SourceReportItemKey.Transfered => [new(TransferSourceCells)],
                SourceReportItemKey.Additional => sheetName switch
                {
                    ADDREPORT_ALIASES_WORKSHEET_NAME => [new(AliasesSourceCells)],
                    ADDREPORT_RESORTS_WORKSHEET_NAME => [new(ResortsSourceCells)],
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
            // Поиск дипазонов с названиями (например, помещений)
            IEnumerable<SourceReportRange>? rangesWithKeys = rangeRows
                .Where(rs => !string.IsNullOrEmpty(rs.StringRowKey));

            // Поиск дипазона с терминатором (разделителями)
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
                SetRowRange(in worksheet, ref rangeRows, rangeWithTermination?.TerminatingStringValue);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Определяет диапазон строк 
        /// </summary>
        /// <param name="sheet">Лист книги Excel.</param>
        /// <param name="rangesRows"></param>
        /// <param name="termination"></param>
        /// <exception cref="ArgumentException"></exception>
        private static bool SetRowRange(in ExcelWorksheet sheet, ref List<SourceReportRange> rangesRows, (string rowStringKey, int offset)? termination)
        {
            // Получение коллекции кортежей по ключевым строкам (строковые ключи и индексы) для поиска диапазонов.
            Task getKeystringsTask = GetKeyStringsOfRanges(sheet: sheet, rangeSourceReportRows: rangesRows, out List<(string key, int index)>? keyStringsRanges);
            Task.Run(() => getKeystringsTask);
            getKeystringsTask.Wait();
            if (keyStringsRanges is null) return false; // TODO: Сообщение в лог? Ошибка? 

            // Поиск начальных и количества строк с данными диапазонов.
            Task setRowsTask = SetStartRowsIndexesAndNumbersRowsOfDataInRanges(in sheet, ref rangesRows, in termination, in keyStringsRanges);
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
        /// Определяет коллекцию индексов строк таблицы для парсинга данных.
        /// </summary>
        /// <param name="rowsRange">Диапазон строк таблицы.</param>
        /// <param name="keyRowsIndexRange">Коллекция индексов ключевых строк.</param>
        /// <param name="childRowsIndexDict">Словарь (ключи - индексов ключевых строк, значения - список дочерних строк к ключеваым).</param>
        private static Task GetRowIndicesForParsing(in SourceReportRange rowsRange, out List<int>? keyRowsIndexRange, out Dictionary<int, List<int>>? childRowsIndexDict)
        {
            keyRowsIndexRange = null;
            childRowsIndexDict = null;

            // Если у rowsRange есть доп. правила 
            if (rowsRange.AdditionalRangeRules.HasValue)
            {
                // Если получаем данные только по ключевым строкам и их коллекция не пустая
                if (rowsRange.AdditionalRangeRules.Value.IsGetDataByKeyString &&
                    rowsRange.AdditionalRangeRules.Value.ValidKeyRowIndices.Count > 0)
                {
                    keyRowsIndexRange = rowsRange.AdditionalRangeRules.Value.ValidKeyRowIndices;
                }

                // Если получаем данные по дочерним строкам, словарь (ключевых и дочерних строк) не пустой
                if (!rowsRange.AdditionalRangeRules.Value.IsGetDataByKeyString &&
                    rowsRange.AdditionalRangeRules.Value.ValidChildRowIndices.Count > 0)
                {
                    childRowsIndexDict = rowsRange.AdditionalRangeRules.Value.ValidChildRowIndices;
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Получение данных из диапазона таблицы.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowsRange">Заготовка диапазона таблицы.</param>
        /// <param name="keyRowsRange">Коллекция номеров ключевых строк (опциолнально).</param>
        /// <returns></returns>
        private static Task GetDataFromRange(in ExcelWorksheet sheet, ref SourceReportRange rowsRange, List<int>? keyRowsRange = null)
        {
            // Получение заготовок ячеек
            var currentSourceCells = rowsRange.SourceReportCells;

            // Определение коллекции индексов строк для парсинга данных
            // - если не заданы индексы, берем rowsRange.RowsIndexes как список
            List<int>? rowsIndexes = keyRowsRange ??
                (rowsRange.RowsIndexes != null ? [.. rowsRange.RowsIndexes] : []);

            // Итерация по коллекции индексов строк
            for (int i = 0; i < rowsIndexes.Count; i++)
            {
                // Получение индекса строки
                int rowIndex = rowsIndexes[i];

                // Получение данных из строки по индексу
                var getValuesFromRowTask = GetValuesFromRow(in sheet, rowIndex, in currentSourceCells, out RowData? rowData);
                Task.Run(() => getValuesFromRowTask);
                getValuesFromRowTask.Wait();

                // Если операция прошла успешно и данные получены - записываем их
                if (getValuesFromRowTask.Result && rowData.HasValue)
                    rowsRange.SetRowsData(rowData.Value);
            }

            return Task.CompletedTask;
        }

        public static Task<bool> GetValuesFromRow(in ExcelWorksheet sheet, int rowNumber, in List<SourceReportCell> sourceCells, out RowData? rowData)
        {
            rowData = null;

            if (!CheckPresenceAddressesInRow(in sheet, rowNumber, in sourceCells) ||
                !СheckingValuesInRequiredCells(in sheet, rowNumber, in sourceCells) ||
                !FilterMatchingCheck(in sheet, rowNumber, in sourceCells, out int filterCellsCount) ||
                !FindSorceCellsByType(in sourceCells, SourceCellsType.NonFiltration, out List<SourceReportCell>? dataСellsNonFilters) ||
                dataСellsNonFilters is null ||
                dataСellsNonFilters!.Count == 0)
            {
                return Task.Run(() => false);
            }

            // Getting values
            Dictionary<string, TableParsingCell> rowValues = [];
            foreach (var dataCell in dataСellsNonFilters)
            {
                var colNumber = dataCell.ColumnNumber;
                GetNonCompositeValue(in sheet, rowNumber, ref rowValues, in dataCell);
            }

            rowData = new(rowNumber: rowNumber, rowValues: rowValues);

            var success = sheet.Name == ADDREPORT_ALIASES_WORKSHEET_NAME ?
                rowData.HasValue :
                rowValues?.Count == sourceCells.Count - filterCellsCount;

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

            // Если получаем данные по ключевым строкам, но ничего больше не требуется.

            // Если получаем данные по дочерним строкам к ключевой:
            if (!range.AdditionalRangeRules!.Value.IsGetDataByKeyString)
            {
                // Заполняем словарь индексов (ключевая строка, дочерние строки)
                GetChildRowIndices(ref range);

                // Оставляем в индексах дочерних строк только соответствующие регуляным выражениям.
                RemoveIndexesInvalidChildRows(in sheet, ref range);
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
        public static void GetChildRowIndices(ref SourceReportRange range)
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
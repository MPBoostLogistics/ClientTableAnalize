using System.Diagnostics;
using OfficeOpenXml;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    public partial class ExcelSheetParsingManager : BaseManager
    {
        /// <summary>
        /// Получает данные из таблицы Excel.
        /// </summary>
        /// <param name="reportFileKey">Ключ файла отчета.</param>
        /// <param name="package">Пакет (книга) Excel.</param>
        /// <param name="sheetName">Имя рабочего листа книги Excel.</param>
        /// <returns>Время выполнения задачи и перечисление словарей (ключ - имя будущего свойства, значение - ячейка парсинга таблицы.)).</returns>
        public async Task<(double, IEnumerable<RowData>?)> GetDataFromExcelSheet(SourceReportItemKey reportFileKey, ExcelPackage package, string sheetName)
        {
            // 0. Start
            Stopwatch stopwatch = Stopwatch.StartNew();

            // 1. Получение целевого листа из книги Excel.
            if (!GetSheetFromExcelWorkbookByName(in package, sheetName, out ExcelWorksheet? sheet))
                return (StopFuctionExecuting(ref stopwatch), null); // Сообщение в лог, что-то еще вернуть!

            // 2. Получение коллекции диапазонов из целевого листа по ключу отчета.
            Task<List<SourceReportRange>> setTargetTableColumnsTask = SetSourceReportRangeByKey(in sheet!, reportFileKey, sheetName);
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
            var rowsRangesWithRules = rowsRanges
                .Where(rr => rr.AdditionalRangeRules.HasValue)
                .ToList();

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

            // 4. Получение данных из строк диапазонов
            Task getRawDataTask = GetDataFromSourceReportRanges(in reportFileKey, in sheet!, ref rowsRanges);
            await Task.Run(() => getRawDataTask);
            getRawDataTask.Wait();

            // 5. Объединение коллекций
            var items = from sourceReportRowsRange in rowsRanges
                        from item in sourceReportRowsRange.RowsData
                        select item;

            // Stop and return
            return (StopFuctionExecuting(ref stopwatch), items.Distinct(new RowDataComparer()));
        }

        /// <summary>
        /// Получает данные из дочерних строк диапазонов таблицы Excel относительно родительских.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowsRanges">Коллекция заготовок-дипазонов.</param>
        /// <returns>Флаг выполнения операции.</returns>
        public Task GetDataFromSourceReportRanges(in SourceReportItemKey reportFileKey, in ExcelWorksheet sheet, ref List<SourceReportRange> rowsRanges)
        {
            // 1. Итерация по коллекции диапазонов таблицы (например, по помещениям склада).
            for (int i = 0; i < rowsRanges.Count; i++)
            {
                SourceReportRange? currentRowsRange = rowsRanges[i];

                if (currentRowsRange is null || currentRowsRange.RowsIndexes is null) continue;

                // 2. Определение коллекций строк для парсинга.
                Task getRowIndicesForParsingTask = GetRowIndicesForParsing(in currentRowsRange,
                    out List<int>? keyRowsIndexRange,
                    out Dictionary<int, List<int>>? childRowsIndexDict);
                Task.Run(() => getRowIndicesForParsingTask);
                getRowIndicesForParsingTask.Wait();

                // 3. Получение данных
                Task? getDataTask = null;
                switch (keyRowsIndexRange != null, childRowsIndexDict != null)
                {
                    // Если коллекции пустые, получаем данные по RowsIndexes в SourceReportRange
                    case (false, false):
                        OnManagerReached(new($"Ключ {reportFileKey}, {currentRowsRange.StringRowKey}: значения без правил и по всем строкам...", false, false));
                        getDataTask = GetDataFromRange(in sheet, ref currentRowsRange);
                        break;
                    // Если есть только коллекция ключевых строк, получаем данные по keyRowsIndexRange правил в SourceReportRange
                    case (true, false):
                        OnManagerReached(new($"Ключ {reportFileKey}, {currentRowsRange.StringRowKey}: значения по правилам и только по ключевым строкам...", false, false));
                        getDataTask = GetDataFromRange(in sheet, ref currentRowsRange, keyRowsIndexRange);
                        break;
                    // Если есть только коллекция дочерних строк, получаем данные по childRowsIndexDict правил в SourceReportRange
                    case (false, true):
                        OnManagerReached(new($"Ключ {reportFileKey}, {currentRowsRange.StringRowKey}: значения по правилам, словарь ключевая строка - дочерние строки...", false, false));
                        getDataTask = GetValuesFromTableRange(in sheet, ref currentRowsRange, childRowsIndexDict!);
                        break;
                    // Если обе коллекции не пусыте, то это ошибка!
                    case (true, true):
                        OnManagerReached(new($"Ключ {reportFileKey}, {currentRowsRange.StringRowKey}: получение значений невозможно, есть обе коллекции", true, true));
                        break;
                }

                if (getDataTask != null)
                {
                    Task.Run(() => getDataTask);
                    getDataTask.Wait();

                    // Перезапись диапазона
                    rowsRanges[i] = currentRowsRange;
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Получение данных из диапазона таблицы (перегрузка метода).
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowsRange">Заготовка диапазона таблицы.</param>
        /// <param name="keyAndChildRowsIndexDict">Словарь (ключи - номера родительских строк, значения - коллекции номеров дочерних строк).</param>
        /// <returns>Окончение выполнения операции.</returns>
        private static Task GetValuesFromTableRange(in ExcelWorksheet sheet, ref SourceReportRange rowsRange, Dictionary<int, List<int>> keyAndChildRowsIndexDict)
        {
            // Получение заготовок ячеек
            var currentSourceCells = rowsRange.SourceReportCells;

            // Итерация по словарю (ключ - индекс ключевой строки, значения - список индексов дочерних строк)
            foreach (KeyValuePair<int, List<int>> entry in keyAndChildRowsIndexDict)
            {
                // Получение данных из строки по индексу
                var getValuesFromRowTask = GetValuesFromTableRow(in sheet, entry, in currentSourceCells, out RowData? rowData);
                Task.Run(() => getValuesFromRowTask);
                getValuesFromRowTask.Wait();

                // Если операция прошла успешно и данные получены - записываем их
                if (rowData.HasValue)
                    rowsRange.SetRowsData(rowData.Value);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Получение значений из строки.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="entry">Пара (ключ - номер родительской строки, значение - коллекция дочерних строк).</param>
        /// <param name="sourceCells">Коллекция ячеек-шаблонов для парсинга.</param>
        /// <param name="values">Выходной словарь значений (ключ - имя будущего ствойства, значение - объект ItemForParsing)</param>
        /// <returns>Окончание задачи.</returns>
        private static Task GetValuesFromTableRow(in ExcelWorksheet sheet, in KeyValuePair<int, List<int>> entry, in List<SourceReportCell> sourceCells, out RowData? rowData)
        {
            Dictionary<string, TableParsingCell>? values = [];

            // Проход по ячейкам - заготовкам исходного отчета
            foreach (var sourceCell in sourceCells)
            {
                if (!sourceCell.IsGetCompositeDataForCell)
                    GetNonCompositeValue(in sheet, rowNumber: entry.Key, ref values, sourceCell);
                else
                    GetCompositeValue(in sheet, rows: entry, ref values, sourceCell);
            }

            if (values.Count > 0)
                rowData = new(rowNumber: entry.Key, values);
            else
                rowData = null;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Получение композитного (составного) значения для свойства.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="entry">Пара (ключ - номер родительской строки, значение - коллекция дочерних строк).</param>
        /// <param name="values">Выходной словарь значений (ключ - имя будущего ствойства, значение - объект ItemForParsing)</param>
        /// <param name="sourceCell">Ячейка-шаблон лоя парсинга.</param>
        /// <exception cref="ArgumentNullException">Обязательгное значение ячейки оказалось нулевым.</exception>
        private static void GetCompositeValue(in ExcelWorksheet sheet, in KeyValuePair<int, List<int>> rows, ref Dictionary<string, TableParsingCell> values, in SourceReportCell sourceCell)
        {
            var colNumber = sourceCell.ColumnNumber;

            int compositeCount = default;
            foreach (var childRowNumber in rows.Value)
            {
                if (GetValueFromTableCell(in sheet, (childRowNumber, colNumber), out string? stringValue))
                {
                    if (string.IsNullOrEmpty(stringValue) && sourceCell.IsRequired)
                        throw new ArgumentNullException($"A table cell does not contain a <b>required value</b> for name: '{sourceCell.NameOfFutureProperty}'");

                    if (!string.IsNullOrEmpty(stringValue) &&
                        sourceCell.TypeCodeKeyRow == ParsingTypeCode.IntTypeCode &&
                        int.TryParse(stringValue, out int v))

                        compositeCount += v;
                }
            }

            if (compositeCount != 0)
                values.Add(sourceCell.NameOfFutureProperty, new(compositeCount.ToString(), sourceCell.TypeCodeKeyRow, !sourceCell.IsRequired));
        }

        /// <summary>
        /// Получение некомпозитного (несоставного) свойства.
        /// </summary>
        /// <param name="sheet">Рабочий лист книги Excel.</param>
        /// <param name="rowNumber">Номер строки длоя парсинга.</param>
        /// <param name="values">Выходной словарь значений (ключ - имя будущего ствойства, значение - объект ItemForParsing)</param>
        /// <param name="sourceCell">Ячейка-шаблог лоя парсинга.</param>
        /// <exception cref="ArgumentNullException">Обязательгное значение ячейки оказалось нулевым.</exception>
        private static void GetNonCompositeValue(in ExcelWorksheet sheet, int rowNumber, ref Dictionary<string, TableParsingCell> values, in SourceReportCell sourceCell)
        {
            var colNumber = sourceCell.ColumnNumber;

            if (GetValueFromTableCell(in sheet, (rowNumber, colNumber), out string? stringValue))
            {
                if (string.IsNullOrEmpty(stringValue))
                {
                    if (sourceCell.IsRequired)
                        throw new ArgumentNullException($"A table cell does not contain a <b>required value</b> for name: '{sourceCell.NameOfFutureProperty}'");

                    stringValue = string.Empty;
                }

                values.Add(sourceCell.NameOfFutureProperty, new(stringValue, sourceCell.TypeCodeKeyRow, !sourceCell.IsRequired));
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
    }
}
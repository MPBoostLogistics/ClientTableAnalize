using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{

    public class StringObject
    {
        public string Name { get; set; }

        public StringObject(string name)
        {
            Name = name;
        }
    }

    public class RowDataComparer : IEqualityComparer<RowData>
    {
        public bool Equals(RowData x, RowData y) =>
            x.RowNumber == x.RowNumber && x.RowValues == y.RowValues;

        public int GetHashCode([DisallowNull] RowData rd) => rd.RowNumber.GetHashCode();
    }


    public readonly struct RowData(int rowNumber, Dictionary<string, TableParsingCell> rowValues)
    {
        #region Properties
        public int RowNumber { get; } = rowNumber;
        public Dictionary<string, TableParsingCell> RowValues { get; } = rowValues;
        #endregion
    }



    /// <summary>
    /// Описывает диапазон строк с данными из исходного отчета.
    /// </summary>
    public class SourceReportRange(List<SourceReportCell> sourceCells,
        string? key = null, (string key, int offset)? terminatingStringValue = null,
        AdditionalRangeRules? additionalRangeRules = null)
    {
        #region Properties
        /// <summary>
        /// Ключ в виде строки
        /// - содержит название диапазона (например, помещения)
        /// - может быть пустым (если существует только один диапазон)
        /// </summary>
        public string? StringRowKey { get; } = key;

        /// <summary>
        /// Строковое значение, завершающее расчет индекса последней строки (может быть пустым).
        /// </summary>
        public (string key, int offset)? TerminatingStringValue { get; } = terminatingStringValue;

        /// <summary>
        /// Коллекция объектов SourceReportCells (описывает ячейки диапазона таблицы для парсинга).
        /// </summary>
        public List<SourceReportCell> SourceReportCells { get; } = sourceCells;

        /// <summary>
        /// Перечисление индексов строк диапазона.
        /// </summary>
        public IEnumerable<int>? RowsIndexes { get; set; }

        /// <summary>
        /// Коллекция прочинанных строк.
        /// Каждая строка представляет собой словарь значений, где ключ - имя будущего свойства,
        /// а значение - объект ItemForParsing
        /// </summary>
        public List<RowData> RowsData { get; private set; } = [];

        /// <summary>
        /// Дополнитетельные правила для парсинга диапазона.
        /// </summary>
        public AdditionalRangeRules? AdditionalRangeRules { get; set; } = additionalRangeRules;

        #endregion Properties

        #region Functionality
        /// <summary>
        /// Устанавливает начальный индекс строки и количество строк с данными в диапазоне.
        /// </summary>
        /// <param name="startIndexDataRow">Начальный индекс строки с данными в диапазоне.</param>
        /// <param name="numberRowsWithData">Количество строк с данными в диапазоне.</param>
        public void SetStartRowIndexAndNumberRowsOfData(int startIndexDataRow, int numberRowsWithData) =>
            RowsIndexes = Enumerable.Range(start: startIndexDataRow, count: numberRowsWithData);

        /// <summary>
        /// Записывает данные по прочитанным строкам.
        /// </summary>
        /// <param name="values">Словарь, где ключ - имя свойства, а значение - объект TableParsingCell</param>
        public void SetRowsData(RowData rowData) => RowsData.Add(rowData);

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(string.IsNullOrEmpty(StringRowKey) ? "" : $"key: {StringRowKey}, ");
            sb.Append(string.IsNullOrEmpty(TerminatingStringValue?.key) ? "" : $"terminating: {TerminatingStringValue?.key}, ");
            sb.Append($"Source columns count: {SourceReportCells.Count}, ");
            sb.Append(RowsIndexes != null ?
                 $"range row indexes count: {RowsIndexes.Count()}, " : "");

            return sb.ToString();
        }

        #endregion Functionality
    }

    /// <summary>
    /// Дополнительные правила для обработки диапазона (поиска необходимых строк).
    /// </summary>
    public readonly struct AdditionalRangeRules(int keyStringColumn, Regex[] keyStringsRegexes,
        bool isGetDataByKeyString, int? childStringColumn = null, Regex[]? childStringRegexes = null)
    {
        /// <summary>
        /// Номер столбца таблицы для поиска ключевых строк.
        /// </summary>
        public int KeyStringColumn { get; } = keyStringColumn;

        /// <summary>
        /// Паттерн для поиска ключевых строк.
        /// </summary>
        public Regex[] KeyStringRegex { get; } = keyStringsRegexes;

        /// <summary>
        /// Флаг получения данных только по ключевой строке.
        /// </summary>
        public bool IsGetDataByKeyString { get; } = isGetDataByKeyString;

        /// <summary>
        /// Список индексов строк, содержащих ключевые значения.
        /// </summary>
        public List<int> ValidKeyRowIndices { get; } = [];

        /// <summary>
        /// Регулярные выражения для поиска дочерних строк.
        /// </summary>
        public Regex[]? ChildStringRegexes { get; } = childStringRegexes;

        /// <summary>
        /// Номер столбца таблицы для поиска дочерних строк.
        /// </summary>
        public int? ChildStringColumn { get; } = childStringColumn;

        /// <summary>
        /// Словарь дочерних строк (ключ - индекс ключевой строки, значение - список индексов дочерних строк).
        /// </summary>
        public Dictionary<int, List<int>> ValidChildRowIndices { get; } = [];

        #region Functionality
        public readonly void SetValidKeyRowIndices(int index) => ValidKeyRowIndices.Add(index);

        #endregion
    }
}
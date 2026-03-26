using System.Text;
using System.Text.RegularExpressions;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
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
        /// Полученный набор данных по прочитанным строкам (каждая строка представляет собой словарь значений).
        /// </summary>
        public List<Dictionary<string, ItemForParsing>> RowsData { get; private set; } = [];

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

        public void SetRowsData(Dictionary<string, ItemForParsing> values) => RowsData.Add(values);

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
    public readonly struct AdditionalRangeRules(int keyStringColumn, Regex[] keyStringsRegex,
        bool isGetDataByKeyString, int? childStringColumn = null, Regex[]? childStringRegexes = null)
    {
        /// <summary>
        /// Номер столбца таблицы для поиска ключевых строк.
        /// </summary>
        public int KeyStringColumn { get; } = keyStringColumn;

        /// <summary>
        /// Паттерн для поиска ключевых строк.
        /// </summary>
        public Regex[] KeyStringRegex { get; } = keyStringsRegex;

        /// <summary>
        /// Флаг получения данных только по ключевой строке.
        /// </summary>
        public bool IsGetDataByKeyString { get; } = isGetDataByKeyString;

        /// <summary>
        /// Список индексов строк, содержащих ключевые значения.
        /// </summary>
        public List<int> ValidKeyRowIndices { get; } = [];

        /// <summary>
        /// Паттерн для поиска ключевых строк.
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
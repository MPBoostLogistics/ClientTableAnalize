using System.Text;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    /// <summary>
    /// Диапазон строк с данными исходного отчета.
    /// </summary>
    public class RangeRowsWithDataInSourceReport(List<SourceCell> sourceCells, string? key = null, (string key, int offset)? terminatingStringValue = null)
    {
        #region Properties
        /// <summary>
        /// Ключ в виде строки содержит название диапазона (например, помещения),
        /// может быть пустым (если существует только один диапазон)
        /// </summary>
        public string? StringRowKey { get; } = key;

        /// <summary>
        /// Строковое значение, завершающее расчет индекса последней строки (может быть пустым).
        /// </summary>
        public (string key, int offset)? TerminatingStringValue { get; } = terminatingStringValue;

        /// <summary>
        /// Коллекция объектов SourceCell (описывает ячейки диапазона таблицы для парсинга).
        /// </summary>
        public List<SourceCell> SourceColumns { get; } = sourceCells;

        /// <summary>
        /// Перечисление индексов строк диапазона.
        /// </summary>
        public IEnumerable<int>? RangeRowIndexesСollection { get; set; }

        /// <summary>
        /// Полученный набор данных по прочитанным строкам (каждая строка представляет собой словарь значений).
        /// </summary>
        public List<Dictionary<string, ItemForParsing>> DataOnReadRowsCollection { get; private set; } = [];

        #endregion Properties

        #region Functionality
        /// <summary>
        /// Устанавливает начальный индекс строки и количество строк с данными в диапазоне.
        /// </summary>
        /// <param name="startIndexDataRow">Начальный индекс строки с данными в диапазоне.</param>
        /// <param name="numberRowsWithData">Количество строк с данными в диапазоне.</param>
        public void SetStartRowIndexAndNumberRowsOfData(int startIndexDataRow, int numberRowsWithData) =>
            RangeRowIndexesСollection = Enumerable.Range(start: startIndexDataRow, count: numberRowsWithData);

        public void SetDataOnReadRowsCollection(Dictionary<string, ItemForParsing> values) => DataOnReadRowsCollection.Add(values);

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(string.IsNullOrEmpty(StringRowKey) ? "" : $"key: {StringRowKey}, ");
            sb.Append(string.IsNullOrEmpty(TerminatingStringValue?.key) ? "" : $"terminating: {TerminatingStringValue?.key}, ");
            sb.Append($"Source columns count: {SourceColumns.Count}, ");
            sb.Append(RangeRowIndexesСollection != null ?
                 $"range row indexes count: {RangeRowIndexesСollection.Count()}, " : "");

            return sb.ToString();
        }

        #endregion Functionality
    }
}
namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    /// <summary>
    /// Ячейка парсинга таблицы.
    /// </summary>
    /// <param name="value">Значение ячейки.</param>
    /// <param name="parsingTypeCode">Тип данных для ячейки.</param>
    /// <param name="isMaybeNullable">Может ли быть пустой.</param>
    public class TableParsingCell(string? value, ParsingTypeCode parsingTypeCode, bool isMaybeNullable = false)
    {
        #region Properties
        public string? Value { get; } = value;
        public ParsingTypeCode TypeCode { get; } = parsingTypeCode;
        public bool IsMaybeNullable { get; } = isMaybeNullable;
        #endregion
    }
}
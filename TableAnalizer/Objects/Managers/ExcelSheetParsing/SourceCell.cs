namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    /// <summary>
    /// Представляет ячейку для получения данных из исходного отчета.
    /// </summary>
    /// <param name="columnNumber">Номер столбца для ячейки.</param>
    /// <param name="CellName">Название значения.</param>
    /// <param name="parsingTypeCode">Тип значения для парсинга.</param>
    /// <param name="IsRequired">Обязательность значений в ячейке.</param>
    /// <param name="filterStrings">Набор строк для фильтрации данных ячейки.</param>
    public readonly struct SourceCell(int columnNumber, string cellName, ParsingTypeCode parsingTypeCode, bool isRequired, string[]? filterStrings = null)
    {
        #region Properties
        /// <summary>
        /// Номер столбца
        /// </summary>
        public int ColumnNumber { get; } = columnNumber;
        /// <summary>
        /// Имя столбца
        /// </summary>
        public string CellName { get; } = cellName;
        /// <summary>
        /// Код парсинга типа
        /// </summary>
        public ParsingTypeCode ParsingTypeCode { get; } = parsingTypeCode;
        /// <summary>
        /// Флаг обязательного значения
        /// </summary>
        public bool IsRequired { get; } = isRequired;
        /// <summary>
        /// Строковые значеения для фильтрации
        /// </summary>
        public string[]? FilterStrings { get; } = filterStrings;
        #endregion
    }
}
using System.Text.RegularExpressions;
using TableAnalizer.Objects;

namespace TableAnalizer.Objects.Managers.ExcelSheetParsing
{
    /// <summary>
    /// Ячейка исходного отчета.
    /// </summary>
    /// <param name="columnNumber">Номер столбца в таблице.</param>
    /// <param name="nameOfFutureProperty">Имя будущего свойства для значения.</param>
    /// <param name="typeCodeKeyRow">Тип данных ключевой строки.</param>
    /// <param name="isRequired">Значение обязательно.</param>
    /// <param name="filterStrings">Коллекция строк для фильтрации.</param>
    /// <param name="typesAndRegeхsChildRows">Словарь типов и регулярных выражений для проверки дочерних строк (опционально.)</param>
    public readonly struct SourceReportCell(int columnNumber, string nameOfFutureProperty, ParsingTypeCode typeCodeKeyRow,
        bool isRequired, string[]? filterStrings, Dictionary<ParsingTypeCode, Regex>? typesAndRegeхsChildRows)
    {
        #region Properties
        public int ColumnNumber { get; } = columnNumber;
        public string NameOfFutureProperty { get; } = nameOfFutureProperty;
        public ParsingTypeCode TypeCodeKeyRow { get; } = typeCodeKeyRow;
        public bool IsRequired { get; } = isRequired;
        public string[]? FilterStrings { get; } = filterStrings;
        public Dictionary<ParsingTypeCode, Regex>? TypesAndRegeхsChildRows { get; } = typesAndRegeхsChildRows;
        #endregion
    }
}
using System.Drawing;
using OfficeOpenXml.Style;

namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Настройки для ячейки итогового отчета.
    /// </summary>
    public class FinalReportCellSettings
    {
        public Color TextСolor { get; }
        public Color? CellFillColor { get; private set; }
        public bool IsTextBold { get; }
        public int FontSize { get; }
        public ExcelFillStyle ExcelFillStyle { get; private set; }

        public FinalReportCellSettings(Color textСolor, int textSize, Color? cellFillColor, bool isTextBold = false)
        {
            TextСolor = textСolor;
            FontSize = textSize;
            IsTextBold = isTextBold;

            SetCellFillColor(cellFillColor);
        }

        public void SetCellFillColor(Color? color)
        {
            switch (color is null)
            {
                case true:
                    ExcelFillStyle = ExcelFillStyle.None;
                    break;
                case false:
                    ExcelFillStyle = ExcelFillStyle.Solid;
                    CellFillColor = color;
                    break;
            }
        }
    }

    /// <summary>
    /// Ячейка для итогового отчета.
    /// </summary>
    public readonly struct FinalReportCell(string columnName, float columnWidth, string columnFormat,
        bool isWrapTextInHeader, int degreeRotationTextInHeader, string сommentTextInHeader, bool isCellUsedInClientReport,
        string propertyName, ParsingTypeCode parsingTypeCode, Predicate<int>? valueСheckingPredicate,
        FinalReportCellSettings targetCellSettings, TypesFormulas? typeFormula, int groupCellsFromCurrent = 0)
    {
        #region Properties> 
        // public int ColNumber { get; } = colNumber;
        /// <summary>
        /// Имя колонки
        /// </summary>
        public string ColumnName { get; } = columnName;
        /// <summary>
        /// Ширина колонки
        /// </summary>
        public float ColumnWidth { get; } = columnWidth;
        /// <summary>
        /// Строковое представдение формата значения в колонке.
        /// </summary>
        public string ColumnFormat { get; } = columnFormat;
        /// <summary>
        /// Переносить текст в заголовке.
        /// </summary>
        public bool IsWrapTextInHeader { get; } = isWrapTextInHeader;
        /// <summary>
        /// Градус вращения текста в заголовке.
        /// </summary>
        public int DegreeRotationTextInHeader { get; } = degreeRotationTextInHeader;
        /// <summary>
        /// Текст комментария в заголовке.
        /// </summary>
        public string CommentTextInHeader { get; } = сommentTextInHeader;
        /// <summary>
        /// Имя свойства.
        /// </summary>
        public string PropertyName { get; } = propertyName;
        /// <summary>
        /// Тип значений в ячейке.
        /// </summary>
        public ParsingTypeCode ParsingTypeCode { get; } = parsingTypeCode;
        /// <summary>
        /// Предикат, проверящий значение.
        /// </summary>
        public readonly Predicate<int>? ValueСheckingPredicate = valueСheckingPredicate;
        /// <summary>
        /// Тип формулы для подсчета итоговых значений.
        /// </summary>
        public TypesFormulas? TypeFormula { get; } = typeFormula;
        /// <summary>
        /// Настройки для ячейки итогового отчета.
        /// </summary>
        public readonly FinalReportCellSettings CellSettings { get; } = targetCellSettings;
        /// <summary>
        /// Количество ячеек группировки от текущей. 
        /// </summary>
        public int GroupCellsFromCurrentCount { get; } = groupCellsFromCurrent;
        /// <summary>
        /// Ячейка участвует в клиентском отчете.
        /// </summary>
        public bool IsCellUsedInClientReport { get; } = isCellUsedInClientReport;
        #endregion
    }
}
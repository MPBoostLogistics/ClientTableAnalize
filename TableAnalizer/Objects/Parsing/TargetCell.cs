
using System.Drawing;
using OfficeOpenXml.Style;
using TableAnalizer.Objects.Enums;

namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents the result cell for an Excel table.
    /// </summary>
    public readonly struct TargetCell(int colNumber, string colName, double colWidth, string format,
        bool wrapTextInHeader, int textRotationInHeader, string propertyName, ParsingTypeCode parsingTypeCode,
        ExcelFillStyle excelFillStyle, Predicate<int>? valueСheckingPredicate, bool isBoldFont = false)
    {
        #region Properties
        public int ColNumber { get; } = colNumber;
        public string ColName { get; } = colName;
        public double ColumnWidth { get; } = colWidth;
        public string Format { get; } = format;
        public bool WrapTextInHeader { get; } = wrapTextInHeader;
        public int TextRotationInHeader { get; } = textRotationInHeader;
        public string PropertyName { get; } = propertyName;
        public ParsingTypeCode ParsingTypeCode { get; } = parsingTypeCode;
        public ExcelFillStyle ExcelFillStyle { get; } = excelFillStyle;
        public readonly Predicate<int>? ValueСheckingPredicate = valueСheckingPredicate; 
        public Color HighlightTextСolor { get; } = ColorTranslator.FromHtml("#72bcd4");
        public bool IsBoldFont { get; } = isBoldFont;
        #endregion
    }
}
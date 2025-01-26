using System.Drawing;
using OfficeOpenXml.Style;
using TableAnalizer.Objects.Enums;

namespace TableAnalizer.Objects.Parsing
{
    public class TargetCellSettings 
    {
        public Color TextСolor { get; }
        public Color? CellFillColor { get; }
        public bool IsTextBold { get; }
        public int TextSize { get; }
        public ExcelFillStyle ExcelFillStyle { get; }

        public TargetCellSettings(Color textСolor, int textSize, Color? cellFillColor,  bool isTextBold = false)
        {
            TextСolor = textСolor;
            TextSize = textSize;
            IsTextBold = isTextBold;

            switch(cellFillColor is null) 
            {
                case true:
                    ExcelFillStyle = ExcelFillStyle.None;
                    break;
                case false:
                    ExcelFillStyle = ExcelFillStyle.Solid;
                    CellFillColor = cellFillColor;
                    break;
            }
        }
    }

    /// <summary>
    /// Represents the result cell for an Excel table.
    /// </summary>
    public readonly struct TargetCell(int colNumber, string colName, double colWidth, string format,
        bool wrapTextInHeader, int textRotationInHeader, string propertyName, ParsingTypeCode parsingTypeCode,
        Predicate<int>? valueСheckingPredicate, TargetCellSettings targetCellSettings)
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
        public readonly Predicate<int>? ValueСheckingPredicate = valueСheckingPredicate; 
        public readonly TargetCellSettings CellSettings { get; } = targetCellSettings;
        #endregion
    }
}
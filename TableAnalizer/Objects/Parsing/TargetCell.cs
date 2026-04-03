using System.Drawing;
using OfficeOpenXml.Style;

namespace TableAnalizer.Objects.Parsing
{
    public class TargetCellSettings
    {
        public Color TextСolor { get; }
        public Color? CellFillColor { get; private set; }
        public bool IsTextBold { get; }
        public int FontSize { get; }
        public ExcelFillStyle ExcelFillStyle { get; private set; }

        public TargetCellSettings(Color textСolor, int textSize, Color? cellFillColor, bool isTextBold = false)
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
    /// Represents the result cell for an Excel table.
    /// </summary>
    public readonly struct TargetCell(int colNumber, string colName, float colWidth, string format,
        bool wrapTextInHeader, int textRotationInHeader, string textCommentInHeader, string propertyName, ParsingTypeCode parsingTypeCode,
        Predicate<int>? valueСheckingPredicate, TargetCellSettings targetCellSettings, TypesFormulas? typeFormula, int groupCellsFromCurrent = 0)
    {
        #region Properties
        public int ColNumber { get; } = colNumber;
        public string ColName { get; } = colName;
        public float ColumnWidth { get; } = colWidth;
        public string Format { get; } = format;
        public bool WrapTextInHeader { get; } = wrapTextInHeader;
        public int TextRotationInHeader { get; } = textRotationInHeader;
        public string TextCommentInHeader { get; } = textCommentInHeader;
        public string PropertyName { get; } = propertyName;
        public ParsingTypeCode ParsingTypeCode { get; } = parsingTypeCode;
        public readonly Predicate<int>? ValueСheckingPredicate = valueСheckingPredicate;
        public TypesFormulas? TypeFormula { get; } = typeFormula;
        public readonly TargetCellSettings CellSettings { get; } = targetCellSettings;
        public int GroupCellsFromCurrentCount { get; } = groupCellsFromCurrent;
        #endregion
    }
}
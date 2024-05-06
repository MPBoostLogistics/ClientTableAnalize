using System.Drawing;
using ClientTableAnalizer.Enums;
using OfficeOpenXml.Style;

namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents the result cell for an Excel table.
    /// </summary>
    /// <remarks><i>New version.</i></remarks>
    public readonly struct TargetCell
    {
        #region Properties
        public int ColNumber { get; }
        public string ColName { get; }
        public double ColumnWidth { get; }
        public string Format { get; }
        public bool WrapTextInHeader { get; }
        public int TextRotationInHeader { get; }
        public string PropertyName { get;}
        public ParsingTypeCode ParsingTypeCode { get; }
        public ExcelFillStyle ExcelFillStyle { get;}
        public bool IsBoldFont { get; }

        #endregion

        #region Constructors
        public TargetCell(int colNumber, string colName, double colWidth, string format, 
            bool wrapTextInHeader, int textRotationInHeader, string propertyName, ParsingTypeCode parsingTypeCode, 
            ExcelFillStyle excelFillStyle, bool isBoldFont = false)
        {
            ColNumber = colNumber;
            ColName = colName;
            ColumnWidth = colWidth;
            Format = format;
            WrapTextInHeader = wrapTextInHeader;
            TextRotationInHeader = textRotationInHeader;
            PropertyName = propertyName;
            ParsingTypeCode = parsingTypeCode;
            ExcelFillStyle = excelFillStyle;
            IsBoldFont = isBoldFont;
        }
        #endregion
    }
}
namespace ClientTableAnalizer.Objects
{
    /// <summary>
    /// Represents a cell in a source table.
    /// </summary>
    public readonly struct SourceCell 
    {
        #region Properties
        public int ColumnNumber { get; }
        public Type ValueType { get; }
        public string? FilterString { get; }
        #endregion

        #region Constructors
        public SourceCell(int columnNumber, Type valueType,  string? filterString = null)
        {
            ColumnNumber = columnNumber;
            ValueType = valueType;
            FilterString = filterString;
        }
        #endregion
    }

// Тип который нужно получит по строке ExcelRowType


    internal readonly struct TargetCell
    {
        #region Properties
        public int ColumnNumber { get; }
        public string Name { get; }
        public double ColumnWidth { get; }
        public string Format { get; }
        public bool WrapTextInHeader { get; }
        public int TextRotationInHeader { get; }
        #endregion

        #region Constructors
        public TargetCell(int columnNumber, string name,double columnWidth,  string format, bool wrapTextInHeader, int textRotationInHeader)
        {
            ColumnNumber = columnNumber;
            Name = name;
            ColumnWidth = columnWidth;
            Format = format;
            WrapTextInHeader = wrapTextInHeader;
            TextRotationInHeader = textRotationInHeader;
        }
        #endregion
    }
}
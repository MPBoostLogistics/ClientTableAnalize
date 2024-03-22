namespace ClientTableAnalizer.Objects
{
    internal readonly struct ResultCell
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
        public ResultCell(int columnNumber, string name,double columnWidth,  string format, bool wrapTextInHeader, int textRotationInHeader)
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
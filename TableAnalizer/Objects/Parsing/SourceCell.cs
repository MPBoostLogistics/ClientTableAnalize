namespace ClientTableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents source cell of an Excel table.
    /// </summary>
    public readonly struct SourceCell 
    {
        #region Properties
        public int ColIndex { get; }
        public string Name { get; }
        public Type ValueType { get; }
        public string? FilterString { get; }
        #endregion

        #region Constructors
        public SourceCell(int colIndex, string name, Type valueType,  string? filterString = null)
        {
            ColIndex = colIndex;
            Name = name;
            ValueType = valueType;
            FilterString = filterString;
        }
        #endregion
    }
}
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
        public bool IsValueRequired { get; }
        public string? FilterString { get; }
        #endregion

        #region Constructors
        public SourceCell(int colIndex, string name, Type valueType,  bool isValueRequired, string? filterString = null)
        {
            ColIndex = colIndex;
            Name = name;
            ValueType = valueType;
            IsValueRequired = isValueRequired;
            FilterString = filterString;
        }
        #endregion
    }
}
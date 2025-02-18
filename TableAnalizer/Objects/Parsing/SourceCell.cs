namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents source column of an Excel table.
    /// </summary>
    public readonly struct SourceColumn 
    {
        #region Properties
        public int ColIndex { get; }
        public string Name { get; }
        public ParsingTypeCode ParsingTypeCode { get; }
        public bool IsValueRequired { get; }
        public string? FilterString { get; }
        #endregion

        #region Constructors
        public SourceColumn(int colIndex, string name, ParsingTypeCode parsingTypeCode,  bool isValueRequired, string? filterString = null)
        {
            ColIndex = colIndex;
            Name = name;
            ParsingTypeCode = parsingTypeCode;
            IsValueRequired = isValueRequired;
            FilterString = filterString;
        }
        #endregion
    }
}
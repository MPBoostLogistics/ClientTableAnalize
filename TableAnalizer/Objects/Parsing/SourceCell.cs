namespace TableAnalizer.Objects.Parsing
{
    /// <summary>
    /// Represents source column of an Excel table.
    /// </summary>
    public readonly struct SourceColumn(int colIndex, string name, ParsingTypeCode parsingTypeCode, bool isValueRequired, string[]? filterStrings = null)
    {
        #region Properties
        public int ColIndex { get; } = colIndex;
        public string Name { get; } = name;
        public ParsingTypeCode ParsingTypeCode { get; } = parsingTypeCode;
        public bool IsValueRequired { get; } = isValueRequired;
        public string[]? FilterStrings { get; } = filterStrings;
        #endregion
    }
}
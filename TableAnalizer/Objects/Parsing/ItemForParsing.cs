namespace TableAnalizer.Objects.Parsing
{
    public class ItemForParsing(string? value, ParsingTypeCode parsingTypeCode, bool isMaybeNullable = false)
    {
        #region Properties
        public string? Value { get; } = value;
        public ParsingTypeCode TypeCode { get; } = parsingTypeCode;
        public bool IsMaybeNullable { get; } = isMaybeNullable;
        #endregion
    }
}
namespace TableAnalizer.Objects.Parsing
{
    public class ItemForParsing 
    {
        public string? Value { get;}
        public ParsingTypeCode TypeCode { get;}
        public bool IsMaybeNullable { get; }

        public ItemForParsing(string? value, ParsingTypeCode parsingTypeCode, bool isMaybeNullable = false)
        {
            Value = value;
            TypeCode = parsingTypeCode;
            IsMaybeNullable = isMaybeNullable;
        }
    }
}
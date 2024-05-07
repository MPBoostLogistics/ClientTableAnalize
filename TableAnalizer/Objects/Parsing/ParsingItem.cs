using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.Parsing
{
    public class ParsingItem 
    {
        public string? Value { get;}
        public ParsingTypeCode TypeCode { get;}
        public bool IsMaybeNullable { get; }

        public ParsingItem(string? value, ParsingTypeCode parsingTypeCode, bool isMaybeNullable = false)
        {
            Value = value;
            TypeCode = parsingTypeCode;
            IsMaybeNullable = isMaybeNullable;
        }
    }
}
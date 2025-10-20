namespace TableAnalizer.Objects.Parsing
{
    public class DataFromConsole(string questionMessage, string errorMessage, ParsingTypeCode typeCode, bool isRequred)
    {
        #region Properties
        public string QuestionMessage { get; } = questionMessage;
        public string ErrorMessage { get; } = errorMessage;
        public ParsingTypeCode TypeCode { get; } = typeCode;
        public bool IsRequred { get; } = isRequred;
        #endregion
    }
}
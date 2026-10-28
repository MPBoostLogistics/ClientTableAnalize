using TableAnalizer.Objects;

namespace TableAnalizer.Services.Console
{
    public class DataFromConsole(string questionMessage, string errorMessage, ParsingTypeCode typeCode, bool isRequred)
    {
        #region Properties
        /// <summary>
        /// Сообщение вопроса к пользователю в консоли.
        /// </summary>
        public string QuestionMessage { get; } = questionMessage;
        /// <summary>
        /// Сообщение ошибки пользователя в консоли.
        /// </summary>
        public string ErrorMessage { get; } = errorMessage;
        /// <summary>
        /// Код парсинга типа данных.
        /// </summary>
        public ParsingTypeCode TypeCode { get; } = typeCode;
        /// <summary>
        /// Флаг обязательности заполнения.
        /// </summary>
        public bool IsRequred { get; } = isRequred;
        #endregion
    }
}
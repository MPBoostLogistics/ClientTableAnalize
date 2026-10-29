using TableAnalizer.Objects;

namespace TableAnalizer.Services.Console
{
    /// <summary>
    /// Сущность итерации ввода из командной строки.
    /// </summary>
    /// <param name="questionMessage">Сообщение вопроса к пользователю в консоли.</param>
    /// <param name="errorMessage">Сообщение ошибки пользователю в консоль.</param>
    /// <param name="typeCode">Код типа данных, получаемых из консоли.</param>
    /// <param name="isRequred">Флаг обязательного значения.</param>
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
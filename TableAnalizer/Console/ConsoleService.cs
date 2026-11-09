using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableAnalizer.Configuration;
using TableAnalizer.Objects;
using TableAnalizer.Services;
using TableAnalizer.Services.Console;

namespace TableAnalizer.Console
{
    public partial class ConsoleService : IConsoleService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<ConsoleService> _logger;
        private readonly IParsingService _parsingService;
        #endregion

        #region Properties
        public string ServiceName { get; private set; }
        #endregion

        #region Consructors and Destructors
        public ConsoleService(ILogger<ConsoleService> logger, IOptions<ProgramOptions> options, IParsingService parsingService)
        {
            _logger = logger;
            _options = options.Value;
            _parsingService = parsingService;

            ServiceName = GetType().Name;

            if (_options.IsTracing)
            {
                _logger.LogInformation("Создана служба {ServiceName}", ServiceName);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Получение первичных данных (пути в директории с данными клиента) из консоли от пользователя.
        /// </summary>
        /// <returns>Объект DirectoryInfo (опционально).</returns>
        /// <remarks>Шаг 2, Ревизия 112026.</remarks>
        public DirectoryInfo? GetPrimaryDataFromConsole()
        {
            DataFromConsole cletientReportPathData = new(
                questionMessage: "Укажите путь к директории с клиентскими данными: ",
                errorMessage: "Путь к директории с клиентскими данными не указан или указан неверно.\nПроверьте правильность ввода и повторите попытку.",
                typeCode: ParsingTypeCode.DirectoryTypeCode,
                isRequred: true);

            return GetValueFromConsole<DirectoryInfo?>(cletientReportPathData);
        }

        /// <summary>
        /// Получить значение обобщего типа из консоли.
        /// </summary>
        /// <typeparam name="T">Тип данных.</typeparam>
        /// <param name="data">Сущность интерации получения данных.</param>
        /// <returns>Значение с определеным типом.</returns>
        private T? GetValueFromConsole<T>(DataFromConsole data)
        {
            T? type = default;
            while (type is null)
            {
                string? value = GetStringFromConsole(data);

                if (!string.IsNullOrEmpty(value) &&
                    _parsingService.Parse(value.Trim('\''),
                    data.TypeCode,
                    data.IsRequred,
                    out object? resultObj) &&
                    resultObj is T info)
                {
                    type = info;
                    break;
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkRed;
                    System.Console.WriteLine("Значение не указано или указано неверно.\nПроверьте правильность ввода и повторите попытку.");
                    continue;
                }
            }
            return type;
        }

        private static string? GetStringFromConsole(DataFromConsole data)
        {
            string? value = null;
            while (value is null)
            {
                System.Console.Clear();
                System.Console.ForegroundColor = ConsoleColor.DarkBlue;
                System.Console.WriteLine(data.QuestionMessage);

                value = System.Console.ReadLine();
                if (!string.IsNullOrEmpty(value))
                {
                    break;
                }
                else
                {
                    System.Console.ForegroundColor = ConsoleColor.DarkRed;
                    System.Console.WriteLine(data.ErrorMessage);
                    GetStringFromConsole(data);
                }
            }

            return value;
        }
        #endregion
    }
}
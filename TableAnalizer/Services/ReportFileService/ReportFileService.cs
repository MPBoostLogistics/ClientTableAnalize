using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TableAnalizer.Configuration;
using TableAnalizer.Objects;
using TableAnalizer.Objects.ReportItems;

namespace TableAnalizer.Services
{
    public interface IReportFileService : IService
    {
        /// <summary>
        /// Директория на локальном компьютере с постоянными данными для отчетов.
        /// </summary>
        string ConstantDataReportsPath { get; set; }

        /// <summary>
        /// Логин (логины) клиента (-тов) для отчета.
        /// </summary>
        public List<string> ClientLogins { get; set; }

        /// <summary>
        /// Общая директории для отчетов всех клиентов.
        /// </summary>
        public string BaseFinalReportsDirectoryPath { get; set; }

        /// <summary>
        /// Строковый путь к файлу финального отчета логистики по клиенту (-там).
        /// </summary>
        public string FinalReportFilePath { get; set; }

        public Dictionary<SourceReportItemKey, PrimaryReportFileItem> PrimaryReportFiles { get; set; }
        public bool GetDirectoryOfPrimaryData(string? clientDataPath, PrimaryDataType primaryDataKey, out DirectoryInfo? directoryInfo);
        public bool GetFilesOfPrimaryData(DirectoryInfo directoryInfo, ref FileInfo[]? fileInfos, string[] extensions);
        public bool SearchFilesByDataTypeAndRegex(PrimaryDataType primaryDataType, in FileInfo[] fileInfos);
        public bool CheckDataValidity(PrimaryDataType primaryDataType);
        public bool CheckOfAvailailityData(string? clientDataPath, PrimaryDataType primaryDataKey);
    }

    public partial class ReportFileService : IReportFileService
    {
        #region Fields
        private readonly ProgramOptions _options;
        private readonly ILogger<ReportFileService> _logger;

        /// <summary>
        /// Ключи базовый путей в файловой системе.
        /// </summary>
        private static readonly string[] pathKeyNames = Enum.GetNames(typeof(PathKey));
        #endregion

        #region Properties
        public string ServiceName { get; private set; } = string.Empty;
        public string ConstantDataReportsPath { get; set; } = string.Empty;
        public List<string> ValidFileExtensions { get; set; } = [];
        public List<string> ClientLogins { get; set; } = [];
        public string BaseFinalReportsDirectoryPath { get; set; } = string.Empty;
        public string FinalReportFilePath { get; set; } = string.Empty;
        public string LogisticsReportFirstName { get; private set; } = string.Empty;

        /// <summary>
        /// Словарь с типами первичных отчетов клиента.
        /// </summary>
        public Dictionary<SourceReportItemKey, PrimaryReportFileItem> PrimaryReportFiles { get; set; } = new()
        {
            { SourceReportItemKey.ShortagesPaid, new PrimaryReportFileItem(PrimaryDataType.Constant, [@"Write-off - shortages paid\.xlsx"], 7, isOptionalReport: false)},
            { SourceReportItemKey.WriteOffPointsAndCouriers, new PrimaryReportFileItem(PrimaryDataType.Constant, [@"Write-off - points and couriers\.xlsx"], 7, isOptionalReport: false)},
            { SourceReportItemKey.WriteOffPointsViaWarehouseShortage, new PrimaryReportFileItem(PrimaryDataType.Constant, [@"Write-off - points through the warehouse shortages\.xlsx"], 0, isOptionalReport: false) },
            { SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment, new PrimaryReportFileItem(PrimaryDataType.Constant, [@"Write-off - points through the warehouse adjustment\.xlsx"], 0, isOptionalReport: false)},
            { SourceReportItemKey.Ransoms, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"Отчет\sвыкупов\sпо\sклиенту\s\S+", @"Отчет_выкупов_по_клиентам_\S+"], 0, isOptionalReport: false)},
            { SourceReportItemKey.Balance, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bбаланс\b"], 7, isOptionalReport: false)},
            { SourceReportItemKey.OutgoingProducts, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bрасходы\b"], 7, isOptionalReport: true)},
            { SourceReportItemKey.Transfered, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bперемещения\b"], 7, isOptionalReport: true)},
            { SourceReportItemKey.Additional, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bдоп\b"], 7, isOptionalReport: true)},
        };

        #endregion

        #region Constructors and destructors

        public ReportFileService(ILogger<ReportFileService> logger, IOptions<ProgramOptions> options)
        {
            _logger = logger;
            _options = options.Value;

            ServiceName = GetType().Name;

            if (_options.IsTracing)
            {
                _logger.LogInformation("Создана служба {ServiceName}", ServiceName);
            }

            GetAppConfig();
        }
        #endregion

        #region Methods
        private void GetAppConfig()
        {
            if (!string.IsNullOrEmpty(_options.ConstantDataReportsPath))
            {
                ConstantDataReportsPath = _options.ConstantDataReportsPath;
            }
            else
            {
                string errorMessage = "В конфигурации не задан путь к директории с постоянными данными.";
                _logger.LogCritical("{errorMessage}", errorMessage);
                throw new Exception(errorMessage);
            }

            if (_options.ValidFilesExtentionsList != null && _options.ValidFilesExtentionsList.Count > 0)
            {
                ValidFileExtensions = _options.ValidFilesExtentionsList;
            }
            else
            {
                string errorMessage = "В конфигурации не заданы допустимые расширения для файлов отчетов.";
                _logger.LogCritical("{errorMessage}", errorMessage);
                throw new Exception(errorMessage);
            }

            if (!string.IsNullOrEmpty(_options.BaseFinalReportsDirectoryPath))
            {
                BaseFinalReportsDirectoryPath = _options.BaseFinalReportsDirectoryPath;
            }
            else
            {
                string errorMessage = "В конфигурации не задан путь к общей директории для финальных отчетов всех клиентов.";
                _logger.LogCritical("{errorMessage}", errorMessage);
                throw new Exception(errorMessage);
            }


            if (!string.IsNullOrEmpty(_options.LogisticsReportFirstName))
            {
                LogisticsReportFirstName = _options.LogisticsReportFirstName;
            }
            else
            {
                string errorMessage = "В конфигурации не задан префикс для файлов отчетов логистики.";
                _logger.LogCritical("{errorMessage}", errorMessage);
                throw new Exception(errorMessage);
            }
        }

        /// <summary>
        /// Проверяет наличие и корректность необходимых данных для создания итогового отчета.
        /// </summary>
        /// <param name="clientDataPath">Путь к директории  с первичными данными клиента (опционально).</param>
        /// <param name="primaryDataType">Тип первичных данных (постоянные или переменные).</param>
        /// <returns>Флаг проверки предоставленных данных.</returns>
        public bool CheckOfAvailailityData(string? clientDataPath, PrimaryDataType primaryDataType)
        {
            // Путь к директории с данными.
            var dataPath = primaryDataType == PrimaryDataType.Variable ?
                clientDataPath :
                ConstantDataReportsPath;

            _logger.LogInformation("Проверяю данные по пути: {dataPath}...", dataPath);

            FileInfo[]? fileInfos = null;

            // Получаем сущность директории с первичными данными по их типу. (directoryInfo).
            if (!GetDirectoryOfPrimaryData(dataPath, primaryDataType, out DirectoryInfo? directoryInfo) && directoryInfo == null)
            {
                var error = $"Директория '{dataPath}' не существует.";
                _logger.LogError("{error}", error);
                return false;
            }

            // Из ранее определенной директории получаем массив файлов по расширениям. 
            if (!GetFilesOfPrimaryData(directoryInfo!, ref fileInfos, [.. ValidFileExtensions]) && fileInfos is null && fileInfos?.Length == 0)
            {
                var error = $"Директория '{dataPath}' пустая, либо в ней отсутствуют файлы с указанными расширениями.";
                _logger.LogError("{error}", error);
                return false;
            }

            // Проверяем полноту предоставленных данных (имена файлов соответствуют регуркам) и 
            // актуальность данных по предельному сроку (если он установлен для конкретного отчета).чы
            if (!SearchFilesByDataTypeAndRegex(primaryDataType, in fileInfos!) ||
                !CheckDataValidity(primaryDataType))
                return false;

            // Из имени файла отчета по выкупам получаем логин клиентова.
            if (primaryDataType == PrimaryDataType.Variable &&
                PrimaryReportFiles[SourceReportItemKey.Ransoms].Info != null)
                _ = GetLoginsAndFinalReportPath(PrimaryReportFiles[SourceReportItemKey.Ransoms].Info!);

            return !PrimaryReportFiles.Any(el => !el.Value.IsOptionalReport && el.Value.Info == null) &&
                ClientLogins.Count > 0 &&
                !string.IsNullOrEmpty(FinalReportFilePath);
        }

        /// <summary>
        /// Получает директорию с первичными данными.
        /// </summary>
        /// <param name="clientDataPath">Строковая переменная, содержащая путь к первичным данным.(Необязательно)</param>
        /// <param name="primaryDataKey">Тип первичных данных (постоянные или переменные).</param>
        /// <param name="directoryInfo">Сущность найденной директории.</param>
        public bool GetDirectoryOfPrimaryData(string? clientDataPath, PrimaryDataType primaryDataKey, out DirectoryInfo? directoryInfo)
        {
            directoryInfo = null;

            // Если это переменные (клиентские данные), проверка строковой переменной на наличие пути к источнику данных.
            if (primaryDataKey == PrimaryDataType.Variable && string.IsNullOrEmpty(clientDataPath))
            {
                var errorMessage = $"Путь к источнику переменных данных не задан или каталог не существует:\n'{clientDataPath}'";
                _logger.LogError("{errorMessage}", errorMessage);

                return false;
            }

            // Проверка существования диретории по переданному типу данных.
            GetDirectoryInfo(primaryDataKey == PrimaryDataType.Constant ? ConstantDataReportsPath : clientDataPath!, out directoryInfo);
            if (directoryInfo == null)
            {
                var path = primaryDataKey == PrimaryDataType.Variable ? clientDataPath : ConstantDataReportsPath;
                var errorMessage = $"Путь к источнику данных для типа '{primaryDataKey}' не задан, либо директория не существует: '{path}'";
                _logger.LogError("{errorMessage}", errorMessage);
                return false;
            }

            return directoryInfo != null;
        }

        /// <summary>
        /// Получает файлы первичных данных по указанным расширениям.
        /// </summary>
        /// <param name="directoryInfo">Объект DirectoryInfo для поиска файлов.</param>
        /// <param name="fileInfos">Массив объектов FileInfo или null.</param>
        /// <param name="extensions">Допустимые расширения файлов.</param>
        /// <returns>Флаг наличия файлов.</returns>
        public bool GetFilesOfPrimaryData(DirectoryInfo directoryInfo, ref FileInfo[]? fileInfos, string[] extensions)
        {
            if (!SearchFilesInDirectory(directoryInfo, out fileInfos, extensions) || fileInfos == null || fileInfos.Length == 0)
            {
                var errorMessage = $"В директории '{directoryInfo.FullName}' нет файлов с расширениями: «{string.Join(", ", extensions)}».";
                _logger.LogError("{errorMessage}", errorMessage);
                return false;
            }

            return fileInfos is not null || fileInfos!.Length > 0;
        }

        /// <summary>
        /// Проверяет существование указанной директории.
        /// </summary>
        /// <param name="dirPath">Строковый путь к директории.</param>
        /// <param name="directoryInfo">Объект типа DirectoryInfo (опционально).</param>
        /// <param name="isCreateNewDirectory"Создавать новую диреторию в случае ее отсутсвия.</param>
        /// <returns>Флаг существования директории.</returns>
        /// <remarks>revision 012026</remarks>
        public static bool GetDirectoryInfo(string dirPath, out DirectoryInfo? directoryInfo, bool isCreateNewDirectory = false)
        {
            string path = dirPath;
            if (dirPath.Contains('\''))
            {
                path = dirPath.Replace("'", "");
            }

            directoryInfo = Directory.Exists(path) ?
                new DirectoryInfo(path) :
                (!isCreateNewDirectory) ?
                    null :
                    Directory.CreateDirectory(path);

            return directoryInfo != null;
        }

        /// <summary>
        /// Ищет файлы в указанной директории по определенным расширениям (опционально).
        /// </summary>
        /// <param name="dirInfo">Объект директории.</param>
        /// <param name="fileInfos">Массив объектов FileInfo (опционально).</param>
        /// <param name="extensions">Массив допустимых разрешений файлов.</param>
        /// <returns>Флаг количества найденных файлов (больше 0).</returns>
        /// <remarks>revision 012026</remarks>
        public bool SearchFilesInDirectory(DirectoryInfo dirInfo, out FileInfo[]? fileInfos, string[]? extensions = null)
        {
            fileInfos = null;

            fileInfos = dirInfo.GetFiles();
            if (fileInfos == null || fileInfos.Length == 0)
            {
                var errorMessage = "По пути '" + dirInfo + "' нет файлов";
                _logger.LogError("{errorMessage}", errorMessage);
            }

            if (extensions != null && extensions.Length != 0)
            {
                IEnumerable<FileInfo> collection = fileInfos!.Where(fileInfo => extensions.Contains(fileInfo.Extension));
                List<FileInfo> list = [.. collection];
                fileInfos = [.. list];
            }

            return fileInfos != null && fileInfos.Length != 0;
        }

        /// <summary>
        /// Ищет файлы в предоставленной директории по совпадению имени файла регулярному выражению и записывает их в словарь. 
        /// </summary>
        /// <param name="primaryDataType">Тип данных (постоянные или переменнные).</param>
        /// <param name="fileInfos">Массив объектов FileInfo.</param>
        /// <returns>Булев флаг - у всех объетов словаря PrimaryReportFiles с указанным типом данных записаны FileInfo.</returns>
        /// <remarks>revision 012026</remarks>
        public bool SearchFilesByDataTypeAndRegex(PrimaryDataType primaryDataType, in FileInfo[] fileInfos)
        {
            // Получение объектов из словаря по типу данных (постоянные или переменные).
            var objs = PrimaryReportFiles
                .Where(el => el.Value.DataType == primaryDataType)
                .ToList();

            // Сбрасываем предыдущие результаты: иначе Info прошлого клиента
            // будет ошибочно принят за найденный файл текущего.
            foreach (var ob in objs)
            {
                ob.Value.Reset();
            }

            // Цикл поиска совпадения имени файла по регулярному выражению.
            foreach (var ob in objs)
            {
                FileInfo? matchFile = fileInfos
                    .FirstOrDefault(fi => ob.Value.RegexesForFileName
                        .Any(rx => rx.IsMatch(fi.Name)));

                if (matchFile != null)
                {
                    PrimaryReportFiles[ob.Key].Set(matchFile);
                    continue;
                }

                var errorMessage = $"Не предоставлен файл по ключу {ob.Key} - нет совпадений имени файла по регулярному выражению";
                _logger.LogError("{errorMessage}", errorMessage);
                break;
            }

            return PrimaryReportFiles
                .Count(el => el.Value.DataType == primaryDataType && el.Value.Info != null) == objs.Count;
        }

        /// <summary>
        /// Проверяет актуальность данных (дата последнего изменения файла не должна выходить за срок актуальности в днях если он установлен).
        /// </summary>
        /// <param name="primaryDataType">Тип первичных данных для отчетов.</param>
        /// <returns>Флаг проверки.</returns>
        /// <remarks>revision 012026</remarks>
        public bool CheckDataValidity(PrimaryDataType primaryDataType) =>
            !PrimaryReportFiles
                .Any(el => el.Value.DataType == primaryDataType && !el.Value.IsDataHasNotExpired());

        /// <summary>
        /// Получает список клиентских аккаунтов на основе имени входящего отчёта и определяет путь до конечной папки итогового.
        /// </summary>
        /// <param name="fileInfo">Объект FileInfo.</param>
        /// <returns></returns>
        private bool GetLoginsAndFinalReportPath(FileInfo fileInfo)
        {
            ClientLogins = GetLoginsFromFilename(fileInfo.Name);
            var message = $"Логины клиентов для отчета: {string.Join(", ", ClientLogins)}";
            _logger.LogInformation("{message}", message);

            return DefineDirInfoForClientFinalReport(BaseFinalReportsDirectoryPath);
        }

        /// <summary>
        /// Получает логин (логины) клиента из имени файла.
        /// </summary>
        /// <param name="filename">Строковое имя файла.</param>
        /// <returns>Спискок логинов клиентов.</returns>
        public static List<string> GetLoginsFromFilename(string filename)
        {
            static void RemoveChars(bool start, bool end, char[] chars, ref string str)
            {
                if (start)
                {
                    foreach (var c in chars)
                    {
                        if (str.StartsWith(c))
                            str = str.TrimStart(c);
                    }
                }

                if (end)
                {
                    foreach (var c in chars)
                    {
                        if (str.EndsWith(c))
                            str = str.TrimEnd(c);
                    }
                }
            }

            List<string>? filenameStrings;

            var isSummary = filename.Contains('_');
            filenameStrings = isSummary ? [.. filename.Split('_', '.', ',')] : [.. filename.Split(' ', '.')];

            filenameStrings.RemoveRange(0, 4);
            filenameStrings.RemoveRange(filenameStrings.Count - 1, 1); // remove xlsx

            // Single login
            if (filenameStrings.Count == 1)
            {
                var element = filenameStrings[0];

                if (element.StartsWith('+'))
                    RemoveChars(true, false, ['+'], ref element);

                filenameStrings[0] = element;

            }
            // Many logins
            else
            {
                for (int i = 0; i < filenameStrings.Count; i++)
                {
                    var element = filenameStrings[i];

                    RemoveChars(start: true, end: false, ['+'], ref element);
                    RemoveChars(start: false, end: true, [','], ref element);

                    filenameStrings[i] = element.Trim();
                }
            }

            return filenameStrings;
        }

        /// <summary>
        /// Определяет директорию для файла конечного отчета.
        /// </summary>
        /// <param name="basePath"></param>
        /// <returns>Task</returns>
        public bool DefineDirInfoForClientFinalReport(string basePath)
        {
            if (ClientLogins is null || ClientLogins.Count == 0)
            {
                var message = "Для клинта (-тов) не задан логин (-ны), поэтому создать каталог невозможно.";
                _logger.LogError("{message}", message);
                return false;
            }

            // Общая директоория со всему отчетами логистики.
            var mpBoostClientsPath = Path.Combine(basePath, pathKeyNames[0], pathKeyNames[1]);
            // например, /Users/dmitrijsamarcev/Documents/MPBoostClients

            // Формирование пути к дириктории с клиентом.
            System.Text.StringBuilder stringBuilder = new();
            for (int i = 0; i < ClientLogins.Count; i++)
            {
                stringBuilder.Append(ClientLogins[i]);
                if (ClientLogins[i] != ClientLogins.Last())
                {
                    stringBuilder.Append(", ");
                }
            }

            var finalReportDirectoryPath = Path.Combine(mpBoostClientsPath, stringBuilder.ToString());
            // например, /Users/dmitrijsamarcev/Documents/MPBoostClients/79136726993

            DirectoryInfo? finalReportDirectory;
            if (GetDirectoryInfo(finalReportDirectoryPath, out DirectoryInfo? dirInfoForClientTargetReports, true))
            {
                finalReportDirectory = dirInfoForClientTargetReports;
            }
            else
            {
                var message = "Ошибка при создании диретокрии для итоговых отчетов по клиенту (-там)";
                _logger.LogError("{message}", message);
                return false;
            }

            var reportDateTime = DateTime.Now.ToString("dd-MM-yyyy");

            // Формирование финального пути файла отчета логистики для клиента (-тов).
            FinalReportFilePath = ClientLogins.Count == 1 ?
                $"{finalReportDirectory!.FullName}/{LogisticsReportFirstName} {ClientLogins[0]} {reportDateTime}.xlsx" :
                $"{finalReportDirectory!.FullName}/{LogisticsReportFirstName} {stringBuilder.ToString()} {reportDateTime}.xlsx";
            // например, /Users/dmitrijsamarcev/Documents/MPBoostClients/79136726993/LogisticsReport 79136726993 30-10-2025.xlsx
            return !string.IsNullOrEmpty(FinalReportFilePath);
        }

        #endregion
    }
}
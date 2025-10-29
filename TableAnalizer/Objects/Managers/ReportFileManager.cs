using System.Text;
using System.Text.RegularExpressions;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.ReportItems;

namespace TableAnalizer.Objects.Managers
{
    public class ReportFileManager(IConfiguration config) : BaseManager(config)
    {
        #region Fields 

        private int DataAgeLimitInDays { get; set; }

        private string? logisticsReportFirstName;

        /// <summary>
        /// Dictionary of primary report files.
        /// </summary>
        public static Dictionary<SourceReportItemKey, PrimaryReportFileItem> PrimaryReportFiles { get; private set; } = new()
        {
            { SourceReportItemKey.ShortagesPaid, new PrimaryReportFileItem(PrimaryDataType.Permanent, [@"Write-off\s\-\sshortages\spaid"], 7, isOptionalReport: false)},
            { SourceReportItemKey.WriteOffPointsAndCouriers, new PrimaryReportFileItem(PrimaryDataType.Permanent, [@"Write-off\s\-\spoints\sand\scouriers"], 7, isOptionalReport: false)},
            { SourceReportItemKey.WriteOffPointsViaWarehouseShortage, new PrimaryReportFileItem(PrimaryDataType.Permanent, [@"Write-off\s\-\spoints\sthrough\sthe\swarehouse\sshortages"], 0, isOptionalReport: false) },
            { SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment, new PrimaryReportFileItem(PrimaryDataType.Permanent, [@"Write-off\s\-\spoints\sthrough\sthe\swarehouse\sadjustment"], 0, isOptionalReport: false)},
            { SourceReportItemKey.Ransoms, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"Отчет\sвыкупов\sпо\sклиенту\s\S+", @"Отчет_выкупов_по_клиентам_\S+"], 7, isOptionalReport: false)},
            { SourceReportItemKey.Balance, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bбаланс\b"], 7, isOptionalReport: false)},
            { SourceReportItemKey.OutgoingProducts, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bрасходы\b"], 7, isOptionalReport: true)},
            { SourceReportItemKey.Transfered, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bперемещения\b"], 7, isOptionalReport: true)},
            { SourceReportItemKey.Additional, new PrimaryReportFileItem(PrimaryDataType.Variable, [@"\d+\s+\bдополнительный\b"], 7, isOptionalReport: true)},
        };

        #endregion

        #region Properties
        private static List<string> ClienstLogins { get; set; } = [];
        private string PermanentDataSourcePath { get; set; } = string.Empty;
        public static TargetReportRecipientType? ReportRecipientType { get; set; }

        /// <summary>
        /// Коллекция с файлами исходных отчетов.
        /// </summary>
        public Dictionary<SourceReportItemKey, SourceReportFileItem> SourceReportFileItems { get; private set; } = [];
        public static DirectoryInfo? DirInfoForClientTargetReports { get; private set; }
        public static string OutputClientReportFilePath { get; private set; } = string.Empty;


        #endregion

        #region Functionality

        protected override void AddConfigVariables()
        {
            base.AddConfigVariables();

            IConfigurationSection? stringsSessions = config!.GetSection("Strings");
            PermanentDataSourcePath = stringsSessions.GetValue<string>("PermanentDataSourcePath");

            IConfigurationSection? numbersSessions = config!.GetSection("Numbers");
            DataAgeLimitInDays = numbersSessions.GetValue<int>("DataAgeLimitInDays");

            IConfigurationSection? stringSessions = config!.GetSection("Strings");
            logisticsReportFirstName = stringSessions.GetValue<string>("LogisticsReportFirstName");
        }

        /// <summary>
        /// Get directory of primary data.
        /// </summary>
        /// <param name="clientDataPath">String variable of a path for client data. (Optional)</param>
        /// <param name="primaryDataKey">Key of primary data.</param>
        /// <param name="directoryInfo">Item of a searching directory.</param>
        private bool GetDirectoryOfPrimaryData(string? clientDataPath, PrimaryDataType primaryDataKey, ref DirectoryInfo? directoryInfo)
        {
            // Checking of string variable for a path to client data source.
            if (primaryDataKey == PrimaryDataType.Variable && string.IsNullOrEmpty(clientDataPath))
            {
                OnManagerReached(new($"Path to variable data source is not set or directory does not exist: '{clientDataPath}'", true, true));
            }

            // Checking of a directory existence  
            FileService.GetDirectoryInfo(primaryDataKey == PrimaryDataType.Permanent ? PermanentDataSourcePath : clientDataPath!, out directoryInfo);
            if (directoryInfo == null)
            {
                var path = primaryDataKey == PrimaryDataType.Variable ? clientDataPath : PermanentDataSourcePath;
                OnManagerReached(new($"Path to data source for key '{primaryDataKey}' is not set or directory does not exist: '{path}'", true, true));
            }

            return directoryInfo != null;
        }

        /// <summary>
        /// Get files of primary data.
        /// </summary>
        /// <param name="directoryInfo">DirectoryInfo object for a searching files.</param>
        /// <param name="fileInfos">An array of FileInfos objects or null.</param>
        /// <param name="extensions">An Extensions of files.</param>
        /// <returns>Flag of an existence of files.</returns>
        private bool GetFilesOfPrimaryData(DirectoryInfo directoryInfo, ref FileInfo[]? fileInfos, string[] extensions)
        {
            if (!FileService.SearchFilesInDirectory(directoryInfo.FullName, out fileInfos, extensions) || fileInfos == null || fileInfos.Length == 0)
            {
                OnManagerReached(new($"Directory '{directoryInfo.FullName}' contains no files with extensions: '{string.Join(", ", extensions)}'.", true, true));
                return false;
            }

            return fileInfos is not null || fileInfos!.Length > 0;
        }

        /// <summary>
        /// Checks of availaility of primary data.
        /// </summary>
        /// <param name="clientDataPath">Client data path (optional, only for cliet's data).</param>
        /// <param name="primaryDataKey">Key of a data.</param>
        /// <returns>Execution flag of operation and message.</returns>
        /// <remarks>oct 2025 ver.<>/remarks>
        public async Task<(bool isError, string message)> CheckOfAvailailityData(string? clientDataPath, PrimaryDataType primaryDataKey)
        {
            bool isError = false;
            StringBuilder stringBuilder = new();

            var dataPath = primaryDataKey == PrimaryDataType.Variable ? clientDataPath : PermanentDataSourcePath;

            int permanentDataTypeCount = PrimaryReportFiles
                .Count(p => p.Value.DataType is PrimaryDataType.Permanent && !p.Value.IsOptionalReport);

            int variableDataTypeCount = PrimaryReportFiles
                .Count(p => p.Value.DataType is PrimaryDataType.Variable && !p.Value.IsOptionalReport);

            DirectoryInfo? directoryInfo = null;
            FileInfo[]? fileInfos = null;

            if (GetDirectoryOfPrimaryData(clientDataPath, primaryDataKey, ref directoryInfo) && directoryInfo != null)
            {
                if (GetFilesOfPrimaryData(directoryInfo, ref fileInfos, ValidFileExtensions) && fileInfos is not null && fileInfos.Length > 0)
                {
                    // Checking the number of files (only for permanent data)
                    if (primaryDataKey == PrimaryDataType.Permanent && fileInfos!.Length < PrimaryReportFiles.Count(p => p.Value.DataType is PrimaryDataType.Permanent))
                    {
                        isError = true;
                        stringBuilder.Append($"Directory '{PermanentDataSourcePath}' contains less files than required ({permanentDataTypeCount}).\n");
                    }

                    var checkingReportsTask = CheckingReportsByPrimaryDataType(primaryDataKey, fileInfos!, PrimaryReportFiles);
                    await Task.Run(() => checkingReportsTask);
                    checkingReportsTask.Wait();

                    isError = checkingReportsTask.Result.isError;
                    stringBuilder.Append(checkingReportsTask.Result.message);
                }
                else
                {
                    isError = true;
                    stringBuilder.Append($"Directory '{dataPath}' is empty or there are no files equal to the specified extensions.\n");
                }
            }
            else
            {
                isError = true;
                stringBuilder.Append($"Directory '{dataPath}' does not exist.\n");
            }

            return (isError, stringBuilder.ToString());
        }

        /// <summary>
        /// Checks files by primary data type.
        /// </summary>
        /// <param name="primaryDataType">Primary data type key.</param>
        /// <param name="fileInfos">Searching files.</param>
        /// <param name="dataDict">Source dictionary.</param>
        /// <returns>Execution flag of operation and message.</returns>
        private static async Task<(bool isError, string message)> CheckingReportsByPrimaryDataType(PrimaryDataType primaryDataType, FileInfo[] fileInfos, Dictionary<SourceReportItemKey, PrimaryReportFileItem> dataDict)
        {
            bool isError = false;
            StringBuilder stringBuilder = new();

            foreach (var fileInfo in fileInfos)
            {
                var primaryReportFileItem = dataDict
                    .FirstOrDefault(prfi => prfi.Value.RegexesForFileName
                        .FirstOrDefault(r => r.IsMatch(fileInfo.Name)) != null);

                if (primaryReportFileItem.Value != null)
                {
                    // Checking a file's expiration date  
                    if (primaryReportFileItem.Value.BestBeforeDate > 0 && fileInfo.LastWriteTime.AddDays(primaryReportFileItem.Value.BestBeforeDate) < DateTime.Now)
                    {
                        isError = true;
                        stringBuilder.Append($"Data in file by key '{primaryReportFileItem.Key}' has expired. Please update file.\n");
                    }

                    // Get client's logins
                    if (primaryReportFileItem.Key == SourceReportItemKey.Ransoms)
                    {
                        ClienstLogins = GetLoginsFromFilename(fileInfo.Name);
                    }

                    primaryReportFileItem.Value.Set(fileInfo);
                }
            }

            dataDict.Where(d => d.Value.DataType == primaryDataType).ToList().ForEach(d =>
            {
                if (d.Value.Info == null && !d.Value.IsOptionalReport)
                {
                    isError = true;
                    stringBuilder.Append($"File with data for key '{d.Key}' is missing from directory.\n");
                }
            });

            return (isError, stringBuilder.ToString());
        }
        private void MakeSourceReportFileItem(FileInfo fileInfo, SourceReportItemKey? key, string? loginsFileName, bool isControlAgeOfData)
        {
            var sorceReportFile = new SourceReportFileItem(key.Value, fileInfo, isControlAgeOfData, DataAgeLimitInDays);

            if (key.Value == SourceReportItemKey.Ransoms && !string.IsNullOrEmpty(loginsFileName))
            {
                ClientLogins = GetLoginsFromFilename(loginsFileName);
            }

            // Store SourceReportFiles
            SourceReportFileItems[key.Value] = sorceReportFile;
        }

        /// <summary>
        /// Get source report file for one client.
        /// </summary>
        /// <param name="key">Key to search for report file in dict.</param>
        /// <returns>SourceReportFileForSKU as Task (optional).</returns>
        public async Task<SourceReportFileItem?> GetSourceReport(SourceReportItemKey key) =>
            await Task.Run(() => SourceReportFileItems.TryGetValue(key, out var reportFile) ? reportFile : null);

        /// <summary>
        /// Define directory for client report output.
        /// </summary>
        /// <returns>Path string to directory to save data.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<string?> DefineDirInfoForClientTargetReports(string basePath)
        {
            ManagerReachedEventArgs args;

            if (ClientLogins is null || ClientLogins.Count == 0)
            {
                args = new("Clint's login is not defined, so it is impossible to create a directory.", true, true);
                OnManagerReached(args);
                return null;
            }

            var mpBoostClientsPath = Path.Combine(basePath, pathKeyNames[0], pathKeyNames[1]);

            StringBuilder stringBuilder = new();
            for (int i = 0; i < ClientLogins.Count; i++)
            {
                stringBuilder.Append(ClientLogins[i]);
                if (ClientLogins.Last() != ClientLogins[i])
                {
                    stringBuilder.Append(", ");
                }
            }

            var dirClientsTargetReportsPath = Path.Combine(mpBoostClientsPath, stringBuilder.ToString());
            if (FileService.GetDirectoryInfo(dirClientsTargetReportsPath, out DirectoryInfo? dirInfoForClientTargetReports, true))
            {
                DirInfoForClientTargetReports = dirInfoForClientTargetReports;
            }
            else
            {
                await Task.Delay(100);
                args = new("Error creating directory for final reports", true, true);
                OnManagerReached(args);
                return null;
            }

            var reportDateTime = DateTime.Now.ToString("dd-MM-yyyy");

            OutputClientReportFilePath = ClientLogins.Count == 1 ?
                $"{DirInfoForClientTargetReports!.FullName}/{logisticsReportFirstName} {ClientLogins[0]} {reportDateTime}.xlsx" :
                $"{DirInfoForClientTargetReports!.FullName}/{logisticsReportFirstName} {stringBuilder.ToString()} {reportDateTime}.xlsx";

            args = new($"Directory for target reports:\n{DirInfoForClientTargetReports.FullName}\nClients target report file name:\n{OutputClientReportFilePath}", false, false);
            OnManagerReached(args);

            return DirInfoForClientTargetReports.FullName ?? null;
        }

        /// ???
        static readonly string[] pathKeyNames = Enum.GetNames(typeof(PathKey));

        // /// <summary>
        // /// Gets the report file key using regex and the name of the file with client logins.
        // /// </summary>
        // /// <param name="fileInfo">Object to parse the file name for.</param>
        // /// <returns>Report file key or null, the name of the file with client logins or null.</returns>
        // /// <remarks><i>Rework, testing</i></remarks>
        // private static async Task<(SourceReportItemKey? key, string? loginsFileName)> GetSourceReportKeyByRegex(FileInfo fileInfo)
        // {
        //     SourceReportItemKey? key = null;
        //     string? loginsFileName = null;

        //     var fileName = Path.GetFileName(fileInfo.Name);

        //     if (fileName.StartsWith("~$"))
        //         fileName = fileName.TrimStart('~', '$');

        //     foreach (var regexesSourceReport in RegexesForSourceReports)
        //     {
        //         foreach (var regex in regexesSourceReport.Value)
        //         {
        //             var regexMatch = regex.Match(fileName);
        //             if (regexMatch is not null && regexMatch.Success)
        //             {
        //                 key = regexesSourceReport.Key;
        //                 if (key == SourceReportItemKey.Ransoms)
        //                 {
        //                     loginsFileName = fileName;
        //                 }
        //             }
        //         }
        //     }

        //     await Task.Delay(10);

        //     return (key, loginsFileName);
        // }

        /// <summary>
        /// Gets client logins from the report file name.
        /// </summary>
        /// <param name="filename">Report file name.</param>
        /// <returns>Client login sheet.</returns>
        /// <remarks>oct 2025 ver.<>/remarks>
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

        #endregion
    }
}
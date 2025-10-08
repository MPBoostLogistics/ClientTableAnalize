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
        private const string RANSOMS_SINGLE_REPORT_REGEX_PATTERN = @"Отчет\sвыкупов\sпо\sклиенту\s\S+";
        private const string RANSOMS_MULTY_REPORT_REGEX_PATTERN = @"Отчет_выкупов_по_клиентам_\S+";
        private const string BALANCE_REPORT_REGEX_PATTERN = @"\d+\s+\bбаланс\b";
        private const string SHIPPED_REPORT_REGEX_PATTERN = @"\d+\s+\bрасходы\b";
        private const string TRANSFER_REPORT_REGEX_PATTERN = @"\d+\s+\bперемещения\b";
        private const string ADDITIONAL_REPORT_REGEX_PATTERN = @"\d+\s+\bдополнительный\b";
        private const string PAID_SHORTAGES_REGEX_PATTERN = @"\bОплаченные\sнедостачи\b";
        private const string WRITEOFF_POINTS_VIA_WAREHOUSE_SHORTAGE_REPORT_REGEX_PATTERN = @"\bСписание\sточек\sчерез\sсклад\sнедостачи\b";
        private const string WRITEOFF_POINTS_VIA_WAREHOUSE_ADJUSTMENT_REPORT_REGEX_PATTERN = @"\bСписание\sточек\sчерез\sсклад\sкорректировки\b";
        private const string WRITEOFF_POINTS_REPORT_REGEX_PATTERN = @"\bСписание\sточек\b";
        private const string WRITEOFF_DIRECTORY_NAME = "Списание";

        private int DataAgeLimitInDays { get; set; }

        private string? logisticsReportFirstName;

        private static Dictionary<PermanentDataKey, (Regex regex, int bestBeforeDate)> constantDataSourcePaths = new()
        {
            { PermanentDataKey.PointsAndCouriers, (new(@"Write-off\s\-\spoints\sand\scouriers", RegexOptions.None, TimeSpan.FromSeconds(1)), 7)},
            //{ WriteOffSource.ShortagesPaid, ("Write-off - shortages paid.xlsx", 7)}
            // { WriteOffSource.PointsViaWarehouseShortage, ("Write-off - points through the warehouse (shortages).xlsx", 0) },
            // { WriteOffSource.PointsViaWarehouseAdjustment, ("Write-off - points through the warehouse (adjustments).xlsx", 0) },
            // { WriteOffSource.PointsAndCouriers, ("Write-off - points and couriers.xlsx", 7)},
            // { WriteOffSource.Warehouse, "Write-off - warehouse.xlsx" }
        };

        #endregion

        #region Properties
        private string PermanentDataSourcePath { get; set; } = string.Empty;

        public static TargetReportRecipientType? ReportRecipientType { get; set; }

        /// <summary>
        /// Коллекция с файлами исходных отчетов.
        /// </summary>
        public Dictionary<SourceReportItemKey, SourceReportFileItem> SourceReportFileItems { get; private set; } = [];

        public static DirectoryInfo? DirInfoForClientTargetReports { get; private set; }
        public static string? InputReportsFilePath { get; set; } = string.Empty;
        public static string OutputClientReportFilePath { get; private set; } = string.Empty;

        public static Dictionary<SourceReportItemKey, List<Regex>> RegexesForSourceReports { get; } = new Dictionary<SourceReportItemKey, List<Regex>>
        {
            {
                SourceReportItemKey.Ransoms,
                [
                    new(RANSOMS_SINGLE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                    new(RANSOMS_MULTY_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1))
                ]
            },
            {
                SourceReportItemKey.Balance,
                [
                    new(BALANCE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceReportItemKey.WriteOffPoints,
                [
                    new(WRITEOFF_POINTS_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1))
                ]
            },
            {
                SourceReportItemKey.WriteOffPointsViaWarehouseShortage,
                [
                    new(WRITEOFF_POINTS_VIA_WAREHOUSE_SHORTAGE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1))
                ]
            },
              {
                SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment,
                [
                    new(WRITEOFF_POINTS_VIA_WAREHOUSE_ADJUSTMENT_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1))
                ]
            },
            {
                SourceReportItemKey.OutgoingProducts,
                [
                    new(SHIPPED_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceReportItemKey.Transfered,
                [
                    new(TRANSFER_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceReportItemKey.Additional,
                [
                    new(ADDITIONAL_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceReportItemKey.PaidShortages,
                [
                    new(PAID_SHORTAGES_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            }

        };

        #endregion

        #region  Functionality

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
        /// Checks availability and expiration date of permanent information.
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<PermanentDataKey, FileInfo>?> CheckOfAvailailityPermanentData()
        {
            // Checking of a directory existence  
            FileService.GetDirectoryInfo(PermanentDataSourcePath, out DirectoryInfo? directoryInfo);
            if (directoryInfo == null)
            {
                OnManagerReached(new($"Path to constant data source is not set or directory does not exist: '{PermanentDataSourcePath}'", true, true));
                return null;
            }

            // Checking of a files existence in directory
            if (!FileService.SearchFilesInDirectory(PermanentDataSourcePath, out FileInfo[]? fileInfos, [".xlsx"]) || fileInfos == null || fileInfos.Length == 0)
            {
                OnManagerReached(new($"Directory '{PermanentDataSourcePath}' is empty or there are no files equal to the specified extensions.", true, true));
                return null;
            }

            // Iterate over a dictionary, check for regex and expiration date
            Dictionary<PermanentDataKey, FileInfo> resultsDict = [];
            foreach (var (key, value) in constantDataSourcePaths)
            {
                // Searching for a file using a regex
                var fileInfo = fileInfos.SingleOrDefault(fi => value.regex.IsMatch(fi.Name));
                if (fileInfo == null)
                {
                    OnManagerReached(new($"File with data for key '{key}' is missing from directory.", true, true));
                    return null;
                }

                // Checking a file's expiration date  
                if (fileInfo!.LastWriteTime.AddDays(value.bestBeforeDate) < DateTime.Now)
                {
                    OnManagerReached(new($"Data in file by key '{key}' has expired. Please update file.", true, true));
                    return null;
                }

                resultsDict[key] = fileInfo;
            }

            // Dummy
            await Task.Delay(10);
            return resultsDict;


            // Write-off - points through the warehouse (adjustments).xlsx - списание точек через склад, не обновляется
            // Write-off - shortages paid.xlsx - оплаченные недостачи, обновляется 1 раз в 7 дней

            // Write-off - warehouse.xlsx - списание со склада, обновляется 1 раз в 7 дней

            // Проверить что файлы обновлены (какие необходимо)
        }


        private static DirectoryInfo? GetDirectory(string path, ref bool isError)
        {
            isError = false;

            if (!FileService.GetDirectoryInfo(path, out DirectoryInfo? dirInfo) || dirInfo == null)
            {
                isError = true;
                return null;
            }

            return dirInfo;
        }


        /// <summary>
        /// Checks availability of necessary files for data analysis.
        /// </summary>
        /// <param name="reportsPath">Directory with client reports.</param>
        /// <returns>List of existing keys as Task result.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
        public async Task<List<SourceReportItemKey>?> ChecksAvailabilityNecessaryFiles(string reportsPath)
        {
            List<SourceReportItemKey> existingKeys = [];
            bool isError = false;

            try
            {
                GetFileInfosSourceReports(reportsPath, out FileInfo[] files);

                Task<int>? getFilesTask;

                try
                {
                    getFilesTask = GetSourceReportFiles(files);
                    await Task.Run(() => getFilesTask);
                    getFilesTask?.Wait();
                }
                catch (System.Exception ex)
                {
                    OnManagerReached(new(ex.Message, true, true));
                    return await Task.FromResult<List<SourceReportItemKey>?>(null);
                }

                var getReportFilesResult = getFilesTask?.Result;

                if (getFilesTask?.Result == 0)
                {
                    OnManagerReached(new(string.Empty, true, true));
                }

                StringBuilder stringBuilder = new();

                if (IsRequiredFilesProvided(out List<SourceReportItemKey>? lostKeys, out existingKeys) && existingKeys.Count > 0)
                {
                    stringBuilder.Append($"Provided {existingKeys.Count} source reports on keys: ");
                    foreach (var existingKey in existingKeys)
                    {
                        stringBuilder.Append($"{existingKey}");
                        if (existingKeys.Last() != existingKey)
                        {
                            stringBuilder.Append($", ");
                        }
                    }

                    isError = false;
                }
                else if (lostKeys.Count > 0)
                {
                    stringBuilder.Append($"No provided {lostKeys.Count} required source reports on keys: ");
                    foreach (var lostKey in lostKeys)
                    {
                        stringBuilder.Append($"{lostKey}");
                        if (existingKeys.Last() != lostKey)
                        {
                            stringBuilder.Append($", ");
                        }
                    }

                    isError = true;
                }

                OnManagerReached(new(stringBuilder.ToString(), isError, isError));
                if (isError)
                {
                    return await Task.FromResult<List<SourceReportItemKey>?>(null);
                }
            }
            catch (Exception ex)
            {
                OnManagerReached(new($"Data error: {ex.Message}\n", true, true));
                return await Task.FromResult<List<SourceReportItemKey>?>(null);
            }

            return existingKeys;
        }

        private void GetFileInfosSourceReports(string reportsPath, out FileInfo[] files)
        {
            // Check writeOff directory path 
            var writeOffDirectoryPath = Path.Combine(reportsPath, WRITEOFF_DIRECTORY_NAME);
            if (!Directory.Exists(writeOffDirectoryPath))
            {
                ManagerReachedEventArgs args = new($"Required directory named \"{WRITEOFF_DIRECTORY_NAME}\" is missing", true, true);
                OnManagerReached(args);
            }

            // Reading files in writeOff directory
            FileService.SearchFilesInDirectory(writeOffDirectoryPath, out FileInfo[]? writeOffFiles, ValidFileExtensions);

            // Reading files in root directory
            FileService.SearchFilesInDirectory(reportsPath, out FileInfo[]? commonFiles, ValidFileExtensions);

            // Combine files 
            if (writeOffFiles == null || writeOffFiles.Length == 0 || commonFiles == null || commonFiles.Length == 0)
            {
                ManagerReachedEventArgs args = new($"Failed to retrieve all source report files", true, true);
                OnManagerReached(args);
            }

            files = new FileInfo[writeOffFiles!.Length + commonFiles!.Length];
            Array.Copy(writeOffFiles, files, writeOffFiles.Length);
            Array.Copy(commonFiles, 0, files, writeOffFiles.Length, commonFiles.Length);
        }

        /// <summary>
        /// Parses file name against a regex, creates a SourceReportFileForSKU and writes it to manager's internal collection.
        /// </summary>
        /// <param name="files">Path to files.</param>
        /// <returns>Number of recognized source files.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
        public async Task<int>? GetSourceReportFiles(FileInfo[]? files)
        {
            StringBuilder message = new();
            var isError = false;

            if (files is null || files.Length == 0)
            {
                var errorMessage = "Directory with files for processing does not exist or is empty, check it.";
                message.Append(errorMessage);
                throw new Exception(errorMessage);
            }
            else
            {
                string extString = string.Empty;
                foreach (var validFileExtension in ValidFileExtensions)
                {
                    extString = string.Concat(extString, validFileExtension, " ");
                }

                message.Append($"\nNumber of files in dir with ext \"{extString}\": {files!.Length}");

                foreach (var fileInfo in files!)
                {
                    try
                    {
                        var getSourceReportKeyByRegexTask = GetSourceReportKeyByRegex(fileInfo);
                        await Task.Run(() => getSourceReportKeyByRegexTask);
                        getSourceReportKeyByRegexTask.Wait();

                        var (key, loginsFileName) = getSourceReportKeyByRegexTask.Result;

                        if (key.HasValue)
                        {
                            var isControlAgeOfData = key switch
                            {
                                SourceReportItemKey.Ransoms or
                                SourceReportItemKey.Balance or
                                SourceReportItemKey.OutgoingProducts or
                                //SourceReportItemKey.WriteOffPoints or
                                SourceReportItemKey.PaidShortages
                                => true,
                                _ => false
                            };

                            try
                            {
                                MakeSourceReportFileItem(fileInfo, key, loginsFileName, isControlAgeOfData);
                            }
                            catch (System.Exception ex)
                            {
                                message.Append(ex.Message);
                                throw new Exception(ex.Message);
                            }
                        }
                        else
                        {
                            var errorMessage = $"\nUnable to determine key for report file {fileInfo.Name}";
                            message.Append(errorMessage);
                            throw new Exception(errorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"\nError SourceReportFileForSKU create: {ex.Message}";
                        message.Append(errorMessage);
                        throw new Exception(errorMessage);
                    }
                }

                if (!isError)
                {
                    try
                    {
                        // Get output directory and report filename
                        var personalPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                        var clientOutputPathTask = DefineDirInfoForClientTargetReports(personalPath);
                        await Task.Run(async () => await clientOutputPathTask);
                        clientOutputPathTask.Wait();
                        var clientOutputPath = clientOutputPathTask.Result;

                        if (!string.IsNullOrEmpty(clientOutputPath))
                        {
                            message.Append($"\nDirectory for target report: {clientOutputPath}");
                        }
                        else
                        {
                            var errorMessage = "\nPath to dir for working with client can`t be an empty string!";
                            message.Append(errorMessage);
                            throw new Exception(errorMessage);

                        }
                    }
                    catch (Exception ex)
                    {
                        message.Append($"\nDefineDirInfoForClientTargetReports Error:\n{ex.Message}");
                        throw;
                    }
                }
            }

            ManagerReachedEventArgs args = new(message.ToString(), isError, false, 2000);
            OnManagerReached(args);

            return SourceReportFileItems.Count;
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

        /// <summary>
        /// Checks whether required files have been added to create a client report.
        /// </summary>
        /// <param name="lostKeys">A collection of required file keys that were not provided.</param>
        /// <param name="existingKeys">A collection of the required file keys that were provided.</param>
        /// <returns>Boolean flag indicating whether the operation has been performed.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
        public bool IsRequiredFilesProvided(out List<SourceReportItemKey> lostKeys, out List<SourceReportItemKey> existingKeys)
        {
            existingKeys = lostKeys = new([]);

            var isContainsKeyRansoms = SourceReportFileItems.ContainsKey(SourceReportItemKey.Ransoms);
            var isContainsKeyBalance = SourceReportFileItems.ContainsKey(SourceReportItemKey.Balance);
            var isContainsKeyOutgoingProducts = SourceReportFileItems.ContainsKey(SourceReportItemKey.OutgoingProducts);
            var isContainsKeyTransfer = SourceReportFileItems.ContainsKey(SourceReportItemKey.Transfered);
            var isContainsWriteOffPoints = SourceReportFileItems.ContainsKey(SourceReportItemKey.WriteOffPoints);
            var isContainsWriteOffPointsViaWarehouseShortage = SourceReportFileItems.ContainsKey(SourceReportItemKey.WriteOffPointsViaWarehouseShortage);
            var isContainsWriteOffPointsViaWarehouseAdjustment = SourceReportFileItems.ContainsKey(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment);
            var isContainsAdditional = SourceReportFileItems.ContainsKey(SourceReportItemKey.Additional);
            var isContainsPaidShortages = SourceReportFileItems.ContainsKey(SourceReportItemKey.PaidShortages);

            bool success = isContainsKeyRansoms && isContainsKeyBalance && isContainsWriteOffPoints && isContainsWriteOffPointsViaWarehouseShortage && isContainsWriteOffPointsViaWarehouseAdjustment && isContainsPaidShortages;

            if (success)
            {
                if (isContainsKeyRansoms)
                    existingKeys.Add(SourceReportItemKey.Ransoms);

                if (isContainsKeyBalance)
                    existingKeys.Add(SourceReportItemKey.Balance);

                if (isContainsKeyOutgoingProducts)
                    existingKeys.Add(SourceReportItemKey.OutgoingProducts);

                if (isContainsKeyTransfer)
                    existingKeys.Add(SourceReportItemKey.Transfered);

                if (isContainsWriteOffPoints)
                    existingKeys.Add(SourceReportItemKey.WriteOffPoints);

                if (isContainsWriteOffPointsViaWarehouseShortage)
                    existingKeys.Add(SourceReportItemKey.WriteOffPointsViaWarehouseShortage);

                if (isContainsWriteOffPointsViaWarehouseAdjustment)
                    existingKeys.Add(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment);

                if (isContainsAdditional)
                    existingKeys.Add(SourceReportItemKey.Additional);

                if (isContainsPaidShortages)
                    existingKeys.Add(SourceReportItemKey.PaidShortages);
            }
            else
            {
                if (!isContainsKeyRansoms)
                    lostKeys.Add(SourceReportItemKey.Ransoms);

                if (!isContainsKeyBalance)
                    lostKeys.Add(SourceReportItemKey.Balance);

                if (!isContainsKeyOutgoingProducts)
                    lostKeys.Add(SourceReportItemKey.OutgoingProducts);

                if (!isContainsWriteOffPoints)
                    lostKeys.Add(SourceReportItemKey.WriteOffPoints);

                if (!isContainsWriteOffPointsViaWarehouseShortage)
                    lostKeys.Add(SourceReportItemKey.WriteOffPointsViaWarehouseShortage);

                if (!isContainsWriteOffPointsViaWarehouseAdjustment)
                    lostKeys.Add(SourceReportItemKey.WriteOffPointsViaWarehouseAdjustment);

                if (!isContainsPaidShortages)
                    lostKeys.Add(SourceReportItemKey.PaidShortages);
            }

            return success;
        }

        /// ???
        static readonly string[] pathKeyNames = Enum.GetNames(typeof(PathKey));

        /// <summary>
        /// Gets the report file key using regex and the name of the file with client logins.
        /// </summary>
        /// <param name="fileInfo">Object to parse the file name for.</param>
        /// <returns>Report file key or null, the name of the file with client logins or null.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
        private static async Task<(SourceReportItemKey? key, string? loginsFileName)> GetSourceReportKeyByRegex(FileInfo fileInfo)
        {
            SourceReportItemKey? key = null;
            string? loginsFileName = null;

            var fileName = Path.GetFileName(fileInfo.Name);

            if (fileName.StartsWith("~$"))
                fileName = fileName.TrimStart('~', '$');

            foreach (var regexesSourceReport in RegexesForSourceReports)
            {
                foreach (var regex in regexesSourceReport.Value)
                {
                    var regexMatch = regex.Match(fileName);
                    if (regexMatch is not null && regexMatch.Success)
                    {
                        key = regexesSourceReport.Key;
                        if (key == SourceReportItemKey.Ransoms)
                        {
                            loginsFileName = fileName;
                        }
                    }
                }
            }

            await Task.Delay(10);

            return (key, loginsFileName);
        }

        /// <summary>
        /// Gets client logins from the report file name.
        /// </summary>
        /// <param name="filename">Report file name.</param>
        /// <returns>Client login sheet.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
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
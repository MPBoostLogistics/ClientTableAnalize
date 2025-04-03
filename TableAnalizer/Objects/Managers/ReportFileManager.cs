using System.Text;
using System.Text.RegularExpressions;
using CSharpHelpers.Services;
using Microsoft.Extensions.Configuration;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class ReportFileManager(IConfiguration config) : BaseManager(config)
    {
        #region Fields 
        private const string RANSOMS_SINGLE_REPORT_REGEX_PATTERN = @"Отчет\sвыкупов\sпо\sклиенту\s\S+";
        private const string RANSOMS_MULTY_REPORT_REGEX_PATTERM = @"Отчет_выкупов_по_клиентам_\S+";
        private const string BALANCE_REPORT_REGEX_PATTERN  = @"\d+\s+\bбаланс\b";
        private const string SHIPPED_REPORT_REGEX_PATTERN  = @"\d+\s+\bрасходы\b";
        private const string TRANSFER_REPORT_REGEX_PATTERN  = @"\d+\s+\bперемещения\b";
        private const string ADDITIONAL_REPORT_REGEX_PATTERN  = @"\d+\s+\bдополнительный\b";
        private const string WRITEOFF_POINTS_VIA_WAREHOUSE_REPORT_REGEX_PATTERN = @"\bСписание\sточек\sчерез\sсклад\b";
        private const string WRITEOFF_POINTS_REPORT_REGEX_PATTERN = @"\bСписание\sточек\sчерез\sсклад\b";
        private const string WRITEOFF_DIRECTORY_NAME = "Списание";

        private string? logisticsReportFirstName;

        #endregion

        #region Properties

        public static TargetReportRecipientType? ReportRecipientType { get; set; }

        public Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> ReportsPaths { get; private set; } = [];
        public Dictionary<string, SourceReportFileForSKU> ReportsPathsMultyReport { get; private set; } = new();
        public static DirectoryInfo? DirInfoForClientTargetReports { get; private set; }
        public static string? InputReportsFilePath { get; set; } = string.Empty;
        public static string OutputClientReportFilePath { get; private set; } = string.Empty;

        public static Dictionary<SourceSKUReportFileKey, List<Regex>> RegexesForSourceReports { get; } = new Dictionary<SourceSKUReportFileKey, List<Regex>>
        {
            { 
                SourceSKUReportFileKey.Ransoms,  
                [ 
                    new(RANSOMS_SINGLE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                    new(RANSOMS_MULTY_REPORT_REGEX_PATTERM, RegexOptions.None, TimeSpan.FromSeconds(1)) 
                ] 
            }, 
            {
                SourceSKUReportFileKey.Balance, 
                [
                    new(BALANCE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceSKUReportFileKey.WriteOffPoints, 
                [
                    new(WRITEOFF_POINTS_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1))
                ]
            },
            {
                SourceSKUReportFileKey.WriteOffPointsViaWarehouse, 
                [
                    new(WRITEOFF_POINTS_VIA_WAREHOUSE_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1))
                ]
            },
            {
                SourceSKUReportFileKey.OutgoingProducts, 
                [
                    new(SHIPPED_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceSKUReportFileKey.Transfered, 
                [
                    new(TRANSFER_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            },
            {
                SourceSKUReportFileKey.Additional, 
                [
                    new(ADDITIONAL_REPORT_REGEX_PATTERN, RegexOptions.None, TimeSpan.FromSeconds(1)),
                ]
            }

        };

        #endregion

        #region  Functionality

        protected override void AddConfigVariables()
        {
            base.AddConfigVariables();

            IConfigurationSection? stringSessions = config!.GetSection("Strings");
            logisticsReportFirstName = stringSessions.GetValue<string>("LogisticsReportFirstName");
        }

        /// <summary>
        /// Checks availability of necessary files for data analysis.
        /// </summary>
        /// <param name="reportsPath">Directory with client reports.</param>
        /// <returns>List of existing keys as Task result.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
        public async Task<List<SourceSKUReportFileKey>?> ChecksAvailabilityNecessaryFiles(string reportsPath)
        {
            
            List<SourceSKUReportFileKey> existingKeys = [];
            bool isError = false;

            try
            {
                GetSourceReportFiles(reportsPath, out FileInfo[] files);
      
                Task<int> getFilesTask = GetSourceReportFiles(files);
                await Task.Run(() => getFilesTask);
                getFilesTask.Wait();

                var getReportFilesResult = getFilesTask.Result;

                if (getFilesTask.Result == 0)
                {
                    OnManagerReached(new(string.Empty, true, true));
                }

                StringBuilder stringBuilder = new();

                if (IsRequiredFilesProvided(out List<SourceSKUReportFileKey>? lostKeys, out existingKeys) && existingKeys.Count > 0)
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
            }
            catch (Exception ex)
            {
                OnManagerReached(new($"Data error: {ex.Message}\n", true, true));
            }

            return existingKeys;
        }

        private void GetSourceReportFiles(string reportsPath, out FileInfo[] files)
        {
            // Check writeOff directory path 
            var writeOffDirectoryPath = Path.Combine(reportsPath, WRITEOFF_DIRECTORY_NAME);
            if (Directory.Exists(writeOffDirectoryPath))
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
        public async Task<int> GetSourceReportFiles(FileInfo[]? files)
        {
            StringBuilder message = new();
            var isError = false;

            if (files is null || files.Length == 0)
            {
                message.Append("Directory with files for processing does not exist or is empty, check it.");
                isError = true;
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
                        var  getSourceReportKeyByRegexTask = GetSourceReportKeyByRegex(fileInfo);
                        await Task.Run(() => getSourceReportKeyByRegexTask);
                        getSourceReportKeyByRegexTask.Wait();

                        var (key, loginsFileName) = getSourceReportKeyByRegexTask.Result;
                        if(key.HasValue) 
                        {
                            var sorceReportFile = new SourceReportFileForSKU(key.Value, fileInfo);

                            if(key.Value == SourceSKUReportFileKey.Ransoms && !string.IsNullOrEmpty(loginsFileName)) 
                            {
                                ClientLogins = GetLoginsFromFilename(loginsFileName);
                            }

                            // Store SourceReportFileForSKU
                            ReportsPaths[key.Value] = sorceReportFile;
                        } 
                        else 
                        {
                            message.Append($"\nUnable to determine key for report file {fileInfo.Name}");
                            isError = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        message.Append($"\nError SourceReportFileForSKU create: {ex.Message}");
                        isError = true;
                    }
                }

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
                        message.Append("\nPath to dir for working with client can`t be an empty string!");
                        isError = true;
                    }
                }
                catch (Exception ex)
                {
                    message.Append($"\nDefineDirInfoForClientTargetReports Error:\n{ex.Message}");
                    isError = true;
                }
            }

            ManagerReachedEventArgs args = new(message.ToString(), isError, false, 2000);
            OnManagerReached(args);

            return ReportsPaths.Count;
        }

        /// <summary>
        /// Get source report file for one client.
        /// </summary>
        /// <param name="key">Key to search for report file in dict.</param>
        /// <returns>SourceReportFileForSKU as Task (optional).</returns>
        public async Task<SourceReportFileForSKU?> GetSourceReport(SourceSKUReportFileKey key) =>
            await Task.Run(() => ReportsPaths.TryGetValue(key, out var reportFile) ? reportFile : null);

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
                if(ClientLogins.Last() != ClientLogins[i])
                {
                    stringBuilder.Append(", ");
                }
            }

            var dirClientsTargetReportsPath = Path.Combine(mpBoostClientsPath, stringBuilder.ToString());
            if(FileService.GetDirectoryInfo(dirClientsTargetReportsPath, out DirectoryInfo? dirInfoForClientTargetReports, true)) 
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
        public bool IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out List<SourceSKUReportFileKey> existingKeys)
        {
            existingKeys = lostKeys = new([]);

            var isContainsKeyRansoms = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Ransoms);
            var isContainsKeyBalance = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Balance);
            var isContainsKeyOutgoingProducts = ReportsPaths.ContainsKey(SourceSKUReportFileKey.OutgoingProducts);
            var isContainsKeyTransfer = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Transfered);
            var isContainsWriteOffPoints = ReportsPaths.ContainsKey(SourceSKUReportFileKey.WriteOffPoints);
            var isContainsWriteOffPointsViaWarehouse = ReportsPaths.ContainsKey(SourceSKUReportFileKey.WriteOffPointsViaWarehouse);
            var isContainsAdditional = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Additional);

            bool success = isContainsKeyRansoms && isContainsKeyBalance && isContainsWriteOffPoints && isContainsWriteOffPointsViaWarehouse;

            if (success)
            {
                if (isContainsKeyRansoms)
                    existingKeys.Add(SourceSKUReportFileKey.Ransoms);

                if (isContainsKeyBalance)
                    existingKeys.Add(SourceSKUReportFileKey.Balance);

                if (isContainsKeyOutgoingProducts)
                    existingKeys.Add(SourceSKUReportFileKey.OutgoingProducts);

                if (isContainsKeyTransfer)
                    existingKeys.Add(SourceSKUReportFileKey.Transfered);

                if(isContainsWriteOffPoints)
                    existingKeys.Add(SourceSKUReportFileKey.WriteOffPoints);

                if(isContainsWriteOffPointsViaWarehouse)
                    existingKeys.Add(SourceSKUReportFileKey.WriteOffPointsViaWarehouse);

                if (isContainsAdditional)
                    existingKeys.Add(SourceSKUReportFileKey.Additional);
            }
            else
            {
                if (!isContainsKeyRansoms)
                    lostKeys.Add(SourceSKUReportFileKey.Ransoms);

                if (!isContainsKeyBalance)
                    lostKeys.Add(SourceSKUReportFileKey.Balance);

                if (!isContainsKeyOutgoingProducts)
                    lostKeys.Add(SourceSKUReportFileKey.OutgoingProducts);

                if (!isContainsWriteOffPoints)
                    lostKeys.Add(SourceSKUReportFileKey.WriteOffPoints);

                if (!isContainsWriteOffPointsViaWarehouse)
                    lostKeys.Add(SourceSKUReportFileKey.WriteOffPointsViaWarehouse);
            }
   
            return success;
        }

        // TODO: Перенести в отдельный пакет     
        static readonly string persDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        /// ???
        static readonly string[] pathKeyNames = Enum.GetNames(typeof(PathKey));

        /// <summary>
        /// Gets the report file key using regex and the name of the file with client logins.
        /// </summary>
        /// <param name="fileInfo">Object to parse the file name for.</param>
        /// <returns>Report file key or null, the name of the file with client logins or null.</returns>
        /// <remarks><i>Rework, testing</i></remarks>
        private static async Task<(SourceSKUReportFileKey? key, string? loginsFileName)> GetSourceReportKeyByRegex(FileInfo fileInfo) 
        {
            SourceSKUReportFileKey? key = null;
            string? loginsFileName = null;

            var fileName = Path.GetFileName(fileInfo.Name);

            if(fileName.StartsWith("~$")) 
                fileName = fileName.TrimStart('~','$');
            
            foreach (var regexesSourceReport in RegexesForSourceReports)
            {
                foreach (var regex in regexesSourceReport.Value)
                {
                    var regexMatch = regex.Match(fileName);
                    if(regexMatch is not null && regexMatch.Success) 
                    {
                        key =  regexesSourceReport.Key;
                        if(key == SourceSKUReportFileKey.Ransoms) 
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
                if(start) 
                {
                    foreach (var c in chars)
                    {
                        if(str.StartsWith(c))
                            str = str.TrimStart(c);
                    }
                }

                if(end) 
                {
                    foreach (var c in chars)
                    {
                        if(str.EndsWith(c))
                            str = str.TrimEnd(c);
                    }
                }
            }
            
            List<string>? filenameStrings;

            var isSummary =  filename.Contains('_');
            filenameStrings = isSummary ? [.. filename.Split('_', '.')] : [.. filename.Split(' ', '.')];
       
            filenameStrings.RemoveRange(0,4);
            filenameStrings.RemoveRange(filenameStrings.Count - 1, 1); // remove xlsx

            // Single login
            if(filenameStrings.Count == 1) 
            {
                var element = filenameStrings[0];

                if(element.StartsWith('+'))
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
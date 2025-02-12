using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class FileManager : BaseManager
    {
        #region Variables and constants 
        // This will get the current WORKING directory (i.e. \bin\Debug)
        protected readonly string workingDirectory = Environment.CurrentDirectory;
        // This will get the current PROJECT directory

        private readonly static string _clientReportRegexPattern = @"(ClientReport_)\d+(_)+\d+(-)\d+(-)\d+(.xlsx)";
        private readonly Regex _clientReportRegex = new(_clientReportRegexPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        #endregion

        #region Properties
        public string ProjectDirectory { get; }

        /// <summary>
        /// Dictionary, where key is the key of report file for a client of a certain type, and value is report file itself.
        /// </summary>
        public Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> ReportsPathsSingleReport { get; private set; } = [];
        public Dictionary<string, SourceReportFileForSKU> ReportsPathsMultyReport { get; private set; } = new();
        
        public SourceReportFileForSKU? AdditionalPathsMultyReport { get; private set; }
        
        public static DirectoryInfo? DirInfoForClientTargetReports { get; private set; }
        public static string? InputReportsFilePath { get; set; } = string.Empty;
        public static string OutputClientReportFilePath { get; private set; } = string.Empty;
        #endregion

        #region Constructors 
        public FileManager() : base()
        {
            // Get project directory
            var getParentworkingDirectory = Directory.GetParent(workingDirectory) ?? throw new DirectoryNotFoundException();
            ProjectDirectory = getParentworkingDirectory.Parent!.Parent!.FullName;
        }
        #endregion

        #region  Functionality
        public static bool GetFileInfosFromDirInfoByExtensions(DirectoryInfo? dirInfo, out IEnumerable<FileInfo>? fileInfos, string ext)
        {
            fileInfos = from fileInfo in dirInfo?.GetFiles()
                        where fileInfo.Name.EndsWith(ext)
                        select fileInfo;

            return fileInfos is not null && fileInfos.Any();
        }

        /// <summary>
        /// Checks availability of necessary files for data analysis.
        /// </summary>
        /// <param name="reportsPath">Directory with client reports.</param>
        /// <returns>List of existing keys as Task result.</returns>
        public async Task<List<SourceSKUReportFileKey>?> ChecksAvailabilityNecessaryFiles(string reportsPath, bool isMulty, int delay)
        {
            ManagerReachedEventArgs? args = null;

            try
            {
                Task<int> getFilesTask = GetReportFiles(reportsPath, isMulty, delay);
                await Task.Run(() => getFilesTask);
                getFilesTask.Wait();
   
                var success = IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out List<SourceSKUReportFileKey> existingKeys, isMulty, delay);
                
                if(!success) 
                {
                    switch (isMulty)
                    {
                        case true:
                            await Task.Delay(delay);
                            args = new("There is not enough reports (>1) to generate a summary.", delay, true);
                            return null;
                        case false:
                            switch(lostKeys.Count > 0) 
                            {
                                case true:
                                    var lastKey = lostKeys.Last();
                                    StringBuilder lostKeysBuilder = new("Mandatory data for analysis not provided: ");

                                    foreach (var lostKey in lostKeys)
                                        lostKeysBuilder.Append(string.Concat(lostKey, !Equals(lostKey, lastKey) ? ", " : ". "));

                                    await Task.Delay(delay);
                                    args = new(lostKeysBuilder.ToString(), delay, true);
                                    return null;
    
                                case false:
                                    return null;
                            }
                    }
                }
                else 
                {
                    return existingKeys;
                }
            }
            catch (Exception ex)
            {
                await Task.Delay(100);
                args = new($"Data error: {ex.Message}\n", 2000, true);
                return null;
            }
            finally
            {
                if (args is not null)
                    OnManagerReached(args);
            }
        }

        /// <summary>
        /// Receives supplied files from directory.
        /// </summary>
        /// <param name="dirPathString">Directory path string.</param>
        /// <returns>Number of files transferred for analysis.</returns>
        /// <exception cref="Exception">Exception for empty directory without files.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<int> GetReportFiles(string dirPathString, bool isMulty, int delay)
        {
            ManagerReachedEventArgs? args = default;

            var files = GetFilesFromDirectory(dirPathString, EXCEL_FILE_EXTENSION);
            if (files is null || !files.Any())
            {
                args = new ManagerReachedEventArgs("Directory with files for processing does not exist or is empty, check it.", delay, false);
            }
            else
            {
                Trace.WriteLine($"Number of files in dir with ext \"{EXCEL_FILE_EXTENSION}s\": {files!.Count()}");
                await Task.Delay(delay);

                foreach (var fileInfo in files!)
                {
                    try
                    {
                        var sorceReportFile = new SourceReportFileForSKU(fileInfo, isMulty);
                        var key = sorceReportFile.ReportKey;

                        if (sorceReportFile != null && key.HasValue)
                        {
                            switch (isMulty)
                            {
                                case true:
                                    // Additional for multy report
                                    if(sorceReportFile.ReportKey == SourceSKUReportFileKey.Additional) 
                                    {
                                        AdditionalPathsMultyReport = sorceReportFile;
                                    } 
                                    else 
                                    {
                                        // Save filePath by key 
                                        string loginKey = sorceReportFile.Login!;
                                        ReportsPathsMultyReport[loginKey] = sorceReportFile;
                                    }
                                    break;
                                case false:
                                    ReportsPathsSingleReport[key.Value] = sorceReportFile;

                                    // Save login
                                    if (sorceReportFile?.Login != null)
                                        ClientLogin = sorceReportFile.Login;

                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        args = new($"Error SourceReportFileForSKU create: {ex.Message}", delay, true);
                        throw;
                    }
                }

                string readinessMessage = string.Empty;

                if (isMulty)
                {
                    var logins = string.Join(",", ReportsPathsMultyReport.Keys);
                    readinessMessage = string.Concat($"Логины клиентов: {logins}\n", $"{ReportsPathsMultyReport.Count} файлов (-ла) готово к обработке.\n");
                }
                else
                {
                    readinessMessage = string.Concat($"Логин клиента: {ClientLogin}\n", $"{ReportsPathsSingleReport.Count} файлов (-ла) готово к обработке.\n");
                }

                try
                {
                    var clientOutputPathTask = DefineDirInfoForClientTargetReports(isMulty);
                    await Task.Run(async () => await clientOutputPathTask);
                    clientOutputPathTask.Wait();
                    var clientOutputPath = clientOutputPathTask.Result;

                    if (!string.IsNullOrEmpty(clientOutputPath))
                    {
                        readinessMessage = string.Concat(readinessMessage, $"Directory for target report: {clientOutputPath}");
                    }
                    else
                    {
                        throw new Exception("Path to dir for working with client can`t be an empty string!");
                    }
                }
                catch (System.Exception ex)
                {
                    args = new($"DefineDirInfoForClientTargetReports Error: {ex.Message}", delay, true);
                    throw;
                }

                args = new(readinessMessage, 500, false);
            }

            if (args != null) OnManagerReached(args);

            return files is null || !files.Any() ? 0 : ReportsPathsSingleReport.Count;
        }

        /// <summary>
        /// Returns info about report file by key.
        /// </summary>
        /// <param name="key">Key to search for report file in dict.</param>
        /// <returns>SourceReportFileForSKU as Task (optional).</returns>
        /// <remarks><i>New version.</i></remarks>
        public async Task<SourceReportFileForSKU?> GetReportFile(SourceSKUReportFileKey key) =>
            await Task.Run(() => ReportsPathsSingleReport.TryGetValue(key, out var reportFile) ? reportFile : null);

        /// <summary>
        /// Define directory for client report output.
        /// </summary>
        /// <returns>Path string to directory to save data.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<string?> DefineDirInfoForClientTargetReports(bool isMulty = false)
        {
            ManagerReachedEventArgs args;

            if (!isMulty && ClientLogin is null)
            {
                await Task.Delay(100);
                args = new("Clint's login is not defined, so it is impossible to create a directory.", 2000, true);
                OnManagerReached(args);
                return null;
            }

            DirInfoForClientTargetReports = GetDirectoryInfo(PathKey.MPBoostClients, !isMulty ? ClientLogin : string.Join(",", ReportsPathsMultyReport.Keys));

            if (DirInfoForClientTargetReports == null)
            {
                await Task.Delay(100);
                args = new("Clint's login is not defined, so it is impossible to create a directory.", 500, true);
                OnManagerReached(args);
                return null;
            }

            var dateTime = DateTime.Now.ToString("dd-MM-yyyy");

            OutputClientReportFilePath = !isMulty ?
                $"{DirInfoForClientTargetReports!.FullName}/ClientReport_{ClientLogin}_{dateTime}.xlsx" :
                $"{DirInfoForClientTargetReports!.FullName}/ClientsReportSummary_{ClientLogin}_{dateTime}.xlsx";

            args = new($"Directory for final reports:{DirInfoForClientTargetReports.FullName}\nClients target report file name:{OutputClientReportFilePath}", 1000, false);
            OnManagerReached(args);

            return DirInfoForClientTargetReports.FullName ?? null;
        }

        /// <summary>
        /// Checks whether required files have been added to create a client report.
        /// </summary>
        /// <param name="lostKeys">A collection of required file keys that were not provided.</param>
        /// <returns>Logical flag for performing an operation.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public bool IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out List<SourceSKUReportFileKey> existingKeys, bool isMulty, int delay)
        {
            existingKeys = lostKeys = new([]);
            bool success = false;

            if (isMulty)
            {
                IEnumerable<KeyValuePair<string, SourceReportFileForSKU>>? matchingReport = 
                    from item in ReportsPathsMultyReport
                    where _clientReportRegex.Match(item.Value.FileInfo.Name).Success
                    select item;    

                success = matchingReport.Count() > 1;
            }
            else
            {
                var isContainsKeyRansomsWB = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.RansomsWB);
                var isContainsKeyRansomsOZ = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.RansomsOZ);
                bool isRansomsExisting = isContainsKeyRansomsWB || isContainsKeyRansomsOZ;

                var isContainsKeyStorageWB = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.RemainingWB);
                var isContainsKeyStorageOZ = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.RemainingOZ);
                bool isRemainingExisting = isContainsKeyStorageWB || isContainsKeyStorageOZ;

                var isContainsKeyShipped = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.Shipped);

                var isContainsKeyTransfer = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.Transfer);

                var isContainsWriteOffShortages = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.WriteOffShortages);

                var isContainsAdditional = ReportsPathsSingleReport.ContainsKey(SourceSKUReportFileKey.Additional);

                success = isRansomsExisting && isRemainingExisting && isContainsWriteOffShortages;

                if (success)
                {
                    if (isContainsKeyRansomsWB)
                        existingKeys.Add(SourceSKUReportFileKey.RansomsWB);

                    if (isContainsKeyRansomsOZ)
                        existingKeys.Add(SourceSKUReportFileKey.RansomsOZ);

                    if (isContainsKeyStorageWB)
                        existingKeys.Add(SourceSKUReportFileKey.RemainingWB);

                    if (isContainsKeyStorageOZ)
                        existingKeys.Add(SourceSKUReportFileKey.RemainingOZ);

                    if (isContainsKeyShipped)
                        existingKeys.Add(SourceSKUReportFileKey.Shipped);

                    if (isContainsKeyTransfer)
                        existingKeys.Add(SourceSKUReportFileKey.Transfer);

                    existingKeys.Add(SourceSKUReportFileKey.WriteOffShortages);

                    if (isContainsAdditional)
                        existingKeys.Add(SourceSKUReportFileKey.Additional);
                }
                else
                {
                    if (!isContainsKeyRansomsWB)
                        lostKeys.Add(SourceSKUReportFileKey.RansomsWB);

                    if (!isContainsKeyRansomsOZ)
                        lostKeys.Add(SourceSKUReportFileKey.RansomsOZ);

                    if (!isContainsKeyStorageWB)
                        lostKeys.Add(SourceSKUReportFileKey.RemainingWB);

                    if (!isContainsKeyStorageOZ)
                        lostKeys.Add(SourceSKUReportFileKey.RemainingOZ);

                    if (!isContainsKeyShipped)
                        lostKeys.Add(SourceSKUReportFileKey.Shipped);

                    if (!isContainsWriteOffShortages)
                        lostKeys.Add(SourceSKUReportFileKey.WriteOffShortages);
                }
            }
            return success;
        }

        /// <summary>
        /// Gets the FileInfo collection for client reporting files.
        /// </summary>
        /// <param name="dirPathString">Path string to directory to search for files.</param>
        /// <param name="ext">File extensions.</param>
        /// <returns>Сollection for client reporting files.</returns>
        /// <exception cref="DirectoryNotFoundException">Exception: Directory at spec path was not found.</exception>
        /// <exception cref="Exception">Exception: Cannot create DirectoryInfo for directory specified in path.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public static IEnumerable<FileInfo>? GetFilesFromDirectory(string dirPathString, string? ext = null)
        {
            if(dirPathString.Contains('\'')) 
            {
                dirPathString = dirPathString.Replace("'", "");
            }

            if (!Directory.Exists(dirPathString))
                throw new DirectoryNotFoundException($"Directory at path {dirPathString} was not found.");

            var dirInfo = new DirectoryInfo(dirPathString) ?? throw new Exception($"Cannot make DirectoryInfo path {dirPathString}");

            if (ext == null)
            {
                return dirInfo.GetFiles(); ;
            }
            else
            {
                return from fileInfo in dirInfo.GetFiles()
                       where fileInfo.Name.EndsWith(EXCEL_FILE_EXTENSION)
                       select fileInfo;
            }
        }

        /// ???        
        static readonly string persDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        /// ???
        static readonly string[] pathKeyNames = Enum.GetNames(typeof(PathKey));

        public static DirectoryInfo? CreateDirectoryInfo(PathKey pathKey, string? login = null)
        {
            // Documents/MPBoostClients

            if (login is null)
            {
                return pathKey switch
                {
                    PathKey.MPBoostClients => Directory.CreateDirectory(persDirPath + $"/{pathKeyNames[0]}/{pathKeyNames[1]}"),
                    _ => null
                };
            }
            else
            {
                return pathKey switch
                {
                    PathKey.MPBoostClients => Directory.CreateDirectory(persDirPath + $"/{pathKeyNames[0]}/{pathKeyNames[1]}/{login}"),
                    _ => null
                };
            }
        }

        public static DirectoryInfo? GetDirectoryInfo(PathKey pathKey, string? login = null, string[]? logins = null)
        {
            DirectoryInfo? result = null;
            string directoryName = string.Empty;

            switch (string.IsNullOrEmpty(login), logins is null)
            {
                case (true, true):
                    switch (pathKey)
                    {
                        case PathKey.Documents:
                            result = new DirectoryInfo(persDirPath + $"/{pathKeyNames[0]}");
                            break;
                        case PathKey.MPBoostClients:
                            result = new DirectoryInfo(persDirPath + $"/{pathKeyNames[0]}/{pathKeyNames[1]}");
                            break;
                        default:
                            break;
                    }
                    break;

                case (true, false):
                    directoryName = string.Join(",", logins!);
                    break;

                case (false, true):
                    directoryName = login!;
                    break;
            }

            string path = persDirPath + $"/{pathKeyNames[0]}" + $"/{pathKeyNames[1]}" + $"/{directoryName}";

            if (Directory.Exists(path))
            {
                result = new DirectoryInfo(path);
            }
            else
            {
                result = CreateDirectoryInfo(pathKey, directoryName);
            }

            return result;
        }
        #endregion
    }
}
using System.Text;
using System.Text.RegularExpressions;
using CSharpHelpers.Services;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class ReportFileManager : BaseManager
    {
        #region Variables and constants 
        private readonly static string _clientReportRegexPattern = @"(ClientReport_)\d+(_)+\d+(-)\d+(-)\d+(.xlsx)";
        private readonly Regex _clientReportRegex = new(_clientReportRegexPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        #endregion

        #region Properties

        /// <summary>
        /// Dictionary, where key is the key of report file for a client of a certain type, and value is report file itself.
        /// </summary>
        public Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> ReportsPathsSingle { get; private set; } = [];
        public Dictionary<string, SourceReportFileForSKU> ReportsPathsMultyReport { get; private set; } = new();
        
        public SourceReportFileForSKU? AdditionalPathsMultyReport { get; private set; }
        
        public static DirectoryInfo? DirInfoForClientTargetReports { get; private set; }
        public static string? InputReportsFilePath { get; set; } = string.Empty;
        public static string OutputClientReportFilePath { get; private set; } = string.Empty;
        #endregion

        #region  Functionality

        /// <summary>
        /// Checks availability of necessary files for data analysis.
        /// </summary>
        /// <param name="reportsPath">Directory with client reports.</param>
        /// <returns>List of existing keys as Task result.</returns>
        public async Task<List<SourceSKUReportFileKey>?> ChecksAvailabilityNecessaryFiles(string reportsPath, bool isMulty, bool isTracing, int tracingDelay)
        {
            List<SourceSKUReportFileKey> existingKeys = [];
            string message = string.Empty;
            bool isError = false;

            try
            {
                Task<int> getFilesTask = GetSourceReportFiles(reportsPath, isMulty, validFileExtensions);
                await Task.Run(() => getFilesTask);
                getFilesTask.Wait();

                var getReportFilesResult = getFilesTask.Result;

                if(getFilesTask.Result == 0) 
                {
                    OnManagerReached(new(string.Empty, true, true));
                } 

                var success = IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out existingKeys, isMulty, tracingDelay);
                
                if(!success) 
                {
                    switch (isMulty)
                    {
                        case true:
                            message = "There is not enough reports (>1) to generate a summary.";
                            isError = true;
                            break;
                        case false:
                            switch(lostKeys.Count > 0) 
                            {
                                case true:
                                    var lastKey = lostKeys.Last();
                                    StringBuilder lostKeysBuilder = new("Mandatory data for analysis not provided: ");

                                    foreach (var lostKey in lostKeys)
                                        lostKeysBuilder.Append(string.Concat(lostKey, !Equals(lostKey, lastKey) ? ", " : ". "));
                                    message = lostKeysBuilder.ToString();
                                    isError = true;
                                    break;
    
                                case false:
                                    break;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"Data error: {ex.Message}\n";
                isError = true;
            }

            OnManagerReached(new(message, isError, false));
      
            return existingKeys;
        }

        /// <summary>
        /// Get source report files.
        /// </summary>
        /// <param name="dirPath">Directory for receiving reports.</param>
        /// <param name="isSummaryReport">Flag whether the report is a summary report.</param>
        /// <param name="validFileExtensions"Collection of valid file extensions.</param>
        /// <returns>Number of report files received.</returns>
        public async Task<int> GetSourceReportFiles(string dirPath, bool isSummaryReport, string[] validFileExtensions)
        {
            var count = 0;
            StringBuilder message = new();
            var isError = false;

            FileService.SearchFilesInDirectory(dirPath, out FileInfo[]? files, validFileExtensions);

            if (files is null || files.Length == 0)
            {
                message.Append("Directory with files for processing does not exist or is empty, check it.");
                isError = true;
            }
            else
            {
                message.Append($"\nNumber of files in dir with ext \"{validFileExtensions?.ToString()}\": {files!.Length}");

                foreach (var fileInfo in files!)
                {
                    try
                    {
                        var sorceReportFile = new SourceReportFileForSKU(fileInfo, isSummaryReport);
                        var key = sorceReportFile.ReportKey;

                        if (sorceReportFile != null && key.HasValue)
                        {
                            switch (isSummaryReport)
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
                                    ReportsPathsSingle[key.Value] = sorceReportFile;

                                    // Save login
                                    if (sorceReportFile?.Login != null)
                                        ClientLogin = sorceReportFile.Login;

                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        message.Append($"\nError SourceReportFileForSKU create: {ex.Message}");
                        isError = true;
                    }
                }

                if (isSummaryReport)
                {
                    count = ReportsPathsMultyReport.Count;
                    var logins = string.Join(",", ReportsPathsMultyReport.Keys);
                    message.Append($"\nClient logins: {logins}\n{ReportsPathsMultyReport.Count} file(s) ready for processing.");
                }
                else
                {
                    count = ReportsPathsSingle.Count;
                    message.Append($"\nClient login: {ClientLogin}\n{ReportsPathsSingle.Count} file(s) ready for processing.");
                }

                try
                {
                    var clientOutputPathTask = DefineDirInfoForClientTargetReports(isSummaryReport);
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

            ManagerReachedEventArgs args = new(message.ToString(), isError, false);
            OnManagerReached(args);

            return count;
        }

        /// <summary>
        /// Get source report file for one client.
        /// </summary>
        /// <param name="key">Key to search for report file in dict.</param>
        /// <returns>SourceReportFileForSKU as Task (optional).</returns>
        public async Task<SourceReportFileForSKU?> GetSourceReportForSingleClient(SourceSKUReportFileKey key) =>
            await Task.Run(() => ReportsPathsSingle.TryGetValue(key, out var reportFile) ? reportFile : null);

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
                args = new("Clint's login is not defined, so it is impossible to create a directory.", true, true);
                OnManagerReached(args);
                return null;
            }

            DirInfoForClientTargetReports = GetDirectoryInfo(PathKey.MPBoostClients, !isMulty ? ClientLogin : string.Join(",", ReportsPathsMultyReport.Keys));

            if (DirInfoForClientTargetReports == null)
            {
                await Task.Delay(100);
                args = new("Clint's login is not defined, so it is impossible to create a directory.", true, true);
                OnManagerReached(args);
                return null;
            }

            var dateTime = DateTime.Now.ToString("dd-MM-yyyy");

            OutputClientReportFilePath = !isMulty ?
                $"{DirInfoForClientTargetReports!.FullName}/ClientReport_{ClientLogin}_{dateTime}.xlsx" :
                $"{DirInfoForClientTargetReports!.FullName}/ClientsReportSummary_{ClientLogin}_{dateTime}.xlsx";

            args = new($"Directory for final reports:{DirInfoForClientTargetReports.FullName}\nClients target report file name:{OutputClientReportFilePath}", false, false);
            OnManagerReached(args);

            return DirInfoForClientTargetReports.FullName ?? null;
        }

        /// <summary>
        /// Checks whether required files have been added to create a client report.
        /// </summary>
        /// <param name="lostKeys">A collection of required file keys that were not provided.</param>
        /// <returns>Logical flag for performing an operation.</returns>
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
                var isContainsKeyRansomsWB = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.RansomsWB);
                var isContainsKeyRansomsOZ = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.RansomsOZ);
                bool isRansomsExisting = isContainsKeyRansomsWB || isContainsKeyRansomsOZ;

                var isContainsKeyStorageWB = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.RemainingWB);
                var isContainsKeyStorageOZ = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.RemainingOZ);
                bool isRemainingExisting = isContainsKeyStorageWB || isContainsKeyStorageOZ;

                var isContainsKeyShipped = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.Shipped);

                var isContainsKeyTransfer = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.Transfer);

                var isContainsWriteOffShortages = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.WriteOffShortages);

                var isContainsAdditional = ReportsPathsSingle.ContainsKey(SourceSKUReportFileKey.Additional);

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
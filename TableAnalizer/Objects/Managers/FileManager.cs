
using TableAnalizer.Enums;
using TableAnalizer.Objects.SKUItems;

namespace TableAnalizer.Objects.Managers
{
    public class FileManager : BaseManager
    {
        #region Variables and constants 
        // This will get the current WORKING directory (i.e. \bin\Debug)
        protected readonly string workingDirectory = Environment.CurrentDirectory;
        // This will get the current PROJECT directory
        #endregion

        #region Properties
        public string ProjectDirectory { get; }

        /// <summary>
        /// Dictionary, where key is the key of report file for a client of a certain type, and value is report file itself.
        /// </summary>
        public Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> ReportsPaths { get; private set; } = new();
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
        public async Task<List<SourceSKUReportFileKey>?> ChecksAvailabilityNecessaryFiles(string reportsPath)
        {
            ManagerReachedEventArgs? args = null;

            try
            {
                var fileCount = GetReportFiles(reportsPath);

                Console.WriteLine(fileCount.Result);

                var success = IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out List<SourceSKUReportFileKey> existingKeys);

                if (!success || lostKeys.Count > 0)
                {
                    var lastKey = lostKeys.Last();
                    var lostKeysString = string.Empty;

                    foreach (var lostKey in lostKeys)
                        lostKeysString = string.Concat(lostKeysString, lostKey, !Equals(lostKey, lastKey) ? ", " : ". ");

                    await Task.Delay(100);
                    args = new (string.Concat("Не предоставлены обязательные данные для анализа: ", lostKeysString), 2000, true);
                    return null;
                }
                else 
                {
                    return existingKeys;
                }
            }
            catch (System.Exception ex)
            {
                await Task.Delay(100);
                args = new (string.Concat($"Ошибка данных: {ex.Message}", "\n"), 2000, true);
                return null;
            } 
            finally 
            {
                if(args is not null)
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
        public async Task<int> GetReportFiles(string dirPathString)
        {
            ManagerReachedEventArgs? args = default;

            var files = GetFilesFromDirectory(dirPathString, EXCEL_FILE_EXTENSION);

            if (files is null || !files.Any())
            {
                args = new ManagerReachedEventArgs("Directory with files for processing does not exist or is empty, check it.", 1000, false);
            }
            else
            {
                foreach (var fileInfo in files!)
                {
                    Console.WriteLine(fileInfo.Name);


                    try
                    {
                        var sorceReportFile = new SourceReportFileForSKU(fileInfo);
                        var key = sorceReportFile.ReportKey;

                        if (sorceReportFile != null && key.HasValue)
                        {
                            // Save filePath by key 
                            ReportsPaths[key.Value] = sorceReportFile;

                            // Save login
                            if (sorceReportFile?.Login != null)
                                ClientLogin = sorceReportFile.Login;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        args = new($"{ex.Message}", 500, true);
                        throw;
                    }
                }

                var readinessMessage = $"Логин клиента: {ClientLogin}\n";
                readinessMessage = string.Concat(readinessMessage, $"{ReportsPaths.Count} файлов (-ла) готово к обработке.\n");

                try
                {
                    var clientOutputPathTask = DefineDirInfoForClientTargetReports();
                    await Task.Run(async() => await clientOutputPathTask);
                    clientOutputPathTask.Wait();
                    var clientOutputPath = clientOutputPathTask.Result;
                    
                    if (!string.IsNullOrEmpty(clientOutputPath))
                    {
                        readinessMessage = string.Concat(readinessMessage, $"Директория для работы с клиентом: {clientOutputPath}");
                    } else 
                    {
                        throw new Exception("Путь к диретории для работы с клиентом не может быть пустой строкой!");
                    }
                }
                catch (System.Exception ex)
                {
                    args = new($"{ex.Message}", 500, true);
                    throw;
                }

                args = new(readinessMessage, 500, false);
            }

            if (args != null) OnManagerReached(args);

            return files is null || !files.Any() ? 0 : ReportsPaths.Count;
        }

        /// <summary>
        /// Returns info about report file by key.
        /// </summary>
        /// <param name="key">Key to search for report file in dict.</param>
        /// <returns>SourceReportFileForSKU as Task (optional).</returns>
        /// <remarks><i>New version.</i></remarks>
        public async Task<SourceReportFileForSKU?> GetReportFile(SourceSKUReportFileKey key) =>
            await Task.Run(() => ReportsPaths.TryGetValue(key, out var reportFile) ? reportFile : null);

        /// <summary>
        /// Define directory for client report output.
        /// </summary>
        /// <returns>Path string to directory to save data.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<string?> DefineDirInfoForClientTargetReports()
        {
            ManagerReachedEventArgs args;

            if (ClientLogin is null)
            {
                await Task.Delay(100);
                args = new("Clint's login is not defined, so it is impossible to create a directory.", 2000, true);
                OnManagerReached(args);
                return null;
            }

            DirInfoForClientTargetReports = GetDirectoryInfo(PathKey.MPBoostClients, ClientLogin);

            if (DirInfoForClientTargetReports == null)
            {
                await Task.Delay(100);
                args = new("Clint's login is not defined, so it is impossible to create a directory.", 500, true);
                OnManagerReached(args);
                return null;
            }

            var dateTime = DateTime.Now.ToString("dd-MM-yyyy");
            OutputClientReportFilePath = $"{DirInfoForClientTargetReports!.FullName}/ClientReport_{ClientLogin}_{dateTime}.xlsx";

            args = new($"Directory for client final reports:{DirInfoForClientTargetReports.FullName}\nClient target report file name:{OutputClientReportFilePath}", 1000, false);
            OnManagerReached(args);

            return DirInfoForClientTargetReports.FullName ?? null;
        }

        /// <summary>
        /// Checks whether required files have been added to create a client report.
        /// </summary>
        /// <param name="lostKeys">A collection of required file keys that were not provided.</param>
        /// <returns>Logical flag for performing an operation.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public bool IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys, out List<SourceSKUReportFileKey> existingKeys)
        {
            existingKeys = new(Array.Empty<SourceSKUReportFileKey>());
            lostKeys = new(Array.Empty<SourceSKUReportFileKey>());

            var isContainsKeyRansomsWB = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RansomsWB);
            var isContainsKeyRansomsOZ = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RansomsOZ);
            bool isRansomsExisting = isContainsKeyRansomsWB || isContainsKeyRansomsOZ;

            var isContainsKeyStorageWB = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RemainingWB);
            var isContainsKeyStorageOZ = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RemainingOZ);
            bool isRemainingExisting = isContainsKeyStorageWB || isContainsKeyStorageOZ;

            var isContainsKeyShipped = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Shipped);

            var isContainsKeyTransfer = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Transfer);

            var isContainsWriteOffShortages = ReportsPaths.ContainsKey(SourceSKUReportFileKey.WriteOffShortages);

            var isContainsAdditional = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Additional);

            var success = isRansomsExisting && isRemainingExisting && isContainsWriteOffShortages;
            if(success) 
            {
                if(isContainsKeyRansomsWB)
                    existingKeys.Add(SourceSKUReportFileKey.RansomsWB);
                
                if(isContainsKeyRansomsOZ)
                    existingKeys.Add(SourceSKUReportFileKey.RansomsOZ);

                if(isContainsKeyStorageWB)
                   existingKeys.Add(SourceSKUReportFileKey.RemainingWB);
                
                if(isContainsKeyStorageOZ)
                    existingKeys.Add(SourceSKUReportFileKey.RemainingOZ);
                
                if(isContainsKeyShipped)
                    existingKeys.Add(SourceSKUReportFileKey.Shipped);

                if(isContainsKeyTransfer)
                    existingKeys.Add(SourceSKUReportFileKey.Transfer);

                existingKeys.Add(SourceSKUReportFileKey.WriteOffShortages);

                if(isContainsAdditional)
                    existingKeys.Add(SourceSKUReportFileKey.Additional);
            } 
            else 
            {
                if(!isContainsKeyRansomsWB)
                    lostKeys.Add(SourceSKUReportFileKey.RansomsWB);
                
                if(!isContainsKeyRansomsOZ)
                    lostKeys.Add(SourceSKUReportFileKey.RansomsOZ);

                if(!isContainsKeyStorageWB)
                   lostKeys.Add(SourceSKUReportFileKey.RemainingWB);
                
                if(!isContainsKeyStorageOZ)
                    lostKeys.Add(SourceSKUReportFileKey.RemainingOZ);

                if(!isContainsKeyShipped)
                    lostKeys.Add(SourceSKUReportFileKey.Shipped);
                
                if(!isContainsWriteOffShortages)
                    lostKeys.Add(SourceSKUReportFileKey.WriteOffShortages);
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
            if (!Directory.Exists(dirPathString))
                throw new DirectoryNotFoundException($"Directory at path {dirPathString} was not found");

            var dirInfo = new DirectoryInfo(dirPathString) ?? throw new Exception($"Cannot make DirectoryInfo path {dirPathString}");

            if (ext == null)
            {
                var files = dirInfo.GetFiles();

                foreach (var file in files)
                {
                    Console.WriteLine(file.Name);
                }


                return files;
            }
            else
            {
                return from fileInfo in dirInfo.GetFiles()
                       where fileInfo.Name.EndsWith(EXCEL_FILE_EXTENSION)
                       select fileInfo;
            }
        }

        // TODO: Описать функционал 
        public static bool DeleteExistingFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            return !File.Exists(path);
        }

        #endregion

        /// ???        
        static readonly string persDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        /// ???
        static readonly string[] pathKeyNames = Enum.GetNames(typeof(PathKey));
        /// ???
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
        /// ???
        public static DirectoryInfo? GetDirectoryInfo(PathKey pathKey, string? login = null)
        {
            if (login is null)
            {
                return pathKey switch
                {
                    PathKey.Documents => new DirectoryInfo(persDirPath + $"/{pathKeyNames[0]}"),
                    PathKey.MPBoostClients => new DirectoryInfo(persDirPath + $"/{pathKeyNames[0]}/{pathKeyNames[1]}"),
                    _ => null,
                };
            }
            else
            {

                var path = persDirPath + $"/{pathKeyNames[0]}" + $"/{pathKeyNames[1]}" + $"/{login}";

                if (Directory.Exists(path))
                {
                    return new DirectoryInfo(path);
                }
                else
                {
                    return CreateDirectoryInfo(pathKey, login);
                }
            }
        }
    }
}
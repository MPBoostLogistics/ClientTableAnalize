using System.Diagnostics;
using ClientTableAnalizer.Enums;
using ClientTableAnalizer.Objects.SKUItems;

namespace ClientTableAnalizer.Objects.Managers
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
        public Dictionary<SourceSKUReportFileKey, SourceReportFileForSKU> ReportsPaths { get; private set; } = new();
        public static DirectoryInfo? ClientDirectoryInfo { get; private set; }
        public static string OutputReportFilePath { get; private set; } = string.Empty;
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
        /// <summary>
        /// Receives supplied files from directory.
        /// </summary>
        /// <param name="dirPathString">Directory path string.</param>
        /// <returns>Number of files transferred for analysis.</returns>
        /// <exception cref="Exception">Exception for empty directory without files.</exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<int> GetReportFiles(string dirPathString)
        {
            Trace.WriteLine(dirPathString);


            ManagerReachedEventArgs? args = default;

            var files = GetFilesFromDirectory(dirPathString, reportFileExtension);

            if (files is null || !files.Any())
            {
                args = new ManagerReachedEventArgs("Директория с файлами для обработки не сущесвтует или пустая, проверьте ее.",1000, false);
            }
            else
            {
                foreach (var fileInfo in files!)
                {
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
                    var clientOutputPath = await Task.Run(GetClientOutputPath);

                    if (!string.IsNullOrEmpty(clientOutputPath))
                    {
                        readinessMessage = string.Concat(readinessMessage, $"Директория для работы с клиентом: {clientOutputPath}");
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
        /// Define directory for client report output.
        /// </summary>
        /// <returns>Path string to directory to save data.</returns>
        /// <exception cref="Exception"></exception>
        /// <remarks><i>New version, tested</i></remarks>
        public async Task<string?> GetClientOutputPath()
        {
            if (ClientLogin is null)
                throw new Exception("Логин клинта не определен, поэтому невозможно создать директорию.");

            ClientDirectoryInfo = GetDirectoryInfo(PathKey.MPBoostClients, ClientLogin);

            if (ClientLogin != null && ClientDirectoryInfo != null)
            {
                var dateTime = DateTime.Now.ToString("dd-MM-yyyy");
                OutputReportFilePath = $"{ClientDirectoryInfo.FullName}/ClientReport_{ClientLogin}_{dateTime}.xlsx";

                return ClientDirectoryInfo.FullName;
            }
            else
            {
                await Task.Delay(100);
                throw new Exception("Логин клинта не определен, невозможно создать директорию для работы с ним.");
            }
        }

         /// <summary>
        /// Checks whether required files have been added to create a client report.
        /// </summary>
        /// <param name="lostKeys">A collection of required file keys that were not provided.</param>
        /// <returns>Logical flag for performing an operation.</returns>
        /// <remarks><i>New version, tested</i></remarks>
        public bool IsRequiredFilesProvided(out List<SourceSKUReportFileKey> lostKeys)
        {
            lostKeys = new(Array.Empty<SourceSKUReportFileKey>());

            var isContainsKeyRedemtionWB = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RansomsWB);
            var isContainsKeyRedemtionOZ = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RedemtionOZ);

            var isContainsKeyStorageWB = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RemainingWB);
            var isContainsKeyStorageOZ = ReportsPaths.ContainsKey(SourceSKUReportFileKey.RemainingOZ);

            var isContainsKeyShipped = ReportsPaths.ContainsKey(SourceSKUReportFileKey.Shipped);

            var isWBSuccess = isContainsKeyRedemtionWB && isContainsKeyStorageWB && isContainsKeyShipped;
            if (!isWBSuccess)
            {
                if (!isContainsKeyRedemtionWB) lostKeys.Add(SourceSKUReportFileKey.RansomsWB);
                if (!isContainsKeyStorageWB) lostKeys.Add(SourceSKUReportFileKey.RemainingWB);
            }

            var isOZSuccess = isContainsKeyRedemtionOZ && isContainsKeyStorageOZ && isContainsKeyShipped;
            if (!isOZSuccess)
            {
                if (!isContainsKeyRedemtionOZ) lostKeys.Add(SourceSKUReportFileKey.RedemtionOZ);
                if (!isContainsKeyStorageOZ) lostKeys.Add(SourceSKUReportFileKey.RemainingOZ);
            }

            if (!isContainsKeyShipped) lostKeys.Add(SourceSKUReportFileKey.Shipped);

            var success = isWBSuccess == true || isOZSuccess == true;
            return success;
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
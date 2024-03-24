namespace ClientTableAnalizer.Objects.Managers
{

    public class BaseManager
    {

        #region
        // This will get the current WORKING directory (i.e. \bin\Debug)
        protected readonly string workingDirectory = Environment.CurrentDirectory;
        // This will get the current PROJECT directory
        public string ProjectDirectory { get; }
        /// <summary>
        /// Manager event delegate.
        /// </summary>
        public event EventHandler<ManagerReachedEventArgs>? ManagerReached;

        #endregion

        public delegate void ManagerHandler(string message);

        #region Constuctor and destructors
        protected BaseManager()
        {
            // Get project directory
            var getParentworkingDirectory = Directory.GetParent(workingDirectory) ?? throw new DirectoryNotFoundException();
            ProjectDirectory = getParentworkingDirectory.Parent!.Parent!.FullName;
        }
        #endregion


        protected static bool IsDirectoryExists(string dirPathString) => Directory.Exists(dirPathString);

        protected static bool IsFileExists(string filePathString) => File.Exists(filePathString);

        protected static IEnumerable<FileInfo>? GetFilesFromDirectory(string dirPathString, string? ext = null)
        {
            if (!IsDirectoryExists(dirPathString))
                throw new DirectoryNotFoundException($"Directory at path {dirPathString} was not found");

            var dirInfo = new DirectoryInfo(dirPathString) ?? throw new Exception($"Cannot make DirectoryInfo path {dirPathString}");

            if (ext == null)
            {
                return dirInfo.GetFiles();
            }
            else
            {
                return from fileInfo in dirInfo.GetFiles()
                       where fileInfo.Name.EndsWith(".xlsx")
                       select fileInfo;
            }
        }

        protected static bool IsDataPathExists(string path, out FileInfo fileInfo)
        {
            if (!IsFileExists(path))
                throw new FileNotFoundException($"Файл по пути\n{path}\nне найден. Проверьте правильность указания данных.");

            fileInfo = new FileInfo(path);
            return true;
        }

        protected static int? ParseValueToInt(object? obj) =>
            obj is not null && !string.IsNullOrEmpty(obj.ToString()) ?
            int.Parse(obj.ToString()!.Trim()) :
            null;

        /// <summary>
        /// Raises a manager event with arguments.
        /// </summary>
        /// <param name="e">Manager event arguments.</param>ю
        protected virtual void OnManagerReached(ManagerReachedEventArgs e) =>
            ManagerReached?.Invoke(this, e);
    }

}
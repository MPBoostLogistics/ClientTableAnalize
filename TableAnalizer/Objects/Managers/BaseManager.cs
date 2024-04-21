namespace ClientTableAnalizer.Objects.Managers
{
    public class BaseManager
    {
        protected const string reportFileExtension = ".xlsx";

        #region Properties 
        public string? ClientLogin { get; protected set; } = string.Empty;
        #endregion

        #region
        /// <summary>
        /// Manager event delegate.
        /// </summary>
        public event EventHandler<ManagerReachedEventArgs>? ManagerReached;

        #endregion

        public delegate void ManagerHandler(string message);

        #region Constuctor and destructors
        protected BaseManager()
        {

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

        // Из старой реалиазции
        private static string FormСorrectPath(string path)
        {
            var copy = path;

            // Clear "\"
            var charArray = copy.Trim().ToList();
            for (int i = 0; i < charArray.Count; i++)
            {
                if (charArray[i] == '\u005c') charArray.RemoveAt(i);
            }

            copy = new string(charArray.ToArray());

            // Add "/"
            return $"/{copy}";
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
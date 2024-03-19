namespace ClientTableAnalizer.Objects.Managers
{
    public class BaseManager
    {
 
        
        #region Constuctor and destructors
        protected BaseManager()
        {
            
        }
        #endregion

        protected static bool IsDataPathExists(string path, out FileInfo fileInfo) 
        {
            if (!File.Exists(path)) 
                throw new FileNotFoundException($"Файл по пути\n{path}\nне найден. Проверьте правильность указания данных.");

            fileInfo = new FileInfo(path);
            return true;
        }

        protected static int? ParseValueToInt(object? obj) =>
            obj is not null && !string.IsNullOrEmpty(obj.ToString()) ?
            int.Parse(obj.ToString()!.Trim()) :
            null;

    }
    
}
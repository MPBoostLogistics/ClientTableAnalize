namespace TableAnalizer.Console
{
    public interface IConsoleService
    {
        public Task<DirectoryInfo> GetPrimaryDataFromConsole();
    }
}
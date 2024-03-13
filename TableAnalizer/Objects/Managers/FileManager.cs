using ClientTableAnalizer.Enums;

namespace ClientTableAnalizer.Objects.Managers
{
	public static class FileManager
	{
        static readonly string persDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        static readonly string [] pathKeyNames = Enum.GetNames(typeof(PathKey));

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
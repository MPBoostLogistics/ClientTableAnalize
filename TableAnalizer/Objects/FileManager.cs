

using System;

namespace TableAnalizer.Objects
{
	public enum PathKey
	{
        Documents, MPBoostClients
    }

	public static class FileManager
	{
        static string persDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        static string [] pathKeyNames = Enum.GetNames(typeof(PathKey));

        public static DirectoryInfo? CreateDirectoryInfo(PathKey pathKey)
        {
            return pathKey switch
            {
                PathKey.MPBoostClients => Directory.CreateDirectory(persDirPath + $"/{pathKeyNames[0]}/{pathKeyNames[1]}"),
                 _ => null
            };
        }





        public static DirectoryInfo? GetDirectoryInfo(PathKey pathKey)
		{
            return pathKey switch
            {
                PathKey.Documents => new DirectoryInfo(persDirPath + $"/{pathKeyNames[0]}"),
                PathKey.MPBoostClients => new DirectoryInfo(persDirPath + $"/{pathKeyNames[0]}/{pathKeyNames[1]}"),
                _ => null,
            };
        }
    }
}
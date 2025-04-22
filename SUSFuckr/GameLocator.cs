using System;
using System.IO;

namespace SUSFuckr
{
    public static class GameLocator
    {
        private static readonly string[] CommonSteamPaths =
        {
            Environment.ExpandEnvironmentVariables("%PROGRAMFILES(X86)%/Steam"),
            Environment.ExpandEnvironmentVariables("%PROGRAMFILES%/Steam"),
            Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%/Steam"),
            "D:/Steam",
            "D:/Gry/Steam"
        };

        public static string? TryFindAmongUsPath()
        {
            foreach (var basePath in CommonSteamPaths)
            {
                var path = Path.Combine(basePath, "steamapps", "common", "Among Us");
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe")))
                {
                    return path.Replace("\\\\", "\\").Replace("/", "\\");
                }
            }
            return null;
        }
    }
}
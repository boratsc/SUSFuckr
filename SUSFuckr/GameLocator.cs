using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

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
            "D:/",
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

        public static void CheckAndSetupVanillaMod(List<ModConfiguration> modConfigs)
        {
            // Znajd� istniej�c� konfiguracj� dla "AmongUs" z prawid�ow� �cie�k�
            var existingConfig = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs" &&
                                                                !string.IsNullOrEmpty(x.InstallPath) &&
                                                                File.Exists(Path.Combine(x.InstallPath, "Among Us.exe")));

            if (existingConfig != null)
            {
                // Je�li konfiguracja ju� istnieje i jest prawid�owa, nie r�b nic
                Console.WriteLine("Among Us ju� zainstalowano z poprawn� konfiguracj�.");
                return;
            }

            // Spr�buj znale�� Among Us w znanych lokalizacjach
            string? foundPath = TryFindAmongUsPath();

            // Je�li nie znaleziono, pro� u�ytkownika o wskazanie folderu
            if (foundPath == null)
            {
                MessageBox.Show("Nie znaleziono podstawowej wersji Among Us. Prosz� wskaza� folder r�cznie poprzez \"Przegl�daj\".", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Dodaj poprawn� konfiguracj� moda
            var vanillaMod = new ModConfiguration
            {
                ModName = "AmongUs",
                PngFileName = "Vanilla.png",
                InstallPath = foundPath,
                GitHubRepoOrLink = null,
                ModType = "Vanilla",
                DllInstallPath = null,
                ModVersion = "",
                LastUpdated = null,
                AmongVersion = GetGameVersion(foundPath)
            };

            modConfigs.Add(vanillaMod);
            ConfigManager.SaveConfig(modConfigs);
        }

        private static string GetGameVersion(string path)
        {
            try
            {
                var exePath = Path.Combine(path, "Among Us.exe");
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                return versionInfo.FileVersion ?? "Nieznana";
            }
            catch
            {
                Console.WriteLine($"Nie uda�o si� odczyta� wersji gry.");
                return "Nieznana";
            }
        }
    }
}
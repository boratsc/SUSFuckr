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
            // ZnajdŸ istniej¹c¹ konfiguracjê dla "AmongUs" z prawid³ow¹ œcie¿k¹
            var existingConfig = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs" &&
                                                                !string.IsNullOrEmpty(x.InstallPath) &&
                                                                File.Exists(Path.Combine(x.InstallPath, "Among Us.exe")));

            if (existingConfig != null)
            {
                // Jeœli konfiguracja ju¿ istnieje i jest prawid³owa, nie rób nic
                Console.WriteLine("Among Us ju¿ zainstalowano z poprawn¹ konfiguracj¹.");
                return;
            }

            // Spróbuj znaleŸæ Among Us w znanych lokalizacjach
            string? foundPath = TryFindAmongUsPath();

            // Jeœli nie znaleziono, proœ u¿ytkownika o wskazanie folderu
            if (foundPath == null)
            {
                MessageBox.Show("Nie znaleziono podstawowej wersji Among Us. Proszê wskazaæ folder rêcznie poprzez \"Przegl¹daj\".", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Dodaj poprawn¹ konfiguracjê moda
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
                Console.WriteLine($"Nie uda³o siê odczytaæ wersji gry.");
                return "Nieznana";
            }
        }
    }
}
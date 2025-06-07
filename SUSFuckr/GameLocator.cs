using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Text.Json;




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

        private static readonly string[] CommonEpicPaths =
        {
            Environment.ExpandEnvironmentVariables("%PROGRAMFILES(X86)%/Epic Games"),
            Environment.ExpandEnvironmentVariables("%PROGRAMFILES%/Epic Games"),
            "D:/Epic Games",
            "D:/Gry/Epic Games",
            "D:/Gry/Epic"
        };

        public static string? TryFindAmongUsPath(out string? mode)
        {
            Console.WriteLine("Rozpoczêto wyszukiwanie Among Us.");
            mode = null;

            foreach (var basePath in CommonSteamPaths)
            {
                // Dodano "steamapps\\common" do podstawowej œcie¿ki Steam
                var path = Path.Combine(basePath, "steamapps", "common", "Among Us");
                Console.WriteLine($"Sprawdzam œcie¿kê: {path}");
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe")))
                {
                    Console.WriteLine($"Znaleziono grê w Steam: {path}");
                    mode = "steam";
                    return path.Replace("\\\\", "\\").Replace("/", "\\");
                }
            }

            foreach (var basePath in CommonEpicPaths)
            {
                var path = Path.Combine(basePath, "AmongUs");
                Console.WriteLine($"Sprawdzam œcie¿kê: {path}");
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe")) && Directory.Exists(Path.Combine(path, ".egstore")))
                {
                    Console.WriteLine($"Znaleziono grê w Epic: {path}");
                    mode = "epic";
                    return path.Replace("\\\\", "\\").Replace("/", "\\");
                }
            }

            Console.WriteLine("Nie znaleziono gry Among Us.");
            return null;
        }

        public static void CheckAndSetupVanillaMod(List<ModConfiguration> modConfigs, IConfiguration configuration)
        {
            var existingConfig = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs" &&
                                                                x.Id == 0 &&
                                                                !string.IsNullOrEmpty(x.InstallPath));

            string currentMode = configuration["Configuration:Mode"] ?? "steam";

            if (existingConfig != null)
            {
                Console.WriteLine("Among Us ju¿ zainstalowano z wersj¹ Vanilla dla Epic.");
                return;
            }

            string? foundPath = TryFindAmongUsPath(out string? detectedMode);
            if (foundPath == null)
            {
                MessageBox.Show("Nie znaleziono podstawowej wersji Among Us. Proszê wskazaæ folder rêcznie poprzez \"Wska¿ rêcznie\".", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ustaw tryb w konfiguracji
            if (detectedMode != null)
            {
                configuration["Configuration:Mode"] = detectedMode;
            }

            var vanillaMod = new ModConfiguration
            {
                ModName = "AmongUs",
                PngFileName = "Vanilla.png",
                InstallPath = foundPath,
                GitHubRepoOrLink = string.Empty,
                EpicGitHubRepoOrLink = string.Empty, // Dodaj to pole
                ModType = "Vanilla",
                DllInstallPath = null,
                ModVersion = "",
                LastUpdated = DateTime.Now,
                AmongVersion = GetGameVersion(foundPath),
                Description = $"Detected as {detectedMode}"
            };


            modConfigs.Add(vanillaMod);
            ConfigManager.SaveConfig(modConfigs);

            if (detectedMode != null)
            {
                configuration["Configuration:Mode"] = detectedMode;
                ConfigManager.SaveConfigurationSetting("Mode", detectedMode);
            }
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
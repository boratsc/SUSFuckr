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
            Console.WriteLine("Rozpocz�to wyszukiwanie Among Us.");
            mode = null;

            foreach (var basePath in CommonSteamPaths)
            {
                // Dodano "steamapps\\common" do podstawowej �cie�ki Steam
                var path = Path.Combine(basePath, "steamapps", "common", "Among Us");
                Console.WriteLine($"Sprawdzam �cie�k�: {path}");
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe")))
                {
                    Console.WriteLine($"Znaleziono gr� w Steam: {path}");
                    mode = "steam";
                    return path.Replace("\\\\", "\\").Replace("/", "\\");
                }
            }

            foreach (var basePath in CommonEpicPaths)
            {
                var path = Path.Combine(basePath, "AmongUs");
                Console.WriteLine($"Sprawdzam �cie�k�: {path}");
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe")) && Directory.Exists(Path.Combine(path, ".egstore")))
                {
                    Console.WriteLine($"Znaleziono gr� w Epic: {path}");
                    mode = "epic";
                    return path.Replace("\\\\", "\\").Replace("/", "\\");
                }
            }

            Console.WriteLine("Nie znaleziono gry Among Us.");
            return null;
        }

        public static void CheckAndSetupVanillaMod(List<ModConfiguration> modConfigs, IConfiguration configuration)
        {
            // Znajd� istniej�c� konfiguracj� dla "AmongUs" z poprawn� �cie�k�
            var existingConfig = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs" &&
                                                                !string.IsNullOrEmpty(x.InstallPath) &&
                                                                File.Exists(Path.Combine(x.InstallPath, "Among Us.exe")));
            if (existingConfig != null)
            {
                Console.WriteLine("Among Us ju� zainstalowano z poprawn� konfiguracj�.");
                return;
            }

            // Spr�buj znale�� Among Us w znanych lokalizacjach
            string? foundPath = TryFindAmongUsPath(out string? mode);

            if (foundPath == null)
            {
                MessageBox.Show("Nie znaleziono podstawowej wersji Among Us. Prosz� wskaza� folder r�cznie poprzez \"Przegl�daj\".", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ustaw tryb w konfiguracji
            if (mode != null)
            {
                configuration["Configuration:Mode"] = mode;
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
                AmongVersion = GetGameVersion(foundPath),
                Description = $"Detected as {mode}"
            };
            modConfigs.Add(vanillaMod);
            ConfigManager.SaveConfig(modConfigs);
            if (mode != null)
            {
                configuration["Configuration:Mode"] = mode;
                ConfigManager.SaveConfigurationSetting("Mode", mode);
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
                Console.WriteLine($"Nie uda�o si� odczyta� wersji gry.");
                return "Nieznana";
            }
        }
    }
}
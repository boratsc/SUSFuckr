using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace SUSFuckr
{
    public class EpicVersionManager
    {
        private readonly string legendaryPath;
        private readonly string manifestDirectory;
        private readonly string installDirectory;
        private const string EpicAppId = "963137e4c29d4c79a81323b8fab03a40";
        private readonly string appSettingsFilePath;


        public EpicVersionManager()
        {
            legendaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "legendary.exe");
            manifestDirectory = AppDomain.CurrentDomain.BaseDirectory;
            installDirectory = PathSettings.ModsInstallPath;
            appSettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"); // Inicjalizacja ścieżki w konstruktorze

        }

        public async Task ModifyEpicAsync(ModConfiguration modConfig, ProgressBar progressBar, Label progressLabel)
        {
            if (modConfig == null || string.IsNullOrEmpty(modConfig.GitHubRepoOrLink))
            {
                MessageBox.Show($"Brak adresu URL do pobrania dla moda '{modConfig?.ModName ?? "unknown"}'.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string baseDirectory = installDirectory;
            string tempDirectory = Path.Combine(baseDirectory, "temp");
            Directory.CreateDirectory(tempDirectory);
            string modFile = Path.Combine(tempDirectory, "mod.zip");
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressLabel.Text = "Ściąganie moda...";
            await DownloadFileAsync(modConfig.GitHubRepoOrLink, modFile);

            if (!File.Exists(modFile))
            {
                MessageBox.Show($"Nie udało się pobrać moda z {modFile}.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string gameBasePath = Path.Combine(baseDirectory, modConfig.ModName, "AmongUs");
            if (Directory.Exists(gameBasePath))
            {
                Directory.Delete(gameBasePath, true);
            }
            Directory.CreateDirectory(gameBasePath);
            string tempExtractPath = Path.Combine(tempDirectory, "extractMod");
            Directory.CreateDirectory(tempExtractPath);
            ZipFile.ExtractToDirectory(modFile, tempExtractPath);

            string sourcePath = Directory.Exists(Path.Combine(tempExtractPath, "BepInEx"))
                ? tempExtractPath
                : Directory.GetDirectories(tempExtractPath).FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(sourcePath))
            {
                MessageBox.Show("Nie znaleziono plików do skopiowania.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CopyContent(sourcePath, gameBasePath);
            var existingConfigs = ConfigManager.LoadConfig();
            var existingConfig = existingConfigs.FirstOrDefault(c => c.Id == modConfig.Id);

            if (existingConfig != null)
            {
                existingConfig.InstallPath = gameBasePath;
                existingConfig.LastUpdated = DateTime.Now;
            }
            else
            {
                modConfig.InstallPath = gameBasePath;
                existingConfigs.Add(modConfig);
            }

            ConfigManager.SaveConfig(existingConfigs);
            Directory.Delete(tempDirectory, true);
            progressBar.Visible = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public async Task HandleEpicGameAsync(ModConfiguration modConfig)
        {
            if (modConfig == null || string.IsNullOrEmpty(modConfig.AmongVersion))
            {
                MessageBox.Show("Konfiguracja gry jest nieprawidłowa.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await RunLegendaryCommandAsync("auth --import");

            int lastLaunchId = GetLastLaunchId();
            if (modConfig.Id == lastLaunchId)
            {
                await LaunchGameAsync();
                return;
            }

            if (!File.Exists(legendaryPath))
            {
                await DownloadLegendaryAsync();
            }

            string amongVersionFormatted = modConfig.AmongVersion?.Replace("-", ".") ?? string.Empty;

            if (modConfig.Id == 0)
            {
                await UninstallGameAsync();
                await InstallGameAsync(modConfig, amongVersionFormatted);
                await LaunchGameAsync();
            }
            else
            {
                await DownloadManifestAsync(amongVersionFormatted);
                await UninstallGameAsync();
                await InstallGameAsync(modConfig, amongVersionFormatted);
                await LaunchGameAsync();
            }

            SaveLastLaunchId(modConfig.Id);
        }

        private async Task DownloadManifestAsync(string amongVersionFormatted)
        {
            if (string.IsNullOrWhiteSpace(amongVersionFormatted))
            {
                MessageBox.Show("Niepoprawna wersja Among Us.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string manifestUrl = $"https://github.com/whichtwix/Data/raw/master/epic/manifests/{EpicAppId}_{amongVersionFormatted}.manifest";
            string manifestPath = Path.Combine(manifestDirectory, $"{EpicAppId}_{amongVersionFormatted}.manifest");
            await DownloadFileAsync(manifestUrl, manifestPath);
        }

        public async Task UninstallGameAsync()
        {
            string commandArguments = $"uninstall {EpicAppId} -y";
            await RunLegendaryCommandAsync(commandArguments);
        }

        public async Task InstallGameAsync(ModConfiguration modConfig, string amongVersionFormatted)
        {
            string installDirectory;
            if (modConfig.Id == 0)
            {
                installDirectory = modConfig.InstallPath.Replace("AmongUs", "").TrimEnd(Path.DirectorySeparatorChar);
            }
            else
            {
                installDirectory = Path.Combine(PathSettings.ModsInstallPath, modConfig.ModName);
            }
            Directory.CreateDirectory(installDirectory);
            string commandArguments;

            if (modConfig.Id == 0)
            {
                commandArguments = $"install {EpicAppId} --base-path \"{installDirectory}\" -y";
            }
            else
            {
                string manifestFilePath = Path.Combine(manifestDirectory, $"{EpicAppId}_{amongVersionFormatted}.manifest");
                if (!File.Exists(manifestFilePath))
                {
                    MessageBox.Show($"Nie znaleziono manifestu: {manifestFilePath}.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                commandArguments = $"install {EpicAppId} -y --manifest \"{manifestFilePath}\" --base-path \"{installDirectory}\"";
            }
            await RunLegendaryCommandAsync(commandArguments);
        }

        public async Task LaunchGameAsync()
        {
            string commandArguments = $"launch {EpicAppId} --skip-version-check";
            await RunLegendaryCommandAsync(commandArguments);
        }

        public async Task RunLegendaryCommandAsync(string commandArguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = legendaryPath,
                    Arguments = commandArguments,
                    UseShellExecute = true,
                    CreateNoWindow = false // Możesz pozostawić GUI jeśli aplikacja legendary ma GUI
                };

                using Process? process = Process.Start(psi);
                if (process != null)
                {
                    // Możesz oczekiwać na zakończenie procesu, jeśli jest to wymagane
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas operacji: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DownloadLegendaryAsync()
        {
            string url = "https://github.com/whichtwix/legendary/releases/latest/download/legendary.exe";
            await DownloadFileAsync(url, legendaryPath);
            Console.WriteLine("Legendary downloaded.");
        }

        private async Task DownloadFileAsync(string url, string filePath)
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    MessageBox.Show($"Nie znaleziono zasobu dla URL: {url}.", "Błąd 404", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fs);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Błąd HTTP: {ex.Message} dla URL: {url}.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas pobierania pliku: {ex.Message}.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyContent(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destinationFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destinationFile, true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destinationDir = Path.Combine(destDir, Path.GetFileName(dir));
                DirectoryCopy(dir, destinationDir);
            }
        }

        private void DirectoryCopy(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destinationFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destinationFile, true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destinationDir = Path.Combine(destDir, Path.GetFileName(dir));
                DirectoryCopy(dir, destinationDir);
            }
        }

        private int GetLastLaunchId()
        {
            var json = File.ReadAllText(appSettingsFilePath);
            var jsonObj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
            if (jsonObj != null && jsonObj.ContainsKey("Configuration") && jsonObj["Configuration"].ContainsKey("lastLaunchId"))
            {
                return int.Parse(jsonObj["Configuration"]["lastLaunchId"]?.ToString() ?? "-1");
            }
            return -1;
        }

        private void SaveLastLaunchId(int id)
        {
            var json = File.ReadAllText(appSettingsFilePath);
            var jsonObj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
            if (jsonObj != null && jsonObj.ContainsKey("Configuration"))
            {
                var config = jsonObj["Configuration"];
                config["lastLaunchId"] = id;
                jsonObj["Configuration"] = config;
                var updatedJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(appSettingsFilePath, updatedJson);
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace SUSFuckr
{
    public class Updater
    {
        private readonly IConfiguration configuration;
        private readonly string currentVersion;

        public Updater(string currentVersion, IConfiguration configuration)
        {
            this.currentVersion = currentVersion ?? "0.0.0";
            this.configuration = configuration;
        }

        public async Task CheckAndPromptForUpdateAsync()
        {
            try
            {
                string latestVersion = await GetLatestVersionAsync();
                bool needsUpdaterUpdate = NeedsUpdaterUpdate();
                bool needsCleanup = NeedsCleanup();

                if (string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    DialogResult dialogResult = MessageBox.Show(
                        $"Dostêpna jest nowa wersja aplikacji.\nObecna wersja: {currentVersion}\nNowa wersja: {latestVersion}\nCzy chcesz zaktualizowaæ?",
                        "Aktualizacja aplikacji",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (dialogResult == DialogResult.Yes)
                    {
                        await DownloadAndRunUpdaterAsync(latestVersion);
                    }
                }
                else if (needsUpdaterUpdate || needsCleanup)
                {
                    // Nie ma nowej wersji, ale trzeba posprz¹taæ/aktualizowaæ updatera
                    await UpdateUpdaterIfNeededAsync();
                    CleanupOldFrameworkFilesIfNeeded();
                    UIOutput.Write("Wykonano niezbêdne czynnoœci porz¹dkowe i/lub aktualizacjê updatera. Mo¿esz teraz bezpiecznie korzystaæ z aplikacji.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas sprawdzania wersji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<string> GetLatestVersionAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync("https://susfuckr.boracik.pl/api/susfuckr-current-version");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var versionInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                return versionInfo?["version"] ?? "0.0.0";
            }
        }

        private bool NeedsUpdaterUpdate()
        {
            string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
            if (string.IsNullOrEmpty(appDirPath)) return false;

            string updaterDir = Path.Combine(appDirPath, "updater");
            string depsPath = Path.Combine(updaterDir, "Updater.deps.json");
            return File.Exists(depsPath);
        }

        private bool NeedsCleanup()
        {
            string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
            if (string.IsNullOrEmpty(appDirPath)) return false;

            string runtimeConfigPath = Path.Combine(appDirPath, "SUSFuckr.runtimeconfig.json");
            return File.Exists(runtimeConfigPath);
        }

        private async Task UpdateUpdaterIfNeededAsync()
        {
            string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
            if (string.IsNullOrEmpty(appDirPath)) return;

            string updaterDir = Path.Combine(appDirPath, "updater");
            string depsPath = Path.Combine(updaterDir, "Updater.deps.json");

            if (File.Exists(depsPath))
            {
                string updaterExeUrl = "https://susfuckr.boracik.pl/susfuckr/updater/Updater.exe";
                string tempUpdaterPath = Path.Combine(Path.GetTempPath(), "Updater.exe");

                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine("Wykryto stary updater. Pobieranie nowego Updater.exe...");
                    HttpResponseMessage response = await client.GetAsync(updaterExeUrl);
                    response.EnsureSuccessStatusCode();
                    using (FileStream fs = new FileStream(tempUpdaterPath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                // Usuñ wszystko z katalogu updater
                foreach (var file in Directory.GetFiles(updaterDir))
                {
                    try { File.Delete(file); } catch { }
                }

                // Skopiuj nowego updatera
                string newUpdaterPath = Path.Combine(updaterDir, "updater.exe");
                File.Copy(tempUpdaterPath, newUpdaterPath, true);
                File.Delete(tempUpdaterPath);

                Console.WriteLine("Updater zosta³ zaktualizowany.");
            }
        }

        private void CleanupOldFrameworkFilesIfNeeded()
        {
            string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
            if (string.IsNullOrEmpty(appDirPath)) return;

            string runtimeConfigPath = Path.Combine(appDirPath, "SUSFuckr.runtimeconfig.json");

            if (File.Exists(runtimeConfigPath))
            {
                Console.WriteLine("Wykryto plik SUSFuckr.runtimeconfig.json – sprz¹tanie starych plików...");

                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "susfuckr.exe",
            "appsettings.json",
            "config.json"
        };

                foreach (var file in Directory.GetFiles(appDirPath))
                {
                    string fileName = Path.GetFileName(file);
                    if (!allowed.Contains(fileName))
                    {
                        try
                        {
                            File.Delete(file);
                            Console.WriteLine($"Usuniêto plik: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Nie uda³o siê usun¹æ pliku {fileName}: {ex.Message}");
                        }
                    }
                }

                // Usuñ katalog runtimes jeœli istnieje
                string runtimesDir = Path.Combine(appDirPath, "runtimes");
                if (Directory.Exists(runtimesDir))
                {
                    try
                    {
                        Directory.Delete(runtimesDir, true);
                        Console.WriteLine("Usuniêto katalog: runtimes");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Nie uda³o siê usun¹æ katalogu runtimes: {ex.Message}");
                    }
                }
            }
        }

        private async Task DownloadAndRunUpdaterAsync(string latestVersion)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), "LatestVersion.zip");

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    Console.WriteLine("Pobieranie najnowszej wersji...");
                    HttpResponseMessage response = await client.GetAsync("https://susfuckr.boracik.pl/api/download-latest");
                    response.EnsureSuccessStatusCode();
                    using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                // 1. Aktualizuj updatera jeœli trzeba
                await UpdateUpdaterIfNeededAsync();

                // 2. Sprz¹taj stare pliki jeœli trzeba
                CleanupOldFrameworkFilesIfNeeded();

                // 3. Aktualizuj konfiguracjê
                await UpdateConfigurationBeforeExitAsync();

                string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (string.IsNullOrEmpty(appDirPath))
                {
                    throw new InvalidOperationException("Nie mo¿na okreœliæ katalogu aplikacji.");
                }

                string updaterPath = Path.Combine(appDirPath, "updater", "updater.exe");

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    UseShellExecute = true,
                    Arguments = $"\"{appDirPath}\" \"{tempFilePath}\""
                });

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d podczas pobierania i uruchamiania Updater: {ex.Message}");
                MessageBox.Show($"B³¹d aktualizacji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UpdateConfigurationBeforeExitAsync()
        {
            try
            {
                string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (string.IsNullOrEmpty(appDirPath))
                {
                    throw new InvalidOperationException("Nie mo¿na okreœliæ katalogu aplikacji.");
                }

                var tempFilePath = Path.Combine(appDirPath, "config.temp.json");
                using (HttpClient client = new HttpClient())
                {
                    string? updateServerUrl = configuration["Configuration:UpdateServerUrl"];
                    if (string.IsNullOrEmpty(updateServerUrl))
                    {
                        throw new InvalidOperationException("UpdateServerUrl is null or empty.");
                    }

                    HttpResponseMessage response = await client.GetAsync(updateServerUrl);
                    response.EnsureSuccessStatusCode();
                    using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                ConfigUpdater.CompareAndMergeConfigurations(tempFilePath);
                File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas aktualizacji konfiguracji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}

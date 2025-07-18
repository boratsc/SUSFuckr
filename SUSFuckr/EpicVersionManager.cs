﻿using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Collections.Generic;

namespace SUSFuckr
{
    public class EpicVersionManager
    {
        private readonly string legendaryPath;
        private readonly string manifestDirectory;
        private readonly string installDirectory;
        private const string EpicAppId = "963137e4c29d4c79a81323b8fab03a40";
        private readonly string appSettingsFilePath;
        private readonly string logFilePath;             // epic.log.txt
        private readonly string legendaryLogFilePath;    // legendary.log.txt
        public event Action<string>? LegendaryOutput;
        private readonly object _fileLock = new object();

        public EpicVersionManager()
        {
            string exeDir = Path.GetDirectoryName(Environment.ProcessPath)!;

            legendaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "legendary.exe");
            manifestDirectory = AppDomain.CurrentDomain.BaseDirectory;
            installDirectory = PathSettings.ModsInstallPath;
            appSettingsFilePath = Path.Combine(exeDir, "appsettings.json");

            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "epic.log.txt");
            legendaryLogFilePath = Path.Combine(exeDir, "legendary.log.txt");
        }

        private void LogToFile(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(logFilePath, logEntry);
            }
            catch
            {
                // don't throw on logging failure
            }
        }

        private async Task<string?> CheckInstalledAppsAsync()
        {
            try
            {
                UIOutput.Write("Sprawdzanie zainstalowanych aplikacji (legendary.exe list-installed --json)");

                string tempFile = Path.Combine(Path.GetTempPath(), "tempepic.json");

                var psi = new ProcessStartInfo
                {
                    FileName = legendaryPath,
                    Arguments = "list-installed --json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    UIOutput.Write("Nie udało się uruchomić procesu legendary.exe");
                    return null;
                }

                string stdout = await proc.StandardOutput.ReadToEndAsync();
                string stderr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    UIOutput.Write("Otrzymany JSON jest pusty → brak zainstalowanych aplikacji");
                    // fallback na ścieżkę vanilla
                    var configs = ConfigManager.LoadConfig();
                    var vanilla = configs.FirstOrDefault(c => c.Id == 0);
                    if (vanilla != null && !string.IsNullOrEmpty(vanilla.InstallPath))
                    {
                        UIOutput.Write($"Fallback ścieżki vanilla: {vanilla.InstallPath}");
                        return vanilla.InstallPath;
                    }
                    return null;
                }

                File.WriteAllText(tempFile, stdout);

                List<InstalledApp> apps;
                try
                {
                    apps = JsonSerializer.Deserialize<List<InstalledApp>>(stdout)
                           ?? new List<InstalledApp>();
                }
                catch (JsonException jsonEx)
                {
                    UIOutput.Write($"Błąd parsowania JSON z legendary list-installed: {jsonEx.Message}");
                    return null;
                }

                // wczytaj configy tylko raz
                var configsAll = ConfigManager.LoadConfig();
                var vanillaCfg = configsAll.FirstOrDefault(c => c.Id == 0);

                // 1) wyszukaj wpis
                var entry = apps.FirstOrDefault(a => a.app_name == EpicAppId);
                if (entry == null)
                {
                    UIOutput.Write($"Brak pozycji {EpicAppId} w tempepic.json");
                    // spróbuj naprawić manifest używając ścieżki vanilla
                    if (vanillaCfg != null && !string.IsNullOrEmpty(vanillaCfg.InstallPath))
                    {
                        UIOutput.Write($"Naprawiam brakujący wpis, override ścieżki na: {vanillaCfg.InstallPath}");
                        await RunLegendaryCommandAsync(
                            $"repair {EpicAppId} --override-install-path \"{vanillaCfg.InstallPath}\" -y");
                        return vanillaCfg.InstallPath;
                    }
                    return null;
                }

                // 2) wpis znaleziony
                string foundPath = entry.install_path;
                UIOutput.Write($"Legendary zwraca ścieżkę: {foundPath}");

                // 3) porównaj z config.json (vanilla) i naprawiaj jeżeli różne
                if (vanillaCfg != null &&
                    !string.Equals(vanillaCfg.InstallPath, foundPath, StringComparison.OrdinalIgnoreCase))
                {
                    UIOutput.Write($"Różnica ścieżek: config.json='{vanillaCfg.InstallPath}', legendary='{foundPath}'. Naprawiam...");
                    await RunLegendaryCommandAsync(
                        $"repair {EpicAppId} --override-install-path \"{vanillaCfg.InstallPath}\" -y");
                }

                return foundPath;
            }
            catch (Exception ex)
            {
                UIOutput.Write($"ERROR w CheckInstalledAppsAsync: {ex}");
                return null;
            }
        }



        private class InstalledApp
        {
            public string app_name { get; set; } = string.Empty; // Poprawka: dodano domyślną wartość
            public string install_path { get; set; } = string.Empty; // Poprawka: dodano domyślną wartość
        }

        public async Task ModifyEpicAsync(ModConfiguration modConfig, ProgressBar progressBar, Label progressLabel)
        {
            if (modConfig == null)
            {
                MessageBox.Show("Konfiguracja moda jest nieprawidłowa.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Wybierz odpowiedni link w zależności od dostępności EpicGitHubRepoOrLink
            string downloadUrl;
            bool usingFallback = false;

            if (!string.IsNullOrEmpty(modConfig.EpicGitHubRepoOrLink))
            {
                downloadUrl = modConfig.EpicGitHubRepoOrLink;
                UIOutput.Write($"Używam dedykowanego linku Epic dla moda '{modConfig.ModName}': {downloadUrl}");
            }
            else if (!string.IsNullOrEmpty(modConfig.GitHubRepoOrLink))
            {
                downloadUrl = modConfig.GitHubRepoOrLink;
                usingFallback = true;
                UIOutput.Write($"FALLBACK: Używam standardowego linku Steam dla moda '{modConfig.ModName}' w wersji Epic: {downloadUrl}");
            }
            else
            {
                UIOutput.Write($"ERROR: Brak jakiegokolwiek adresu URL do pobrania dla moda '{modConfig.ModName}'");
                MessageBox.Show($"Brak adresu URL do pobrania dla moda '{modConfig.ModName}' w wersji Epic.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Opcjonalnie: pokaż ostrzeżenie użytkownikowi o fallback
            if (usingFallback)
            {
                var result = MessageBox.Show(
                    $"Mod '{modConfig.ModName}' nie ma dedykowanej wersji Epic (x64). " +
                    $"Zostanie użyta wersja Steam (x86), która może nie działać poprawnie.\n\n" +
                    $"Czy chcesz kontynuować?",
                    "Ostrzeżenie - Brak wersji Epic",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    UIOutput.Write($"Użytkownik anulował instalację fallback dla moda '{modConfig.ModName}'");
                    return;
                }
            }

            string baseDirectory = installDirectory;
            string tempDirectory = Path.Combine(baseDirectory, "temp");
            Directory.CreateDirectory(tempDirectory);
            string modFile = Path.Combine(tempDirectory, "mod.zip");
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressLabel.Text = "Ściąganie moda...";

            UIOutput.Write($"Rozpoczynam pobieranie moda '{modConfig.ModName}' z: {downloadUrl}");
            await DownloadFileAsync(downloadUrl, modFile);

            if (!File.Exists(modFile))
            {
                UIOutput.Write($"ERROR: Nie udało się pobrać moda z {downloadUrl}");
                MessageBox.Show($"Nie udało się pobrać moda z {downloadUrl}.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UIOutput.Write($"Pomyślnie pobrano mod do: {modFile}");

            // Reszta kodu pozostaje bez zmian...
            string gameBasePath = Path.Combine(baseDirectory, modConfig.ModName, "AmongUs");
            if (Directory.Exists(gameBasePath))
            {
                UIOutput.Write($"Usuwam istniejący katalog: {gameBasePath}");
                Directory.Delete(gameBasePath, true);
            }
            Directory.CreateDirectory(gameBasePath);
            string tempExtractPath = Path.Combine(tempDirectory, "extractMod");
            Directory.CreateDirectory(tempExtractPath);

            try
            {
                UIOutput.Write($"Rozpakowuję archiwum moda: {modFile} do {tempExtractPath}");
                ZipFile.ExtractToDirectory(modFile, tempExtractPath, overwriteFiles: true);
                UIOutput.Write("Pomyślnie rozpakowano archiwum moda");
            }
            catch (Exception ex)
            {
                UIOutput.Write($"ERROR podczas rozpakowywania: {ex.Message}");
                MessageBox.Show($"Błąd podczas rozpakowywania archiwum: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sourcePath = Directory.Exists(Path.Combine(tempExtractPath, "BepInEx"))
                ? tempExtractPath
                : Directory.GetDirectories(tempExtractPath).FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(sourcePath))
            {
                UIOutput.Write("ERROR: Nie znaleziono plików do skopiowania");
                MessageBox.Show("Nie znaleziono plików do skopiowania.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UIOutput.Write($"Kopiuję pliki z {sourcePath} do {gameBasePath}");
            CopyContent(sourcePath, gameBasePath);

            var existingConfigs = ConfigManager.LoadConfig();
            var existingConfig = existingConfigs.FirstOrDefault(c => c.Id == modConfig.Id);

            if (existingConfig != null)
            {
                existingConfig.InstallPath = gameBasePath;
                existingConfig.LastUpdated = DateTime.Now;
                UIOutput.Write($"Zaktualizowano konfigurację dla istniejącego moda: {modConfig.ModName}");
            }
            else
            {
                modConfig.InstallPath = gameBasePath;
                existingConfigs.Add(modConfig);
                UIOutput.Write($"Dodano nową konfigurację dla moda: {modConfig.ModName}");
            }

            ConfigManager.SaveConfig(existingConfigs);
            Directory.Delete(tempDirectory, true);
            progressBar.Visible = false;

            UIOutput.Write($"SUCCESS: Instalacja moda '{modConfig.ModName}' zakończona pomyślnie");
            if (usingFallback)
            {
                MessageBox.Show(
                    $"Mod '{modConfig.ModName}' został zainstalowany używając wersji Steam. " +
                    "Jeśli wystąpią problemy, sprawdź czy dostępna jest dedykowana wersja Epic.",
                    "Instalacja zakończona (Fallback)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

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

            if (!File.Exists(legendaryPath))
            {
                await DownloadLegendaryAsync();
            }

            await RunLegendaryCommandAsync("auth --import");

            string installDirectory;
            int lastLaunchId = GetLastLaunchId();
            if (modConfig.Id == lastLaunchId)
            {
                installDirectory = modConfig.InstallPath;
                await RunLegendaryCommandAsync($"import 963137e4c29d4c79a81323b8fab03a40 \"{installDirectory}\" -y");
                await LaunchGameAsync();
                return;
            }


            
            if (modConfig.Id == 0)
            {
                installDirectory = modConfig.InstallPath.Replace("AmongUs", "").TrimEnd(Path.DirectorySeparatorChar);
            }
            else
            {
                installDirectory = Path.Combine(PathSettings.ModsInstallPath, modConfig.ModName);
            }
            await RunLegendaryCommandAsync("uninstall 963137e4c29d4c79a81323b8fab03a40 --keep-files -y");
            await RunLegendaryCommandAsync($"import 963137e4c29d4c79a81323b8fab03a40 \"{installDirectory}\" -y");

            // najpierw spróbuj sprawdzić / naprawić istniejącą instalację
            string? foundPath = await CheckInstalledAppsAsync();

            // jeżeli repair nie zadziałał (foundPath == null) → wymuś install
            if (string.IsNullOrEmpty(foundPath))
            {
                // oblicz base-path i ewentualny manifest
                string basePath = modConfig.InstallPath;
                string manifestFile = Path.Combine(
                    manifestDirectory,
                    $"{EpicAppId}_{modConfig.AmongVersion.Replace("-", ".")}.manifest");

                string installArgs;
                if (modConfig.Id == 0)
                {
                    // vanilla instalacja (bez manifestu)
                    installArgs =
                      $"install {EpicAppId} --base-path \"{basePath}\" -y";
                }
                else
                {
                    // moda instalacja (z manifestem)
                    installArgs =
                      $"install {EpicAppId} -y " +
                      $"--manifest \"{manifestFile}\" " +
                      $"--base-path \"{basePath}\"";
                }

                LogToFile($"Repair nie powiódł się → fallback install: {installArgs}");
                await RunLegendaryCommandAsync(installArgs);

                // po installu przyjmujemy, że basePath jest poprawny
                foundPath = basePath;
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
                UIOutput.Write($"Launching legendary.exe {commandArguments}");
                var psi = new ProcessStartInfo
                {
                    FileName = legendaryPath,
                    Arguments = commandArguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };

                process.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    LegendaryOutput?.Invoke(e.Data);
                    lock (_fileLock)
                        File.AppendAllText(legendaryLogFilePath, e.Data + Environment.NewLine);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    var line = "[ERR] " + e.Data;
                    LegendaryOutput?.Invoke(line);
                    lock (_fileLock)
                        File.AppendAllText(legendaryLogFilePath, line + Environment.NewLine);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                var exitMsg = $"Process exited with code {process.ExitCode}";
                LegendaryOutput?.Invoke(exitMsg);
                UIOutput.Write(exitMsg);
            }
            catch (Exception ex)
            {
                UIOutput.Write($"ERROR running legendary.exe {commandArguments}: {ex}");
                MessageBox.Show(
                    $"Wystąpił błąd podczas uruchamiania legendary.exe: {ex.Message}",
                    "Błąd",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
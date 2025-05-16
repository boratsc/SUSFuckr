using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace SUSFuckr
{
    public class ModManager
    {
        private readonly string baseUrl;

        public ModManager(IConfiguration configuration)
        {
            baseUrl = configuration["Configuration:BaseUrl"] ?? throw new ArgumentNullException(nameof(configuration), "BaseUrl is not configured.");
        }

        public async Task ModifyAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs, ProgressBar progressBar, Label progressLabel, string mode)
        {
            if (modConfig.ModType == "full")
            {
                try
                {
                    if (mode == "steam")
                    {
                        await ModifySteamAsync(modConfig, modConfigs, progressBar, progressLabel);
                    }
                    else if (mode == "epic")
                    {
                        var epicManager = new EpicVersionManager();
                        await epicManager.ModifyEpicAsync(modConfig, progressBar, progressLabel);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wyst¹pi³ b³¹d podczas Instalacji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        private async Task ModifySteamAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs, ProgressBar progressBar, Label progressLabel)
        {
            // Nowa logika: przechowuj vanilla .zip w Among Us - Vanilla i u¿ywaj go wielokrotnie
            string baseDirectory = PathSettings.ModsInstallPath;
            Directory.CreateDirectory(baseDirectory);

            // 1. Przygotuj œcie¿ki do vanilla
            string vanillaDir = Path.Combine(baseDirectory, "Among Us - Vanilla");
            Directory.CreateDirectory(vanillaDir);

            string vanillaZipName = $"{modConfig.AmongVersion.Replace("-", "").Replace(".", "")}.zip";
            string vanillaZipPath = Path.Combine(vanillaDir, vanillaZipName);
            string fileUrlAmongUs = baseUrl + vanillaZipName;

            // 2. Pobierz vanilla ZIP jeœli nie istnieje
            if (!File.Exists(vanillaZipPath))
            {
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressLabel.Visible = true;
                progressLabel.Text = "Plik 1 z 2 - 0% pobierania (gra)...";
                await DownloadFileAsync(fileUrlAmongUs, vanillaZipPath, progressBar, progressLabel, "1");
            }

            // 3. Pobierz moda
            string tempDir = Path.Combine(baseDirectory, "temp");
            Directory.CreateDirectory(tempDir);
            string modFile = Path.Combine(tempDir, "mod.zip");
            if (!string.IsNullOrEmpty(modConfig.GitHubRepoOrLink))
            {
                progressBar.Visible = true;
                progressLabel.Text = "Plik 2 z 2 - 0% pobierania (mod)...";
                await DownloadFileAsync(modConfig.GitHubRepoOrLink, modFile, progressBar, progressLabel, "2");
            }
            else
            {
                MessageBox.Show($"Brak adresu URL do pobrania dla moda '{modConfig.ModName}'.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            progressBar.Visible = false;

            // 4. Przygotuj katalog moda
            string modFolderPath = Path.Combine(baseDirectory, modConfig.ModName);
            if (Directory.Exists(modFolderPath))
            {
                Directory.Delete(modFolderPath, true);
            }
            Directory.CreateDirectory(modFolderPath);

            // 5. Rozpakuj vanilla ZIP do katalogu moda
            ZipFile.ExtractToDirectory(vanillaZipPath, modFolderPath);

            // 6. Rozpakuj moda do temp
            string tempExtractPath = Path.Combine(tempDir, "extractMod");
            if (Directory.Exists(tempExtractPath))
                Directory.Delete(tempExtractPath, true);
            Directory.CreateDirectory(tempExtractPath);
            ZipFile.ExtractToDirectory(modFile, tempExtractPath);

            // 7. Skopiuj pliki moda do katalogu moda
            string? sourcePath;
            if (Directory.Exists(Path.Combine(tempExtractPath, "BepInEx")))
            {
                sourcePath = tempExtractPath;
            }
            else
            {
                sourcePath = Directory.GetDirectories(tempExtractPath).FirstOrDefault();
                if (sourcePath == null)
                {
                    MessageBox.Show("Nie znaleziono plików do skopiowania.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            CopyContent(sourcePath, modFolderPath);

            // 8. Zapisz konfiguracjê i posprz¹taj temp
            modConfig.InstallPath = modFolderPath;
            ConfigManager.SaveConfig(modConfigs);
            Directory.Delete(tempDir, true);

            MessageBox.Show($"Instalacja zakoñczona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void CopyContent(string sourceDir, string destDir)
        {
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

        private static void DirectoryCopy(string sourceDir, string destDir)
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

        public async Task ModifyDllAsync(ModConfiguration modConfig, List<ModConfiguration> installedFullMods, ProgressBar progressBar, Label progressLabel)
        {
            try
            {
                string dllUrl = modConfig.GitHubRepoOrLink ?? throw new InvalidOperationException("URL DLL jest wymagany.");
                string baseDirectory = PathSettings.ModsInstallPath;
                Directory.CreateDirectory(baseDirectory);
                string fileName = Path.GetFileName(new Uri(dllUrl).AbsolutePath);
                string tempDllFile = Path.Combine(baseDirectory, "temp", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(tempDllFile)!);

                // Pobierz plik DLL
                progressBar.Visible = true; // Poka¿ pasek postêpu na pocz¹tku pobierania
                progressBar.Style = ProgressBarStyle.Continuous;
                progressLabel.Text = "Pobieranie DLL - 0% pobierania...";
                await DownloadFileAsync(dllUrl, tempDllFile, progressBar, progressLabel, "2");
                progressBar.Visible = false; // Ukryj pasek postêpu po zakoñczeniu pobierania

                foreach (var fullMod in installedFullMods)
                {
                    string targetDir = Path.Combine(fullMod.InstallPath, modConfig.DllInstallPath ?? string.Empty);
                    Directory.CreateDirectory(targetDir);
                    string targetFile = Path.Combine(targetDir, fileName);  // Zachowaj nazwê pliku
                    File.Copy(tempDllFile, targetFile, true);
                }

                Directory.Delete(Path.Combine(baseDirectory, "temp"), true);
                MessageBox.Show($"Instalacja DLL zakoñczona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wyst¹pi³ b³¹d podczas instalacji DLL: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DownloadFileAsync(string url, string targetPath, ProgressBar progressBar, Label progressLabel, string fileNumber)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? 1;
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[81920];
                            long totalRead = 0L;
                            int bytesRead;
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;
                                var percentDone = (int)((totalRead * 100) / totalBytes);
                                progressBar.Invoke(new Action(() =>
                                {
                                    progressBar.Value = percentDone;
                                    progressLabel.Text = $"Plik {fileNumber} z 2 - {percentDone}% pobierania";
                                }));
                            }
                        }
                    }
                }
            }
        }
    }
}
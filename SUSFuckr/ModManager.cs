using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;  

namespace SUSFuckr
{
    public class ModManager
    {
        private readonly string baseUrl = "http://polatany.ipv64.net:8087/";

        public async Task ModifyAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs, ProgressBar progressBar)
        {
            if (modConfig.ModType == "full")
            {
                try
                {
                    string fileName = $"{modConfig.AmongVersion.Replace("-", "").Replace(".", "")}.zip";
                    string fileUrlAmongUs = baseUrl + fileName;
                    string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody");
                    Directory.CreateDirectory(baseDirectory);

                    string tempFileAmongUs = Path.Combine(baseDirectory, "temp", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(tempFileAmongUs)!);

                    progressBar.Visible = true; // Poka¿ pasek postêpu dla pobierania pliku gry
                    progressBar.Style = ProgressBarStyle.Continuous;
                    await DownloadFileAsync(fileUrlAmongUs, tempFileAmongUs, progressBar);

                    string modFile = Path.Combine(baseDirectory, "temp", "mod.zip");

                    if (!string.IsNullOrEmpty(modConfig.GitHubRepoOrLink))
                    {
                        progressBar.Visible = true; // Poka¿ pasek postêpu dla pobierania moda
                        await DownloadFileAsync(modConfig.GitHubRepoOrLink, modFile, progressBar);
                    }
                    else
                    {
                        MessageBox.Show($"Brak adresu URL do pobrania dla moda '{modConfig.ModName}'.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return; // lub inna logika obs³ugi b³êdu
                    }

                    progressBar.Visible = false; // Ukryj pasek postêpu po zakoñczeniu pobierania

                    string modFolderPath = Path.Combine(baseDirectory, modConfig.ModName);

                    if (Directory.Exists(modFolderPath))
                    {
                        Directory.Delete(modFolderPath, true);
                    }
                    Directory.CreateDirectory(modFolderPath);

                    ZipFile.ExtractToDirectory(tempFileAmongUs, modFolderPath);

                    string tempExtractPath = Path.Combine(baseDirectory, "temp", "extractMod");
                    Directory.CreateDirectory(tempExtractPath);
                    ZipFile.ExtractToDirectory(modFile, tempExtractPath);

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

                    modConfig.InstallPath = modFolderPath;
                    ConfigManager.SaveConfig(modConfigs);

                    Directory.Delete(Path.Combine(baseDirectory, "temp"), true);

                    MessageBox.Show($"Instalacja zakoñczona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wyst¹pi³ b³¹d podczas Instalacji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
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


        public async Task ModifyDllAsync(ModConfiguration modConfig, List<ModConfiguration> installedFullMods, ProgressBar progressBar)
        {
            try
            {
                string dllUrl = modConfig.GitHubRepoOrLink ?? throw new InvalidOperationException("URL DLL jest wymagany.");
                string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody");
                Directory.CreateDirectory(baseDirectory);

                // Uzyskaj nazwê pliku z URL-a
                string fileName = Path.GetFileName(new Uri(dllUrl).AbsolutePath);
                string tempDllFile = Path.Combine(baseDirectory, "temp", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(tempDllFile)!);

                // Pobierz plik DLL
                await DownloadFileAsync(dllUrl, tempDllFile, progressBar);

                foreach (var fullMod in installedFullMods)
                {
                    string targetDir = Path.Combine(fullMod.InstallPath, modConfig.DllInstallPath ?? string.Empty);
                    Directory.CreateDirectory(targetDir);
                    string targetFile = Path.Combine(targetDir, fileName);  // Zachowaj nazwê pliku
                    File.Copy(tempDllFile, targetFile, true);
                }

                // Czyszczenie katalogu temp
                Directory.Delete(Path.Combine(baseDirectory, "temp"), true);

                MessageBox.Show($"Instalacja DLL zakoñczona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wyst¹pi³ b³¹d podczas instalacji DLL: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DownloadFileAsync(string url, string targetPath, ProgressBar progressBar)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 1; // Za³ó¿ d³ugoœæ dla procenta
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[81920]; // Iloœæ przekazywanych danych na raz (80 KB)
                            var totalRead = 0L; // Rozpocznij od 0
                            int bytesRead;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                // Oblicz procent za³adowanych danych i aktualizuj pasek postêpu
                                var percentDone = (int)((totalRead * 100) / totalBytes);
                                progressBar.Invoke(new Action(() => progressBar.Value = percentDone));
                            }
                        }
                    }
                }
            }
        }
    }
}
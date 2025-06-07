using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace SUSFuckr
{
    public class ModManager
    {
        private readonly string baseUrl;
        private readonly string downloadToken;
        private readonly string zipPassword;

        public ModManager(IConfiguration configuration)
        {
            baseUrl = configuration["Configuration:BaseUrl"] ?? throw new ArgumentNullException(nameof(configuration), "BaseUrl is not configured.");
            downloadToken = SecretProvider.GetDownloadToken();
            zipPassword = SecretProvider.Get7zPassword();
        }

        public async Task ModifyAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs, ProgressBar progressBar, Label progressLabel, string mode)
        {
            if (modConfig.ModType == "full")
            {
                try
                {
                    UIOutput.Write($"[START] Instalacja moda '{modConfig.ModName}' (tryb: {mode})");
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
                    UIOutput.Write($"[ERROR] Wyst�pi� b��d podczas instalacji: {ex}");
                    MessageBox.Show($"Wyst�pi� b��d podczas Instalacji: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        private async Task ModifySteamAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs, ProgressBar progressBar, Label progressLabel)
        {
            string baseDirectory = PathSettings.ModsInstallPath;
            Directory.CreateDirectory(baseDirectory);

            string vanillaDir = Path.Combine(baseDirectory, "Among Us - Vanilla");
            Directory.CreateDirectory(vanillaDir);

            // Nazwa pliku nie jest ju� kodowana w base64, u�ywamy jej bezpo�rednio
            string vanilla7zName = $"{modConfig.AmongVersion.Replace("-", "").Replace(".", "")}";
            string vanilla7zPath = Path.Combine(vanillaDir, vanilla7zName + ".7z");
            string fileUrlAmongUs = $"{baseUrl}api/susfuckr-download-version?version={vanilla7zName}";

            // Deklaracje zmiennych kt�re b�d� u�ywane po p�tli
            string tempDir = Path.Combine(baseDirectory, "temp");
            string modFolderPath = Path.Combine(baseDirectory, modConfig.ModName);
            string modFile = Path.Combine(tempDir, "mod.zip");

            // 2. Pobierz vanilla 7z je�li nie istnieje
            bool needsDownload = !File.Exists(vanilla7zPath);

            while (true) // P�tla dla ponownego pobierania w przypadku b��d�w
            {
                if (needsDownload)
                {
                    UIOutput.Write($"[INFO] Pobieram vanilla: {fileUrlAmongUs}");
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressLabel.Visible = true;
                    progressLabel.Text = "Plik 1 z 2 - 0% pobierania (gra)...";

                    bool downloaded = false;
                    do
                    {
                        downloaded = await DownloadFileWithMd5CheckAsync(fileUrlAmongUs, vanilla7zPath, progressBar, progressLabel, "1");
                        if (!downloaded)
                        {
                            var retry = MessageBox.Show(
                                "Wyst�pi� b��d podczas pobierania pliku vanilla lub suma kontrolna jest nieprawid�owa. Czy chcesz spr�bowa� ponownie?",
                                "B��d pobierania",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (retry == DialogResult.Yes)
                            {
                                if (File.Exists(vanilla7zPath))
                                    File.Delete(vanilla7zPath);
                            }
                            else
                            {
                                if (File.Exists(vanilla7zPath))
                                    File.Delete(vanilla7zPath);
                                return;
                            }
                        }
                    } while (!downloaded);

                    UIOutput.Write($"[INFO] Pobrano vanilla do: {vanilla7zPath}");
                }
                else
                {
                    UIOutput.Write($"[INFO] Plik vanilla ju� istnieje: {vanilla7zPath}");
                }

                // Sprawd� rozmiar pliku
                if (!File.Exists(vanilla7zPath) || new FileInfo(vanilla7zPath).Length < 1000)
                {
                    UIOutput.Write($"[ERROR] Pobrany plik vanilla jest nieprawid�owy lub pusty. Sprawd� token i wersj�.");
                    MessageBox.Show("Pobrany plik vanilla jest nieprawid�owy lub pusty. Sprawd� token i wersj�.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 3. Pobierz moda
                Directory.CreateDirectory(tempDir);
                if (!string.IsNullOrEmpty(modConfig.GitHubRepoOrLink))
                {
                    UIOutput.Write($"[INFO] Pobieram moda: {modConfig.GitHubRepoOrLink}");
                    progressBar.Visible = true;
                    progressLabel.Text = "Plik 2 z 2 - 0% pobierania (mod)...";
                    await DownloadFileAsync(modConfig.GitHubRepoOrLink, modFile, progressBar, progressLabel, "2");
                    UIOutput.Write($"[INFO] Pobrano moda do: {modFile}");
                }
                else
                {
                    UIOutput.Write($"[ERROR] Brak adresu URL do pobrania dla moda '{modConfig.ModName}'.");
                    MessageBox.Show($"Brak adresu URL do pobrania dla moda '{modConfig.ModName}'.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                progressBar.Visible = false;

                // 4. Przygotuj katalog moda
                if (Directory.Exists(modFolderPath))
                {
                    UIOutput.Write($"[INFO] Usuwam istniej�cy katalog moda: {modFolderPath}");
                    Directory.Delete(modFolderPath, true);
                }
                Directory.CreateDirectory(modFolderPath);

                // 5. Rozpakuj vanilla 7z do katalogu moda
                try
                {
                    UIOutput.Write($"[INFO] Rozpakowuj� vanilla 7z: {vanilla7zPath} do {modFolderPath}");
                    await Task.Run(() => Extract7zWithPassword(vanilla7zPath, modFolderPath, zipPassword));
                    UIOutput.Write($"[INFO] Rozpakowano vanilla.");

                    // Je�li rozpakowywanie si� uda�o, wychodzimy z p�tli
                    break;
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[ERROR] B��d podczas rozpakowywania archiwum: {ex}");

                    // Usu� uszkodzony plik
                    if (File.Exists(vanilla7zPath))
                    {
                        try
                        {
                            File.Delete(vanilla7zPath);
                            UIOutput.Write($"[INFO] Usuni�to uszkodzony plik: {vanilla7zPath}");
                        }
                        catch (Exception deleteEx)
                        {
                            UIOutput.Write($"[WARNING] Nie uda�o si� usun�� uszkodzonego pliku: {deleteEx.Message}");
                        }
                    }

                    // Zapytaj u�ytkownika czy chce pobra� ponownie
                    var retryResult = MessageBox.Show(
                        $"B��d podczas rozpakowywania archiwum vanilla:\n{ex.Message}\n\nPlik mo�e by� uszkodzony. Czy chcesz pobra� go ponownie?",
                        "B��d rozpakowywania",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (retryResult == DialogResult.Yes)
                    {
                        needsDownload = true; // Oznacz �e trzeba pobra� ponownie
                        continue; // Kontynuuj p�tl� - pobierz i spr�buj ponownie
                    }
                    else
                    {
                        // U�ytkownik nie chce ponownie pobiera� - zako�cz
                        return;
                    }
                }
            }

            // 6. Rozpakuj moda do temp
            string tempExtractPath = Path.Combine(tempDir, "extractMod");

            // Dodaj bardziej agresywne czyszczenie katalogu
            await SafeDeleteDirectory(tempExtractPath);
            Directory.CreateDirectory(tempExtractPath);

            try
            {
                UIOutput.Write($"[INFO] Rozpakowuj� archiwum moda: {modFile} do {tempExtractPath}");

                // U�yj ExtractToDirectory z nadpisywaniem plik�w
                ZipFile.ExtractToDirectory(modFile, tempExtractPath, overwriteFiles: true);

                UIOutput.Write($"[INFO] Rozpakowano archiwum moda.");
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] B��d podczas rozpakowywania archiwum moda: {ex}");
                MessageBox.Show($"B��d podczas rozpakowywania archiwum moda: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 7. Skopiuj pliki moda do katalogu moda
            string sourcePath; // Zmieniam na non-nullable
            if (Directory.Exists(Path.Combine(tempExtractPath, "BepInEx")))
            {
                sourcePath = tempExtractPath;
            }
            else
            {
                var tempSourcePath = Directory.GetDirectories(tempExtractPath).FirstOrDefault();
                if (tempSourcePath == null)
                {
                    UIOutput.Write($"[ERROR] Nie znaleziono plik�w do skopiowania.");
                    MessageBox.Show("Nie znaleziono plik�w do skopiowania.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                sourcePath = tempSourcePath;
            }

            UIOutput.Write($"[INFO] Kopiuj� pliki moda z {sourcePath} do {modFolderPath}");
            CopyContent(sourcePath, modFolderPath);

            // 8. Zapisz konfiguracj� i posprz�taj temp
            modConfig.InstallPath = modFolderPath;
            ConfigManager.SaveConfig(modConfigs);
            Directory.Delete(tempDir, true);

            UIOutput.Write($"[SUCCESS] Instalacja zako�czona sukcesem dla moda: {modConfig.ModName}");
            MessageBox.Show($"Instalacja zako�czona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                UIOutput.Write($"[INFO] Pobieram DLL: {dllUrl}");
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Continuous;
                progressLabel.Text = "Pobieranie DLL - 0% pobierania...";
                await DownloadFileAsync(dllUrl, tempDllFile, progressBar, progressLabel, "2");
                progressBar.Visible = false;
                UIOutput.Write($"[INFO] Pobrano DLL do: {tempDllFile}");

                foreach (var fullMod in installedFullMods)
                {
                    string targetDir = Path.Combine(fullMod.InstallPath, modConfig.DllInstallPath ?? string.Empty);
                    Directory.CreateDirectory(targetDir);
                    string targetFile = Path.Combine(targetDir, fileName);
                    File.Copy(tempDllFile, targetFile, true);
                    UIOutput.Write($"[INFO] Skopiowano DLL do: {targetFile}");
                }

                Directory.Delete(Path.Combine(baseDirectory, "temp"), true);
                UIOutput.Write($"[SUCCESS] Instalacja DLL zako�czona sukcesem dla moda: {modConfig.ModName}");
                MessageBox.Show($"Instalacja DLL zako�czona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] Wyst�pi� b��d podczas instalacji DLL: {ex}");
                MessageBox.Show($"Wyst�pi� b��d podczas instalacji DLL: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Extract7zWithPassword(string archivePath, string extractPath, string password)
        {
            try
            {
                UIOutput.Write($"[INFO] Rozpakowuj� archiwum 7z: {archivePath} do {extractPath} (z has�em)");

                // �cie�ka do 7z.exe w katalogu aplikacji
                string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (string.IsNullOrEmpty(appDirPath))
                {
                    throw new InvalidOperationException("Nie mo�na okre�li� katalogu aplikacji.");
                }

                string sevenZipPath = Path.Combine(appDirPath, "tools", "7z.exe");

                Directory.CreateDirectory(extractPath);

                if (!File.Exists(sevenZipPath))
                {
                    UIOutput.Write($"[ERROR] Nie znaleziono 7z.exe w katalogu: {Path.Combine(appDirPath, "tools")}");
                    throw new FileNotFoundException($"Nie znaleziono 7z.exe: {sevenZipPath}");
                }

                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = sevenZipPath;
                    process.StartInfo.Arguments = $"x \"{archivePath}\" -o\"{extractPath}\" -p{password} -y";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    UIOutput.Write($"[INFO] Uruchamiam 7z.exe do rozpakowania: {archivePath}");
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"B��d podczas rozpakowywania archiwum. Kod wyj�cia: {process.ExitCode}. B��d: {error}");
                    }

                    UIOutput.Write($"[INFO] Archiwum rozpakowane pomy�lnie.");
                }
            }
            catch (Exception ex)
            {
                string safeErrorMessage = ex.Message.Replace(password, "***HIDDEN***");
                UIOutput.Write($"[ERROR] B��d podczas rozpakowywania archiwum: {safeErrorMessage}");
                throw new Exception(safeErrorMessage, ex.InnerException);
            }
        }



        private async Task SafeDeleteDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            int maxRetries = 3;
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                try
                {
                    // Spr�buj usun�� katalog normalnie
                    Directory.Delete(directoryPath, true);
                    UIOutput.Write($"[INFO] Pomy�lnie usuni�to katalog: {directoryPath}");
                    return;
                }
                catch (IOException ex) when (currentRetry < maxRetries - 1)
                {
                    UIOutput.Write($"[WARNING] Pr�ba {currentRetry + 1} usuni�cia katalogu nieudana: {ex.Message}");
                    currentRetry++;

                    // Spr�buj zwolni� pliki poprzez GC
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    // Odczekaj chwil�
                    await Task.Delay(1000);

                    // Spr�buj usun�� pliki jeden po jednym
                    try
                    {
                        await ForceDeleteDirectory(directoryPath);
                        return;
                    }
                    catch (Exception innerEx)
                    {
                        UIOutput.Write($"[WARNING] Nie uda�o si� wymusi� usuni�cia: {innerEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[ERROR] B��d podczas usuwania katalogu: {ex.Message}");
                    throw;
                }
            }

            throw new IOException($"Nie uda�o si� usun�� katalogu {directoryPath} po {maxRetries} pr�bach.");
        }

        private async Task ForceDeleteDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            // Usu� atrybuty tylko do odczytu z wszystkich plik�w
            foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[WARNING] Nie uda�o si� usun�� pliku {file}: {ex.Message}");
                }
            }

            // Usu� katalogi
            foreach (string dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    await ForceDeleteDirectory(dir);
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[WARNING] Nie uda�o si� usun�� katalogu {dir}: {ex.Message}");
                }
            }

            try
            {
                Directory.Delete(directoryPath, false);
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[WARNING] Nie uda�o si� usun�� g��wnego katalogu {directoryPath}: {ex.Message}");
            }
        }


        // Metoda pomocnicza u�ywaj�ca oryginalnej biblioteki SevenZipExtractor
        private void ExtractWithSevenZipExtractor(string archivePath, string extractPath, string password)
        {
            UIOutput.Write("[INFO] Pr�ba u�ycia biblioteki SevenZipExtractor");
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.dll");

            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException($"Nie znaleziono biblioteki 7z.dll w katalogu: {dllPath}");
            }

            // Nie wy�wietlaj has�a w logach!
            UIOutput.Write($"[INFO] Pr�ba rozpakowania archiwum przy u�yciu cmd.exe i 7z.exe");

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"7z.exe\" x \"{archivePath}\" -o\"{extractPath}\" -p{password} -y",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process == null)
                    throw new Exception("Nie uda�o si� uruchomi� procesu 7z.exe");

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Nie uda�o si� rozpakowa� archiwum. Kod b��du: {process.ExitCode}");
                }
            }

        }


        private async Task<bool> DownloadFileWithMd5CheckAsync(
    string url,
    string targetPath,
    ProgressBar progressBar,
    Label progressLabel,
    string fileNumber)
        {
            UIOutput.Write($"[INFO] Rozpoczynam pobieranie pliku {fileNumber} do: {Path.GetFileName(targetPath)}");
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.Add("Authorization", downloadToken);

                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        UIOutput.Write($"[ERROR] B��d pobierania pliku: {response.StatusCode}\n{errorContent}");
                        MessageBox.Show($"B��d pobierania pliku: {response.StatusCode}\n{errorContent}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // Pobierz sum� MD5 z nag��wka
                    string? md5FromHeader = null;
                    if (response.Headers.TryGetValues("X-File-MD5", out var values))
                        md5FromHeader = values.FirstOrDefault();

                    var totalBytes = response.Content.Headers.ContentLength ?? 1;
                    using (var stream = await response.Content.ReadAsStreamAsync())
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

                    // Sprawd� sum� kontroln�
                    if (!string.IsNullOrEmpty(md5FromHeader))
                    {
                        string localMd5 = CalculateMD5(targetPath);
                        if (!string.Equals(localMd5, md5FromHeader, StringComparison.OrdinalIgnoreCase))
                        {
                            var result = MessageBox.Show(
                                $"Suma kontrolna pliku nie zgadza si� z t� na serwerze!\nOczekiwana: {md5FromHeader}\nLokalna: {localMd5}\n\nCzy mimo to chcesz spr�bowa� rozpakowa� plik? (NIE zalecane!)",
                                "B��d sumy kontrolnej",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                            if (result == DialogResult.No)
                            {
                                File.Delete(targetPath);
                                return false;
                            }
                        }
                    }
                }
            }
            UIOutput.Write($"[INFO] Zako�czono pobieranie: {targetPath}");
            return true;
        }


        private async Task DownloadFileAsync(string url, string targetPath, ProgressBar progressBar, Label progressLabel, string fileNumber)
        {
            UIOutput.Write($"[INFO] Rozpoczynam pobieranie pliku {fileNumber} do: {Path.GetFileName(targetPath)}");
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Add("Authorization", downloadToken);
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        UIOutput.Write($"[ERROR] B��d pobierania pliku: {response.StatusCode}\n{errorContent}");
                        MessageBox.Show($"B��d pobierania pliku: {response.StatusCode}\n{errorContent}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
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
            UIOutput.Write($"[INFO] Zako�czono pobieranie: {targetPath}");
        }

        private string CalculateMD5(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
 }

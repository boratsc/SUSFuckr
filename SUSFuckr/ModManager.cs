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
                    UIOutput.Write($"[ERROR] Wyst¹pi³ b³¹d podczas instalacji: {ex}");
                    MessageBox.Show($"Wyst¹pi³ b³¹d podczas Instalacji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Nazwa pliku nie jest ju¿ kodowana w base64, u¿ywamy jej bezpoœrednio
            string vanilla7zName = $"{modConfig.AmongVersion.Replace("-", "").Replace(".", "")}";
            string vanilla7zPath = Path.Combine(vanillaDir, vanilla7zName + ".7z");
            string fileUrlAmongUs = $"{baseUrl}api/susfuckr-download-version?version={vanilla7zName}";

            // Deklaracje zmiennych które bêd¹ u¿ywane po pêtli
            string tempDir = Path.Combine(baseDirectory, "temp");
            string modFolderPath = Path.Combine(baseDirectory, modConfig.ModName);
            string modFile = Path.Combine(tempDir, "mod.zip");

            // 2. Pobierz vanilla 7z jeœli nie istnieje
            bool needsDownload = !File.Exists(vanilla7zPath);

            while (true) // Pêtla dla ponownego pobierania w przypadku b³êdów
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
                                "Wyst¹pi³ b³¹d podczas pobierania pliku vanilla lub suma kontrolna jest nieprawid³owa. Czy chcesz spróbowaæ ponownie?",
                                "B³¹d pobierania",
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
                    UIOutput.Write($"[INFO] Plik vanilla ju¿ istnieje: {vanilla7zPath}");
                }

                // SprawdŸ rozmiar pliku
                if (!File.Exists(vanilla7zPath) || new FileInfo(vanilla7zPath).Length < 1000)
                {
                    UIOutput.Write($"[ERROR] Pobrany plik vanilla jest nieprawid³owy lub pusty. SprawdŸ token i wersjê.");
                    MessageBox.Show("Pobrany plik vanilla jest nieprawid³owy lub pusty. SprawdŸ token i wersjê.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show($"Brak adresu URL do pobrania dla moda '{modConfig.ModName}'.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                progressBar.Visible = false;

                // 4. Przygotuj katalog moda
                if (Directory.Exists(modFolderPath))
                {
                    UIOutput.Write($"[INFO] Usuwam istniej¹cy katalog moda: {modFolderPath}");
                    Directory.Delete(modFolderPath, true);
                }
                Directory.CreateDirectory(modFolderPath);

                // 5. Rozpakuj vanilla 7z do katalogu moda
                try
                {
                    UIOutput.Write($"[INFO] Rozpakowujê vanilla 7z: {vanilla7zPath} do {modFolderPath}");
                    await Task.Run(() => Extract7zWithPassword(vanilla7zPath, modFolderPath, zipPassword));
                    UIOutput.Write($"[INFO] Rozpakowano vanilla.");

                    // Jeœli rozpakowywanie siê uda³o, wychodzimy z pêtli
                    break;
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[ERROR] B³¹d podczas rozpakowywania archiwum: {ex}");

                    // Usuñ uszkodzony plik
                    if (File.Exists(vanilla7zPath))
                    {
                        try
                        {
                            File.Delete(vanilla7zPath);
                            UIOutput.Write($"[INFO] Usuniêto uszkodzony plik: {vanilla7zPath}");
                        }
                        catch (Exception deleteEx)
                        {
                            UIOutput.Write($"[WARNING] Nie uda³o siê usun¹æ uszkodzonego pliku: {deleteEx.Message}");
                        }
                    }

                    // Zapytaj u¿ytkownika czy chce pobraæ ponownie
                    var retryResult = MessageBox.Show(
                        $"B³¹d podczas rozpakowywania archiwum vanilla:\n{ex.Message}\n\nPlik mo¿e byæ uszkodzony. Czy chcesz pobraæ go ponownie?",
                        "B³¹d rozpakowywania",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (retryResult == DialogResult.Yes)
                    {
                        needsDownload = true; // Oznacz ¿e trzeba pobraæ ponownie
                        continue; // Kontynuuj pêtlê - pobierz i spróbuj ponownie
                    }
                    else
                    {
                        // U¿ytkownik nie chce ponownie pobieraæ - zakoñcz
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
                UIOutput.Write($"[INFO] Rozpakowujê archiwum moda: {modFile} do {tempExtractPath}");

                // U¿yj ExtractToDirectory z nadpisywaniem plików
                ZipFile.ExtractToDirectory(modFile, tempExtractPath, overwriteFiles: true);

                UIOutput.Write($"[INFO] Rozpakowano archiwum moda.");
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] B³¹d podczas rozpakowywania archiwum moda: {ex}");
                MessageBox.Show($"B³¹d podczas rozpakowywania archiwum moda: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    UIOutput.Write($"[ERROR] Nie znaleziono plików do skopiowania.");
                    MessageBox.Show("Nie znaleziono plików do skopiowania.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                sourcePath = tempSourcePath;
            }

            UIOutput.Write($"[INFO] Kopiujê pliki moda z {sourcePath} do {modFolderPath}");
            CopyContent(sourcePath, modFolderPath);

            // 8. Zapisz konfiguracjê i posprz¹taj temp
            modConfig.InstallPath = modFolderPath;
            ConfigManager.SaveConfig(modConfigs);
            Directory.Delete(tempDir, true);

            UIOutput.Write($"[SUCCESS] Instalacja zakoñczona sukcesem dla moda: {modConfig.ModName}");
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
                UIOutput.Write($"[SUCCESS] Instalacja DLL zakoñczona sukcesem dla moda: {modConfig.ModName}");
                MessageBox.Show($"Instalacja DLL zakoñczona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] Wyst¹pi³ b³¹d podczas instalacji DLL: {ex}");
                MessageBox.Show($"Wyst¹pi³ b³¹d podczas instalacji DLL: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Extract7zWithPassword(string archivePath, string extractPath, string password)
        {
            try
            {
                UIOutput.Write($"[INFO] Rozpakowujê archiwum 7z: {archivePath} do {extractPath} (z has³em)");

                // Œcie¿ka do 7z.exe w katalogu aplikacji
                string? appDirPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
                if (string.IsNullOrEmpty(appDirPath))
                {
                    throw new InvalidOperationException("Nie mo¿na okreœliæ katalogu aplikacji.");
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
                        throw new Exception($"B³¹d podczas rozpakowywania archiwum. Kod wyjœcia: {process.ExitCode}. B³¹d: {error}");
                    }

                    UIOutput.Write($"[INFO] Archiwum rozpakowane pomyœlnie.");
                }
            }
            catch (Exception ex)
            {
                string safeErrorMessage = ex.Message.Replace(password, "***HIDDEN***");
                UIOutput.Write($"[ERROR] B³¹d podczas rozpakowywania archiwum: {safeErrorMessage}");
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
                    // Spróbuj usun¹æ katalog normalnie
                    Directory.Delete(directoryPath, true);
                    UIOutput.Write($"[INFO] Pomyœlnie usuniêto katalog: {directoryPath}");
                    return;
                }
                catch (IOException ex) when (currentRetry < maxRetries - 1)
                {
                    UIOutput.Write($"[WARNING] Próba {currentRetry + 1} usuniêcia katalogu nieudana: {ex.Message}");
                    currentRetry++;

                    // Spróbuj zwolniæ pliki poprzez GC
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    // Odczekaj chwilê
                    await Task.Delay(1000);

                    // Spróbuj usun¹æ pliki jeden po jednym
                    try
                    {
                        await ForceDeleteDirectory(directoryPath);
                        return;
                    }
                    catch (Exception innerEx)
                    {
                        UIOutput.Write($"[WARNING] Nie uda³o siê wymusiæ usuniêcia: {innerEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[ERROR] B³¹d podczas usuwania katalogu: {ex.Message}");
                    throw;
                }
            }

            throw new IOException($"Nie uda³o siê usun¹æ katalogu {directoryPath} po {maxRetries} próbach.");
        }

        private async Task ForceDeleteDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            // Usuñ atrybuty tylko do odczytu z wszystkich plików
            foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[WARNING] Nie uda³o siê usun¹æ pliku {file}: {ex.Message}");
                }
            }

            // Usuñ katalogi
            foreach (string dir in Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    await ForceDeleteDirectory(dir);
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[WARNING] Nie uda³o siê usun¹æ katalogu {dir}: {ex.Message}");
                }
            }

            try
            {
                Directory.Delete(directoryPath, false);
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[WARNING] Nie uda³o siê usun¹æ g³ównego katalogu {directoryPath}: {ex.Message}");
            }
        }


        // Metoda pomocnicza u¿ywaj¹ca oryginalnej biblioteki SevenZipExtractor
        private void ExtractWithSevenZipExtractor(string archivePath, string extractPath, string password)
        {
            UIOutput.Write("[INFO] Próba u¿ycia biblioteki SevenZipExtractor");
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7z.dll");

            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException($"Nie znaleziono biblioteki 7z.dll w katalogu: {dllPath}");
            }

            // Nie wyœwietlaj has³a w logach!
            UIOutput.Write($"[INFO] Próba rozpakowania archiwum przy u¿yciu cmd.exe i 7z.exe");

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
                    throw new Exception("Nie uda³o siê uruchomiæ procesu 7z.exe");

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Nie uda³o siê rozpakowaæ archiwum. Kod b³êdu: {process.ExitCode}");
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
                        UIOutput.Write($"[ERROR] B³¹d pobierania pliku: {response.StatusCode}\n{errorContent}");
                        MessageBox.Show($"B³¹d pobierania pliku: {response.StatusCode}\n{errorContent}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    // Pobierz sumê MD5 z nag³ówka
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

                    // SprawdŸ sumê kontroln¹
                    if (!string.IsNullOrEmpty(md5FromHeader))
                    {
                        string localMd5 = CalculateMD5(targetPath);
                        if (!string.Equals(localMd5, md5FromHeader, StringComparison.OrdinalIgnoreCase))
                        {
                            var result = MessageBox.Show(
                                $"Suma kontrolna pliku nie zgadza siê z t¹ na serwerze!\nOczekiwana: {md5FromHeader}\nLokalna: {localMd5}\n\nCzy mimo to chcesz spróbowaæ rozpakowaæ plik? (NIE zalecane!)",
                                "B³¹d sumy kontrolnej",
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
            UIOutput.Write($"[INFO] Zakoñczono pobieranie: {targetPath}");
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
                        UIOutput.Write($"[ERROR] B³¹d pobierania pliku: {response.StatusCode}\n{errorContent}");
                        MessageBox.Show($"B³¹d pobierania pliku: {response.StatusCode}\n{errorContent}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            UIOutput.Write($"[INFO] Zakoñczono pobieranie: {targetPath}");
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

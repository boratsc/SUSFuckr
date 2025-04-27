using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Updater
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            string targetDir = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
            string tempFilePath = Path.Combine(Path.GetTempPath(), "LatestVersion.zip");
            string tempExtractPath = Path.Combine(Path.GetTempPath(), "LatestVersionExtract");

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

                Console.WriteLine("Rozpakowywanie archiwum ZIP...");
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
                Directory.CreateDirectory(tempExtractPath);

                using (ZipArchive archive = ZipFile.OpenRead(tempFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        Console.WriteLine($"Processing entry: {entry.FullName}");
                        if (entry.FullName.StartsWith("SUSFuckr/") && !entry.FullName.StartsWith("SUSFuckr/updater/"))
                        {
                            string relativePath = entry.FullName.Substring("SUSFuckr/".Length);
                            string destinationPath = Path.Combine(tempExtractPath, relativePath);

                            if (entry.Name == "")
                            {
                                Console.WriteLine($"Tworzenie katalogu: {destinationPath}");
                                Directory.CreateDirectory(destinationPath);
                            }
                            else
                            {
                                Console.WriteLine($"Rozpakowywanie pliku: {destinationPath}");
                                entry.ExtractToFile(destinationPath, overwrite: true);
                            }
                        }
                    }
                }
                File.Delete(tempFilePath);

                Console.WriteLine("Docelowa ścieżka: " + targetDir);
                Console.WriteLine("Instalowanie nowej wersji...");
                foreach (string file in Directory.GetFiles(tempExtractPath, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(tempExtractPath, file);
                    string destFile = Path.Combine(targetDir, relativePath);
                    Console.WriteLine($"Kopiowanie {file} do {destFile}");

                    // Bezpieczna konwersja destDir - użycie ?? do zarządzania potencjalnym nullem
                    string destDir = Path.GetDirectoryName(destFile) ?? string.Empty;
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Console.WriteLine($"Tworzenie katalogu: {destDir}");
                        Directory.CreateDirectory(destDir);
                    }

                    File.Copy(file, destFile, true);
                }

                string appExePath = Path.Combine(targetDir, "SUSFuckr.exe");
                if (File.Exists(appExePath))
                {
                    Console.WriteLine($"Próba uruchomienia aplikacji: {appExePath}");
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = appExePath,
                            UseShellExecute = true,
                            WorkingDirectory = targetDir // Określamy katalog roboczy
                        });
                        Console.WriteLine("Aplikacja została uruchomiona pomyślnie.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd podczas uruchamiania aplikacji: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Plik wykonywalny nie istnieje: " + appExePath);
                }

                Console.WriteLine("Operacja zakończona. Naciśnij Enter, aby zakończyć.");

                Directory.Delete(tempExtractPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update failed: {ex.Message}");
                LogError(ex.ToString());

                Console.WriteLine("Błąd operacji. Naciśnij Enter, aby zakończyć.");
                Console.ReadLine();
            }
        }

        private static void LogError(string message)
        {
            // Logika zapisu błędów
            // Możesz dodać logikę zapisu błędów do logu
        }
    }
}
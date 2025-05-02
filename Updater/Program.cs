using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Updater
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Updater <target-dir> <zip-file-path>");
                return;
            }

            string targetDir = args[0];
            string tempFilePath = args[1];
            string tempExtractPath = Path.Combine(Path.GetTempPath(), "LatestVersionExtract");

            try
            {
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

                        // Pomijanie config.json
                        if (entry.FullName.EndsWith("config.json") || entry.FullName.StartsWith("SUSFuckr/updater/"))
                        {
                            continue;
                        }

                        if (entry.FullName.StartsWith("SUSFuckr/"))
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

                Console.WriteLine("Instalowanie nowej wersji...");
                foreach (string file in Directory.GetFiles(tempExtractPath, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(tempExtractPath, file);
                    string destFile = Path.Combine(targetDir, relativePath);
                    Console.WriteLine($"Kopiowanie {file} do {destFile}");

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
                            WorkingDirectory = targetDir
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

                Console.WriteLine("Usuwanie tymczasowych plików...");
                Directory.Delete(tempExtractPath, true);
                File.Delete(tempFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uaktualnienie nie powiodło się: {ex.Message}");
                Console.WriteLine("Naciśnij Enter, aby zakończyć.");
                Console.ReadLine();
            }
        }
    }
}
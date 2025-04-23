using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;  // Add this namespace for the FirstOrDefault extension method

namespace SUSFuckr
{
    public class ModManager
    {
        private readonly string baseUrl = "http://polatany.ipv64.net:8087/";

        public async Task ModifyAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs)
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

                    await DownloadFileAsync(fileUrlAmongUs, tempFileAmongUs);

                    string modFile = Path.Combine(baseDirectory, "temp", "mod.zip");
                    await DownloadFileAsync(modConfig.GitHubRepoOrLink, modFile);

                    string modFolderPath = Path.Combine(baseDirectory, modConfig.ModName);
                    Directory.CreateDirectory(modFolderPath);

                    // Use 'using statements to automatically handle resource disposal
                    ZipFile.ExtractToDirectory(tempFileAmongUs, modFolderPath);

                    string tempExtractPath = Path.Combine(baseDirectory, "temp", "extractMod");
                    Directory.CreateDirectory(tempExtractPath);
                    ZipFile.ExtractToDirectory(modFile, tempExtractPath);

                    // Check structure and move files
                    string sourcePath;
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

                    // Update config and clean up
                    modConfig.InstallPath = modFolderPath;
                    ConfigManager.SaveConfig(modConfigs);

                    // Clean up temp directory
                    Directory.Delete(Path.Combine(baseDirectory, "temp"), true);

                    MessageBox.Show($"Modyfikacja zakoñczona sukcesem dla moda: {modConfig.ModName}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wyst¹pi³ b³¹d podczas modyfikacji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static async Task DownloadFileAsync(string url, string targetPath)
        {
            // Proper use of using to ensure all disposable resources are cleared
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        // Use using to automatically release FileStream resources
                        using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
        }
    }
}
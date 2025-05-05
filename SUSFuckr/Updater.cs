using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json; // Po dodaniu
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

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

                if (string.Compare(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    DialogResult dialogResult = MessageBox.Show($"Dostêpna jest nowa wersja aplikacji.\nObecna wersja: {currentVersion}\nNowa wersja: {latestVersion}\nCzy chcesz zaktualizowaæ?", "Aktualizacja aplikacji", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dialogResult == DialogResult.Yes)
                    {
                        await DownloadAndRunUpdaterAsync(latestVersion);
                    }
                }
                else
                {
                   // MessageBox.Show("Masz ju¿ najnowsz¹ wersjê aplikacji.", "Aktualizacja aplikacji", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                return versionInfo?["version"] ?? "0.0.0"; // Obs³u¿enie wartoœci null
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

                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater", "updater.exe");

                await UpdateConfigurationBeforeExitAsync();

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    UseShellExecute = true,
                    Arguments = $"\"{AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')}\" \"{tempFilePath}\""
                });

                Application.Exit();
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
                var tempFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.temp.json");
                using (HttpClient client = new HttpClient())
                {
                    string updateServerUrl = configuration["Configuration:UpdateServerUrl"]; // U¿yj przekazanej konfiguracji
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
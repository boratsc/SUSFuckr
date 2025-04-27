using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SUSFuckr
{
    public class Updater
    {
        private readonly string currentVersion;

        public Updater(string currentVersion)
        {
            this.currentVersion = currentVersion ?? "0.0.0";
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
                        await UpdateApplicationAsync(latestVersion);
                    }
                }
                else
                {
                    MessageBox.Show("Masz ju¿ najnowsz¹ wersjê aplikacji.", "Aktualizacja aplikacji", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private async Task UpdateApplicationAsync(string latestVersion)
        {
            await Task.Run(() =>
            {
                MessageBox.Show($"Zaraz zaktualizujemy i zrestartujemy aplikacjê.\nObecna wersja: {currentVersion}\nNowa wersja: {latestVersion}", "Aktualizacja aplikacji", MessageBoxButtons.OK, MessageBoxIcon.Information);

                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater", "updater.exe");
                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterPath,
                    UseShellExecute = true,
                    Arguments = $"\"{AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')}\""
                });

                Application.Exit();
            });
        }
    }
}
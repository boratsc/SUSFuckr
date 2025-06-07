using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace SUSFuckr
{
    public static class ModUpdateChecker
    {
        public static async Task CheckForModUpdatesAsync(IConfiguration configuration)
        {
            try
            {
                // 1. Pobierz config.json z endpointu
                var remoteConfigs = await DownloadRemoteConfigAsync(configuration);
                if (remoteConfigs == null || !remoteConfigs.Any())
                {
                    return; // Brak danych z serwera
                }

                // 2. Pobierz lokalne konfiguracje
                var localConfigs = ConfigManager.LoadConfig();

                // 3. Znajdź zainstalowane mody które mają dostępne aktualizacje
                var modsToUpdate = FindModsWithUpdates(localConfigs, remoteConfigs);

                // 4. Zaproponuj aktualizację użytkownikowi
                if (modsToUpdate.Any())
                {
                    ProposeUpdatesToUser(modsToUpdate, configuration);
                }
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] Błąd podczas sprawdzania aktualizacji modów: {ex.Message}");
            }
        }

        private static async Task<List<ModConfiguration>?> DownloadRemoteConfigAsync(IConfiguration configuration)
        {
            try
            {
                var tempFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.temp.json");

                using (HttpClient client = new HttpClient())
                {
                    string downloadToken = SecretProvider.GetDownloadToken();
                    client.DefaultRequestHeaders.Add("Authorization", downloadToken);

                    HttpResponseMessage response = await client.GetAsync(configuration["Configuration:UpdateServerUrl"]);
                    response.EnsureSuccessStatusCode();

                    using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                var jsonContent = await File.ReadAllTextAsync(tempFilePath);
                var remoteConfigs = JsonSerializer.Deserialize<List<ModConfiguration>>(jsonContent) ?? new List<ModConfiguration>();

                File.Delete(tempFilePath);

                return remoteConfigs;
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] Nie udało się pobrać konfiguracji z serwera: {ex.Message}");
                return null;
            }
        }

        private static List<ModUpdateInfo> FindModsWithUpdates(List<ModConfiguration> localConfigs, List<ModConfiguration> remoteConfigs)
        {
            var modsToUpdate = new List<ModUpdateInfo>();

            foreach (var localMod in localConfigs)
            {
                // Sprawdź tylko zainstalowane mody typu "full"
                if (localMod.ModType != "full" || string.IsNullOrEmpty(localMod.InstallPath) || !Directory.Exists(localMod.InstallPath))
                    continue;

                var remoteMod = remoteConfigs.FirstOrDefault(r => r.Id == localMod.Id);
                if (remoteMod == null)
                    continue;

                // Porównaj tylko ModVersion
                if (HasNewerVersion(localMod, remoteMod))
                {
                    modsToUpdate.Add(new ModUpdateInfo
                    {
                        LocalMod = localMod,
                        RemoteMod = remoteMod,
                        CurrentVersion = localMod.ModVersion ?? "Nieznana",
                        NewVersion = remoteMod.ModVersion ?? "Nieznana"
                    });
                }
            }

            return modsToUpdate;
        }

        private static bool HasNewerVersion(ModConfiguration localMod, ModConfiguration remoteMod)
        {
            // Porównaj tylko ModVersion - ignoruj LastUpdated
            if (!string.IsNullOrEmpty(localMod.ModVersion) && !string.IsNullOrEmpty(remoteMod.ModVersion))
            {
                // Sprawdź czy wersje są różne (zakładamy że różna wersja = nowsza wersja)
                return !string.Equals(localMod.ModVersion, remoteMod.ModVersion, StringComparison.OrdinalIgnoreCase);
            }

            // Jeśli remote ma wersję a local nie ma, to jest aktualizacja
            if (string.IsNullOrEmpty(localMod.ModVersion) && !string.IsNullOrEmpty(remoteMod.ModVersion))
            {
                return true;
            }

            // Jeśli nie ma wystarczających danych do porównania, zakładamy że nie ma aktualizacji
            return false;
        }

        private static void ProposeUpdatesToUser(List<ModUpdateInfo> modsToUpdate, IConfiguration configuration)
        {
            try
            {
                using var updateForm = new ModUpdateProposalForm(modsToUpdate, configuration);
                updateForm.ShowDialog();
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] Błąd podczas wyświetlania propozycji aktualizacji: {ex.Message}");
            }
        }
    }

    public class ModUpdateInfo
    {
        public ModConfiguration LocalMod { get; set; } = null!;
        public ModConfiguration RemoteMod { get; set; } = null!;
        public string CurrentVersion { get; set; } = string.Empty;
        public string NewVersion { get; set; } = string.Empty;
    }

    public partial class ModUpdateProposalForm : Form
    {
        private readonly List<ModUpdateInfo> modsToUpdate;
        private readonly IConfiguration configuration;
        private CheckedListBox modsList = null!;
        private Button updateButton = null!;
        private Button cancelButton = null!;

        public ModUpdateProposalForm(List<ModUpdateInfo> modsToUpdate, IConfiguration configuration)
        {
            this.modsToUpdate = modsToUpdate;
            this.configuration = configuration;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Dostępne aktualizacje modów";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var titleLabel = new Label
            {
                Text = "Znaleziono dostępne aktualizacje dla następujących modów:",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(460, 40),
                AutoSize = false
            };
            this.Controls.Add(titleLabel);

            modsList = new CheckedListBox
            {
                Location = new System.Drawing.Point(10, 60),
                Size = new System.Drawing.Size(460, 250),
                CheckOnClick = true
            };

            foreach (var modUpdate in modsToUpdate)
            {
                string displayText = $"{modUpdate.LocalMod.ModName} ({modUpdate.CurrentVersion} → {modUpdate.NewVersion})";
                modsList.Items.Add(displayText, true); // Domyślnie zaznaczone
            }

            this.Controls.Add(modsList);

            updateButton = new Button
            {
                Text = "Aktualizuj wybrane",
                Location = new System.Drawing.Point(300, 320),
                Size = new System.Drawing.Size(120, 30)
            };
            updateButton.Click += UpdateButton_Click;
            this.Controls.Add(updateButton);

            cancelButton = new Button
            {
                Text = "Anuluj",
                Location = new System.Drawing.Point(200, 320),
                Size = new System.Drawing.Size(80, 30)
            };
            cancelButton.Click += (s, e) => this.Close();
            this.Controls.Add(cancelButton);
        }

        private async void UpdateButton_Click(object? sender, EventArgs e)
        {
            var selectedMods = new List<ModUpdateInfo>();

            for (int i = 0; i < modsList.Items.Count; i++)
            {
                if (modsList.GetItemChecked(i))
                {
                    selectedMods.Add(modsToUpdate[i]);
                }
            }

            if (!selectedMods.Any())
            {
                MessageBox.Show("Nie wybrano żadnych modów do aktualizacji.", "Informacja",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Wyłącz przyciski podczas aktualizacji
            updateButton.Enabled = false;
            cancelButton.Enabled = false;
            updateButton.Text = "Aktualizowanie...";

            try
            {
                // 1. Najpierw zaktualizuj konfigurację
                await UpdateConfigurationAsync();

                // 2. Następnie zaktualizuj wybrane mody
                await UpdateSelectedModsAsync(selectedMods);

                MessageBox.Show($"Pomyślnie zaktualizowano {selectedMods.Count} mod(ów). Aplikacja zostanie ponownie uruchomiona.", "Sukces",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Uruchom ponownie aplikację
                var exePath = Application.ExecutablePath;
                Process.Start(exePath);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji modów: {ex.Message}", "Błąd",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Przywróć przyciski w przypadku błędu
                updateButton.Enabled = true;
                cancelButton.Enabled = true;
                updateButton.Text = "Aktualizuj wybrane";
            }
        }

        private async Task UpdateConfigurationAsync()
        {
            try
            {
                var tempFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.temp.json");

                using (HttpClient client = new HttpClient())
                {
                    string downloadToken = SecretProvider.GetDownloadToken();
                    client.DefaultRequestHeaders.Add("Authorization", downloadToken);
                    HttpResponseMessage response = await client.GetAsync(configuration["Configuration:UpdateServerUrl"]);
                    response.EnsureSuccessStatusCode();
                    using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }

                ConfigUpdater.CompareAndMergeConfigurations(tempFilePath);
                File.Delete(tempFilePath);

                UIOutput.Write("Konfiguracja została zaktualizowana.");
            }
            catch (Exception ex)
            {
                UIOutput.Write($"[ERROR] Błąd podczas aktualizacji konfiguracji: {ex.Message}");
                throw;
            }
        }

        private async Task UpdateSelectedModsAsync(List<ModUpdateInfo> selectedMods)
        {
            string mode = configuration["Configuration:Mode"] ?? "steam";

            foreach (var modUpdate in selectedMods)
            {
                try
                {
                    UIOutput.Write($"Aktualizowanie moda: {modUpdate.LocalMod.ModName}");

                    // 1. Usuń stary mod
                    var currentConfigs = ConfigManager.LoadConfig();
                    var modToDelete = currentConfigs.FirstOrDefault(c => c.Id == modUpdate.LocalMod.Id);
                    if (modToDelete != null)
                    {
                        ModDelete.DeleteMod(modToDelete, currentConfigs);
                    }

                    // 2. Pobierz zaktualizowaną konfigurację po usunięciu
                    var updatedConfigs = ConfigManager.LoadConfig();
                    var modToInstall = updatedConfigs.FirstOrDefault(c => c.Id == modUpdate.RemoteMod.Id);

                    if (modToInstall != null)
                    {
                        // 3. Zainstaluj nową wersję używając istniejących mechanizmów
                        var modManager = new ModManager(configuration);

                        // Utwórz kontrolki i dodaj je do formularza
                        var progressBar = new ProgressBar
                        {
                            Style = ProgressBarStyle.Continuous,
                            Visible = false
                        };
                        var progressLabel = new Label
                        {
                            Visible = false
                        };

                        // Dodaj kontrolki do formularza, żeby miały uchwyt okna
                        this.Controls.Add(progressBar);
                        this.Controls.Add(progressLabel);

                        try
                        {
                            if (mode == "steam")
                            {
                                await modManager.ModifyAsync(modToInstall, updatedConfigs, progressBar, progressLabel, mode);
                            }
                            else if (mode == "epic")
                            {
                                var epicManager = new EpicVersionManager();
                                await epicManager.ModifyEpicAsync(modToInstall, progressBar, progressLabel);
                            }
                        }
                        finally
                        {
                            // Usuń kontrolki po zakończeniu
                            this.Controls.Remove(progressBar);
                            this.Controls.Remove(progressLabel);
                            progressBar.Dispose();
                            progressLabel.Dispose();
                        }
                    }

                    UIOutput.Write($"Pomyślnie zaktualizowano mod: {modUpdate.LocalMod.ModName}");
                }
                catch (Exception ex)
                {
                    UIOutput.Write($"[ERROR] Błąd podczas aktualizacji moda {modUpdate.LocalMod.ModName}: {ex.Message}");
                    throw;
                }
            }
        }
    }
}

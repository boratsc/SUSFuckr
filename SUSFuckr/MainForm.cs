using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json; // Dla JsonSerializer
using System.IO.Compression; // Dla ZipFile


namespace SUSFuckr
{
    public partial class MainForm : Form
    {
        private List<ModConfiguration> modConfigs;
        private Label progressLabel;
        private MenuStrip menuStrip = new MenuStrip();
        private readonly IConfiguration Configuration;
        private readonly string appVersion = string.Empty;
        private ToolTip toolTip;
        private Updater updater;
        private ModConfiguration selectedModConfig;
        private ProgressBar progressBarBusy;
        private ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.RichTextBox txtLegendaryLog;


        public MainForm()
        {
            InitializeComponent();


            this.txtLegendaryLog = new System.Windows.Forms.RichTextBox();
            // 
            // txtLegendaryLog
            // 
            this.txtLegendaryLog.Location = new System.Drawing.Point(7, 461);
            this.txtLegendaryLog.Name = "txtLegendaryLog";
            this.txtLegendaryLog.Size = new System.Drawing.Size(600, 100);
            this.txtLegendaryLog.TabIndex = 10;
            this.txtLegendaryLog.Text = "";

            // don't forget to add it to Controls
            this.Controls.Add(this.txtLegendaryLog);

            // Konfiguracja aplikacji
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration = builder.Build();

            // --- dodaj statusStrip + statusLabel ---
            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);

            // --- dodaj progressBarBusy ---
            progressBarBusy = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Width = 200,
                Height = 20,
                Dock = DockStyle.Bottom,
                Visible = false,
                MarqueeAnimationSpeed = 30
            };
            this.Controls.Add(progressBarBusy);

            progressBarBusy.Style = ProgressBarStyle.Marquee;
            progressBarBusy.MarqueeAnimationSpeed = 30;
            progressBarBusy.Visible = false;

            statusLabel.Text = "";
            statusLabel.Visible = false;

            appVersion = Configuration["Configuration:CurrentVersion"] ?? "0.0.1";
            Text = $"SUSFuckr - przyjazny instalator modów {appVersion}";
            Width = 630;
            Height = 625;
            Icon = new Icon("Graphics/icon.ico");

            toolTip = new ToolTip();

            // Inicjalizacja updatera i sprawdzanie wersji
            updater = new Updater(appVersion, Configuration);
            updater.CheckAndPromptForUpdateAsync();

            // Tworzenie menu
            

            // Inicjalizacja konfiguracji modów
            ModConfigHandler.Initialize(Configuration);
            modConfigs = ConfigManager.LoadConfig();
            CreateMenu();
            // Ustawienia layoutu itp.
            Load += FormLoad;
            progressLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(progressBar.Width / 2 - 50, progressBar.Height / 2 - 10),
            };
            progressBar.Controls.Add(progressLabel);

            // Dodatkowe konfiguracje
            GameLocator.CheckAndSetupVanillaMod(modConfigs, Configuration);
        }


        public class OutputForm : Form
        {
            private TextBox outputTextBox;

            public OutputForm()
            {
                outputTextBox = new TextBox
                {
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill
                };
                this.Controls.Add(outputTextBox);
                this.Size = new Size(600, 400); // Rozmiar okna
            }

            // Metoda do aktualizacji zawartości TextBox
            public void AppendText(string text)
            {
                outputTextBox.Invoke((MethodInvoker)delegate
                {
                    outputTextBox.AppendText(text + Environment.NewLine);
                });
            }
        }

        private void CreateMenu()
        {
           
            menuStrip = new MenuStrip();
            ToolStripMenuItem additionalActionsMenuItem = new ToolStripMenuItem("Dodatkowe akcje");

            ToolStripMenuItem touConfigItem = new ToolStripMenuItem("Konfiguracje ToU");
            ToolStripMenuItem saveLocalConfigItem = new ToolStripMenuItem("Zapisz konfiguracje lokalnie");
            saveLocalConfigItem.Click += (s, ev) => ModConfigHandler.SaveLocalConfig();
            touConfigItem.DropDownItems.Add(saveLocalConfigItem);

            ToolStripMenuItem loadLocalConfigItem = new ToolStripMenuItem("Wczytaj konfiguracje lokalne");
            loadLocalConfigItem.Click += (s, ev) => ModConfigHandler.LoadLocalConfig();
            touConfigItem.DropDownItems.Add(loadLocalConfigItem);

            ToolStripMenuItem saveServerConfigItem = new ToolStripMenuItem("Zapisz konfiguracje na serwerze");
            saveServerConfigItem.Click += async (s, ev) => await ModConfigHandler.SaveServerConfigAsync();
            touConfigItem.DropDownItems.Add(saveServerConfigItem);

            ToolStripMenuItem loadServerConfigItem = new ToolStripMenuItem("Wczytaj konfiguracje z serwera");
            loadServerConfigItem.Click += async (s, ev) => await ModConfigHandler.LoadServerConfigAsync();
            touConfigItem.DropDownItems.Add(loadServerConfigItem);

            ToolStripMenuItem loadLocalTxtConfigItem = new ToolStripMenuItem("Wczytaj konfigurację z pliku .txt");
            loadLocalTxtConfigItem.Click += (s, ev) => ModConfigHandler.LoadLocalTxtConfig();
            touConfigItem.DropDownItems.Add(loadLocalTxtConfigItem);

            ToolStripMenuItem changePresetNamesItem = new ToolStripMenuItem("Zmień nazwy presetów lobby");
            changePresetNamesItem.Click += (s, ev) => ModConfigHandler.ChangePresetNames();
            touConfigItem.DropDownItems.Add(changePresetNamesItem);

            ToolStripMenuItem lobbySetItem = new ToolStripMenuItem("Ustaw ilość osób w Lobby");
            lobbySetItem.Click += (s, ev) => LobbySet();
            touConfigItem.DropDownItems.Add(lobbySetItem);



            additionalActionsMenuItem.DropDownItems.Add(touConfigItem);



            ToolStripMenuItem updateConfigItem = new ToolStripMenuItem("Aktualizuj konfigurację");
            updateConfigItem.Click += new EventHandler(UpdateConfigMenuItem_Click);
            updateConfigItem.MouseHover += (s, ev) => toolTip.Show("Pobiera najnowszą wersję konfiguracji z serwera (np. aktualizację modów)", menuStrip, MousePosition.X - this.Location.X, MousePosition.Y - this.Location.Y, 2000);
            additionalActionsMenuItem.DropDownItems.Add(updateConfigItem);

            // Dodanie opcji "Aktualizuj aplikację"
            ToolStripMenuItem updateApplicationItem = new ToolStripMenuItem("Aktualizuj aplikację");
            updateApplicationItem.Click += new EventHandler(UpdateApplicationMenuItem_Click);
            updateApplicationItem.MouseHover += (s, ev) => toolTip.Show("Pobiera najnowszą wersję aplikacji", menuStrip, MousePosition.X - this.Location.X, MousePosition.Y - this.Location.Y, 2000);
            additionalActionsMenuItem.DropDownItems.Add(updateApplicationItem);

            ToolStripMenuItem pathSettingsMenuItem = new ToolStripMenuItem("Zmień ścieżkę instalacji modów");
            pathSettingsMenuItem.Click += new EventHandler(PathSettingsMenuItem_Click);
            pathSettingsMenuItem.MouseHover += (s, ev) => toolTip.Show("Pozwala zmienić domyślną lokalizację instalacji modów", menuStrip, MousePosition.X - this.Location.X, MousePosition.Y - this.Location.Y, 2000);
            additionalActionsMenuItem.DropDownItems.Add(pathSettingsMenuItem);


            menuStrip.Items.Add(additionalActionsMenuItem);

            ToolStripMenuItem dllModsMenuItem = new ToolStripMenuItem("Modyfikacje DLL");
            if (modConfigs != null)
            {
                foreach (var modConfig in modConfigs.Where(x => x.ModType == "dll"))
                {
                    ToolStripMenuItem modItem = new ToolStripMenuItem(modConfig.ModName);
                    // Opcja "Instaluj"
                    ToolStripMenuItem installItem = new ToolStripMenuItem("Instaluj");
                    installItem.Click += (sender, e) => InstallDllMod(modConfig);
                    // Opcja "Usuń"
                    ToolStripMenuItem uninstallItem = new ToolStripMenuItem("Usuń");
                    uninstallItem.Click += (sender, e) => UninstallDllMod(modConfig);
                    modItem.DropDownItems.Add(installItem);
                    modItem.DropDownItems.Add(uninstallItem);
                    dllModsMenuItem.DropDownItems.Add(modItem);
                }
                menuStrip.Items.Add(dllModsMenuItem); // Dodajemy menu tylko raz, po zakończeniu pętli
            }
            else
            {
                MessageBox.Show("Nie załadowano konfiguracji modów.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            ToolStripMenuItem fixBlackScreenItem = new ToolStripMenuItem("Napraw Amonga");
            fixBlackScreenItem.Click += new EventHandler(FixBlackScreenMenuItem_Click);
            fixBlackScreenItem.MouseHover += (s, ev) => toolTip.Show("Naprawia czarny ekran, problemy z komunikacją, \nsprawia, że jest pokój na świecie, ale wywala prawie całą konfigurację!", menuStrip, MousePosition.X - this.Location.X, MousePosition.Y - this.Location.Y, 2000);
            //additionalActionsMenuItem.DropDownItems.Add(fixBlackScreenItem);
            menuStrip.Items.Add(fixBlackScreenItem);

            ToolStripMenuItem infoMenuItem = new ToolStripMenuItem("Informacje");
            infoMenuItem.Click += new EventHandler(InfoMenuItem_Click);
            menuStrip.Items.Add(infoMenuItem);

            ToolStripMenuItem supportMenuItem = new ToolStripMenuItem("Donate");
            supportMenuItem.Click += (s, ev) => Process.Start(new ProcessStartInfo
            {
                FileName = "https://liberapay.com/boracik/donate",
                UseShellExecute = true
            });
            supportMenuItem.MouseHover += (s, ev) => toolTip.Show("Zbieram hajs, żeby Windows się nie pluł, że aplikacja jest niebezpieczna!", menuStrip, MousePosition.X - this.Location.X, MousePosition.Y - this.Location.Y, 2000);
            menuStrip.Items.Add(supportMenuItem);


            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void PathSettingsMenuItem_Click(object sender, EventArgs e)
        {
            using (var pathSettingsForm = new PathSettingsForm())
            {
                pathSettingsForm.ShowDialog(this);
            }
        }

        private void LobbySet()
        {
            // Wyświetlenie dialogu do wpisania liczby graczy
            using (Form dialog = new Form())
            {
                dialog.Text = "Ustaw ilość graczy w Lobby";
                dialog.Size = new Size(300, 140); // Ustawienie rozmiaru okna
                dialog.StartPosition = FormStartPosition.CenterScreen; // Wyśrodkowanie okna na ekranie

                Label label = new Label { Text = "Wpisz liczbę graczy (od 4 do 255):", AutoSize = true, Location = new Point(10, 20) };
                TextBox textBox = new TextBox { Location = new Point(195, 15), Width = 50 };
                Button button = new Button { Text = "OK", Location = new Point(100, 60), DialogResult = DialogResult.OK };

                dialog.Controls.Add(label);
                dialog.Controls.Add(textBox);
                dialog.Controls.Add(button);

                dialog.AcceptButton = button; // Umożliwienie zatwierdzenia formularza po wciśnięciu Enter

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (int.TryParse(textBox.Text, out int numPlayers) && numPlayers >= 4 && numPlayers <= 255)
                    {

                        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string filePath =  Path.Combine(userProfile, @"AppData\LocalLow\Innersloth\Among Us\settings.amogus_TOU");

                        if (File.Exists(filePath))
                        {
                            // Odczytanie zawartości pliku
                            string jsonContent = File.ReadAllText(filePath);

                            // Deserializacja JSON
                            dynamic settings = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

                            // Generowanie nowego kodu
                            string customCode = GenerateCustomCode(numPlayers);

                            // Aktualizacja normalHostOptions
                            string normalHostOptions = settings.multiplayer.normalHostOptions;
                            string updatedNormalHostOptions = $"{normalHostOptions.Substring(0, 8)}{customCode}{normalHostOptions.Substring(12)}";
                            settings.multiplayer.normalHostOptions = updatedNormalHostOptions;

                            // Serializacja zaktualizowanych danych JSON
                            string updatedJsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);

                            // Zapis zaktualizowanych danych do pliku
                            File.WriteAllText(filePath, updatedJsonContent);

                            // Wyświetlenie komunikatu po zakończeniu operacji
                            MessageBox.Show($"Ustawiono liczbę graczy na {numPlayers}. Wymagany jest CrowdedMod, aby uruchomić lobby z większą ilością graczy.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Brak pliku konfiguracyjnego ToU - uruchom grę z modem, zamknij i spróbuj ponownie.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Proszę wpisać poprawną liczbę graczy (od 4 do 255).", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        static string GenerateCustomCode(int value)
        {
            byte[] bytes = new byte[] { 0x00, (byte)value, 0x01 };
            return Convert.ToBase64String(bytes);
        }

        private async void InstallDllMod(ModConfiguration modConfig)
        {
            // Upewnij się, czy `modConfig` nie jest nullem i czy jest typu `dll`
            if (modConfig != null && modConfig.ModType == "dll")
            {
                // Zdobądź listę pełnych modów, dla których instalacja DLL może być wykonywana
                var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath)).ToList();

                using var modSelector = new ModSelectorForm(fullMods);

                // Wybór pełnych modów, z których użytkownik chce zainstalować mod DLL
                if (modSelector.ShowDialog() == DialogResult.OK)
                {
                    var selectedMods = modSelector.SelectedMods;
                    ModManager manager = new ModManager(Configuration);

                    // Pokaż pasek postępu
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressLabel.Visible = true;

                    try
                    {
                        await manager.ModifyDllAsync(modConfig, selectedMods, progressBar, progressLabel);
                        MessageBox.Show($"Instalacja moda {modConfig.ModName} zakończona pomyślnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas instalacji moda {modConfig.ModName}: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // Ukryj pasek postępu
                        progressBar.Visible = false;
                        progressLabel.Visible = false;
                    }
                }
                else
                {
                    MessageBox.Show("Nie wybrano żadnych pełnych modów do instalacji.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Nie można zainstalować, nieprawidłowa konfiguracja lub typ moda.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UninstallDllMod(ModConfiguration modConfig)
        {
            // Upewnij się, czy `modConfig` nie jest nullem i czy jest typu `dll`
            if (modConfig != null && modConfig.ModType == "dll")
            {
                // Pobierz konfiguracje pełnych modów, dla których może być zainstalowany mod DLL
                var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath)).ToList();

                // Używamy pełnych modów do usunięcia DLL z ich katalogów
                foreach (var fullMod in fullMods)
                {
                    string dllPath = Path.Combine(fullMod.InstallPath, modConfig.DllInstallPath ?? string.Empty, $"{modConfig.ModName}.dll");
                    try
                    {
                        if (File.Exists(dllPath))
                        {
                            File.Delete(dllPath);
                            Console.WriteLine($"Plik {dllPath} został usunięty.");
                        }
                        else
                        {
                            Console.WriteLine($"Plik {dllPath} nie istnieje.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd podczas usuwania pliku {dllPath}: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                MessageBox.Show($"Usuwanie moda {modConfig.ModName} zakończone pomyślnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Nie można usunąć, nieprawidłowa konfiguracja lub typ moda.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private async void UpdateApplicationMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await updater.CheckAndPromptForUpdateAsync(); // Poprawne użycie kontekstu
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji aplikacji: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ModifyEpicAndUpdateUI(ModConfiguration oldConfig)
        {
            var epicManager = new EpicVersionManager();
            await epicManager.ModifyEpicAsync(oldConfig, progressBar, progressLabel);

            // Załaduj nowy config z pliku (ważne!)
            modConfigs = ConfigManager.LoadConfig();

            // Znajdź nowy obiekt odpowiadający staremu
            var updatedConfig = modConfigs.FirstOrDefault(c => c.Id == oldConfig.Id);
            if (updatedConfig != null)
            {
                selectedModConfig = updatedConfig;
                UpdateFormDisplay(updatedConfig); //  przyciski i ikonki bazują na aktualnym stanie
                RefreshSingleIcon(selectedIcon);
                RestartApplication();
            }
        }

        private async void UpdateConfigMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                var tempFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.temp.json");
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(Configuration["Configuration:UpdateServerUrl"]);
                    response.EnsureSuccessStatusCode();
                    using (FileStream fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                }
                ConfigUpdater.CompareAndMergeConfigurations(tempFilePath);
                File.Delete(tempFilePath);
                MessageBox.Show("Konfiguracja została zaktualizowana. Aplikacja zostanie teraz ponownie uruchomiona.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RestartApplication();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji konfiguracji: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestartApplication()
        {
            var exePath = Application.ExecutablePath;
            Process.Start(exePath);
            Application.Exit();
        }

        private void InfoMenuItem_Click(object? sender, EventArgs e)
        {
            Information.ShowInfoWindow(appVersion);
        }

        private void FixBlackScreenMenuItem_Click(object? sender, EventArgs e)
        {
            FixBlackScreen.ExecuteFix();
        }

        private void ReturnToMain()
        {
            return;
        }

        private async void FormLoad(object? sender, EventArgs e)
        {
            modConfigs = ConfigManager.LoadConfig(); // Ładujemy istniejące konfiguracje
            var vanillaMod = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs");
            // Sprawdź wersję gry
            string mode = Configuration["Configuration:Mode"] ?? "steam"; // Default to steam
            string? path = vanillaMod?.InstallPath;
            path = path != null && File.Exists(Path.Combine(path, "Among Us.exe"))
                ? path
                : GameLocator.TryFindAmongUsPath(out mode);
            // Aktualizuj konfigurację w zależności od znalezionej wersji
            if (path != null)
            {
                Configuration["Configuration:Mode"] = mode;
                var version = GetGameVersion(path);
                textBoxPath.Text = path.Replace('/', '\\');
                labelVersion.Text = "Wersja: " + version;
                if (vanillaMod == null)
                {
                    vanillaMod = new ModConfiguration
                    {
                        Id = 0,
                        ModName = "AmongUs",
                        PngFileName = "Vanilla.png",
                        InstallPath = path,
                        GitHubRepoOrLink = null,
                        ModType = "Vanilla",
                        DllInstallPath = null,
                        LastUpdated = null,
                        AmongVersion = version,
                        Description = $"Wykryto wersję {mode}"
                    };
                    modConfigs.Add(vanillaMod);
                }
                else
                {
                    vanillaMod.InstallPath = path;
                    vanillaMod.AmongVersion = version;
                    vanillaMod.Description = $"Wykryto wersję {mode}";
                }
                ConfigManager.SaveConfig(modConfigs);
                AddGameIcon(vanillaMod);
                var vanillaIcon = this.scrollablePanel.Controls.OfType<PictureBox>().FirstOrDefault(icon => icon.Name == $"gameIcon_{vanillaMod.ModName}");
                if (vanillaIcon != null)
                {
                    AddInstalledIcon(vanillaIcon);
                    if (!string.IsNullOrEmpty(vanillaMod.Description))
                    {
                        toolTip.SetToolTip(vanillaIcon, vanillaMod.Description);
                    }
                }
            }
            else
            {
                textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                labelVersion.Text = "Wersja: Nieznana";
                if (vanillaMod == null)
                {
                    vanillaMod = new ModConfiguration
                    {
                        Id = 0,
                        ModName = "AmongUs",
                        PngFileName = "Vanilla.png",
                        InstallPath = "",
                        GitHubRepoOrLink = null,
                        ModType = "Vanilla",
                        DllInstallPath = null,
                        LastUpdated = null,
                        AmongVersion = "Nieznana",
                        Description = "Nie znaleziono Among Us.exe."
                    };
                    modConfigs.Add(vanillaMod);
                    ConfigManager.SaveConfig(modConfigs);
                }
                AddGameIcon(vanillaMod);
                var vanillaIcon = this.scrollablePanel.Controls.OfType<PictureBox>().FirstOrDefault(icon => icon.Name == $"gameIcon_{vanillaMod.ModName}");
                if (vanillaIcon != null)
                {
                    if (!string.IsNullOrEmpty(vanillaMod.Description))
                    {
                        toolTip.SetToolTip(vanillaIcon, vanillaMod.Description);
                    }
                }
            }

            // Pobranie legendary.exe dla wersji Epic
            if (mode == "epic" && !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "legendary.exe")))
            {
                try
                {
                    string legendaryUrl = "https://github.com/derrod/legendary/releases/latest/download/legendary.exe";
                    string legendaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "legendary.exe");

                    using (HttpClient client = new HttpClient())
                    {
                        HttpResponseMessage response = await client.GetAsync(legendaryUrl);
                        response.EnsureSuccessStatusCode();
                        using (var fs = new FileStream(legendaryPath, FileMode.Create, FileAccess.Write))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                        var epicManager = new EpicVersionManager();
                        string importCommand = $"import 963137e4c29d4c79a81323b8fab03a40 \"{vanillaMod.InstallPath}\"";
                        await epicManager.RunLegendaryCommandAsync(importCommand);
                    }
                    // Komunikat "informacyjny" po pomyślnym pobraniu
                    //MessageBox.Show("Program legendary.exe dla wersji Epic został pobrany pomyślnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                // Komunikat "błędu" przy niepowodzeniu pobrania
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił błąd podczas pobierania programu legendary.exe: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }


            ConfigureModComponents(modConfigs.Where(x => x.ModName != "AmongUs" && x.ModType != "dll").ToList()); // Exclude DLL mods
        }

        private void UpdateFormDisplay(ModConfiguration modConfig)
        {
            if (modConfig != null)
            {
                if (!string.IsNullOrEmpty(modConfig.InstallPath))
                {
                    textBoxPath.Text = modConfig.InstallPath.Replace('/', '\\');
                }
                else
                {
                    textBoxPath.Text = "";
                }
                labelVersion.Text = "Wersja: " + modConfig.AmongVersion;

                if (!string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath))
                {
                    var modIcon = this.scrollablePanel.Controls.OfType<PictureBox>().FirstOrDefault(icon => icon.Name == $"gameIcon_{modConfig.ModName}");
                    if (modIcon != null)
                    {
                        AddInstalledIcon(modIcon);
                    }
                }

                bool isFullType = string.Equals(modConfig.ModType, "full", StringComparison.OrdinalIgnoreCase);
                bool isVanillaType = string.Equals(modConfig.ModType, "vanilla", StringComparison.OrdinalIgnoreCase);
                btnLaunch.Enabled = (isFullType || isVanillaType) &&
                                    !string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath);

                bool isDllType = string.Equals(modConfig.ModType, "dll", StringComparison.OrdinalIgnoreCase);
                btnModify.Enabled = (isFullType || isDllType) &&
                                    (string.IsNullOrEmpty(modConfig.InstallPath) || !Directory.Exists(modConfig.InstallPath));
                btnDelete.Enabled = modConfigs.Any(m => string.Equals(m.ModType, "full", StringComparison.OrdinalIgnoreCase) &&
                                                        !string.IsNullOrEmpty(m.InstallPath) && Directory.Exists(m.InstallPath)) &&
                                    (isDllType || isFullType);
                btnUpdateMod.Enabled = isFullType && !string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath);
                browseButton.Enabled = (isVanillaType);
            }
            else
            {
                textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                labelVersion.Text = "Wersja: Nieznana";
                btnLaunch.Enabled = false;
                browseButton.Enabled = false;
                btnModify.Enabled = false;
                btnDelete.Enabled = false;
                btnUpdateMod.Enabled = false;
            }
        }

        private PictureBox? selectedIcon = null;
        private Dictionary<PictureBox, Bitmap> originalImages = new Dictionary<PictureBox, Bitmap>();
        private void ConfigureModComponents(List<ModConfiguration> configs)
        {
            int initialY = 5;
            int iconWidth = 64;

            // Obliczenie dynamicznego offsetu
            int numIcons = configs.Count;
            int panelWidth = this.scrollablePanel.ClientSize.Width;

            if (numIcons > 0)
            {
                int totalIconWidth = numIcons * iconWidth;
                int availableSpace = panelWidth - totalIconWidth - 71;
                int offsetX = Math.Max(availableSpace / (numIcons + 1), 5); // Dodaj 1, aby uwzględnić odstępy przed pierwszą i po ostatnią ikoną

                int initialX = 71 + offsetX; // Rozpocznij od 71px plus dynamiczny offset

                ToolTip toolTip = new ToolTip(); // Tworzenie nowego obiektu ToolTip

                foreach (var config in configs.Where(x => x.ModType != "dll")) // Exclude DLL mods
                {
                    try
                    {
                        var imageFile = Path.Combine("Graphics", config.PngFileName);
                        if (!File.Exists(imageFile))
                        {
                            Console.WriteLine($"Pomijanie grafiki: {config.PngFileName} ponieważ plik nie istnieje.");
                            imageFile = Path.Combine("Graphics", "Vanilla.png");
                            if (!File.Exists(imageFile))
                            {
                                MessageBox.Show("Nie znaleziono domyślnego pliku graficznego: Vanilla.png", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                continue;
                            }
                        }
                        Console.WriteLine($"Dodaj ikonę dla: {config.ModName}, PngFileName: {config.PngFileName}");

                        var gameIcon = new PictureBox
                        {
                            Location = new Point(initialX, initialY),
                            Name = $"gameIcon_{config.ModName}",
                            Size = new Size(iconWidth, iconWidth),
                            SizeMode = PictureBoxSizeMode.StretchImage,
                            Image = Image.FromFile(imageFile),
                            Cursor = Cursors.Hand
                        };
                        originalImages[gameIcon] = new Bitmap(gameIcon.Image);

                        if (!string.IsNullOrEmpty(config.Description))
                        {
                            toolTip.SetToolTip(gameIcon, config.Description);
                        }

                        if (!string.IsNullOrEmpty(config.InstallPath) && Directory.Exists(config.InstallPath))
                        {
                            AddInstalledIcon(gameIcon);
                        }

                        gameIcon.Click += GameIcon_Click;
                        this.scrollablePanel.Controls.Add(gameIcon);

                        var labelGame = new Label
                        {
                            Location = new Point(initialX, initialY + iconWidth),
                            Name = $"labelGame_{config.ModName}",
                            Size = new Size(iconWidth, 30),
                            AutoSize = false,
                            MaximumSize = new Size(iconWidth, 30),
                            Text = config.ModName,
                            TextAlign = ContentAlignment.TopCenter
                        };
                        this.scrollablePanel.Controls.Add(labelGame);

                        initialX += iconWidth + offsetX; // Przesunięcie na kolejny start dla następnej ikony
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Problem w czasie ładowania: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddGameIcon(ModConfiguration config)
        {
            try
            {
                var gameIcon = new PictureBox
                {
                    Location = new Point(5, 5),
                    Name = $"gameIcon_{config.ModName}",
                    Size = new Size(64, 64),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Image = Image.FromFile(Path.Combine("Graphics", config.PngFileName)),
                    Cursor = Cursors.Hand
                };
                originalImages[gameIcon] = new Bitmap(gameIcon.Image);
                gameIcon.Click += GameIcon_Click;
                this.scrollablePanel.Controls.Add(gameIcon);

                var labelGame = new Label
                {
                    Location = new Point(5, 69),
                    Name = $"label{config.ModName}",
                    Size = new Size(64, 30),
                    AutoSize = false,
                    MaximumSize = new Size(64, 30),
                    Text = config.ModName,
                    TextAlign = ContentAlignment.TopCenter
                };
                this.scrollablePanel.Controls.Add(labelGame);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Nie znaleziono pliku graficznego: {config.PngFileName}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetGameVersion(string path)
        {
            try
            {
                var exePath = Path.Combine(path, "Among Us.exe");
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                return versionInfo.FileVersion ?? "Nieznana";
            }
            catch
            {
                Console.WriteLine($"Nie udało się odczytać wersji gry.");
                return "Nieznana";
            }
        }

        private void GameIcon_Click(object? sender, EventArgs e)
        {
            var clickedIcon = sender as PictureBox;
            if (clickedIcon != null)
            {
                // Resetowanie wszystkich ikon do ich oryginalnych obrazów
                foreach (var icon in originalImages.Keys)
                {
                    if (icon != clickedIcon)
                    {
                        icon.Image = new Bitmap(originalImages[icon]);
                        if (icon.Image != null)
                        {
                            string tempModName = icon.Name.Replace("gameIcon_", "");
                            var tempModConfig = modConfigs.FirstOrDefault(config => config.ModName == tempModName);
                            if (tempModConfig != null && !string.IsNullOrEmpty(tempModConfig.InstallPath) && Directory.Exists(tempModConfig.InstallPath))
                            {
                                AddInstalledIcon(icon);
                            }
                        }
                        icon.Refresh();
                    }
                }

                // Sprawdzenie statusu instalacji moda
                string clickedModName = clickedIcon.Name.Replace("gameIcon_", "");
                var clickedModConfig = modConfigs.FirstOrDefault(config => config.ModName == clickedModName);
                bool isInstalled = false;

                // Sprawdzenie statusu instalacji na podstawie rodzaju moda
                if (clickedModConfig != null)
                {
                    if (clickedModConfig.ModType == "full" || clickedModConfig.ModType == "Vanilla")
                    {
                        isInstalled = !string.IsNullOrEmpty(clickedModConfig.InstallPath) && Directory.Exists(clickedModConfig.InstallPath);
                    }
                    else if (clickedModConfig.ModType == "dll")
                    {
                        var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath) && Directory.Exists(x.InstallPath)).ToList();
                        isInstalled = fullMods.All(fullMod => File.Exists(Path.Combine(fullMod.InstallPath, clickedModConfig.DllInstallPath ?? string.Empty, $"{clickedModConfig.ModName}.dll")));
                    }
                }

                // Wyznaczenie koloru na podstawie statusu instalacji
                Color color = isInstalled ? Color.Green : Color.Red;

                // Dodanie paska na dole ikony oraz pionowego paska z prawej strony
                if (clickedIcon.Image != null)
                {
                    using (Graphics graphics = Graphics.FromImage(clickedIcon.Image))
                    {
                        Brush brush = new SolidBrush(color);
                        // Pasek na dole
                        graphics.FillRectangle(brush, new Rectangle(0, clickedIcon.Height - 10, clickedIcon.Width, 10));

                        // Pasek z prawej strony
                        graphics.FillRectangle(brush, new Rectangle(clickedIcon.Width - 2, 0, 2, clickedIcon.Height));
                    }
                    clickedIcon.Refresh();
                }

                // Aktualizacja wybranej ikony
                selectedIcon = clickedIcon;
                UpdateFormDisplay(clickedModConfig);
            }
        }



        private async void LaunchButton_Click(object sender, EventArgs e)
        {
            // 1) Walidacja wyboru
            if (selectedIcon == null)
            {
                MessageBox.Show("Nie wybrano wersji gry do uruchomienia.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var modConfig = modConfigs
                .FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
            if (modConfig == null)
            {
                MessageBox.Show("Brak wybranej wersji do uruchomienia.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2) Włączamy UI „busy”
            this.UseWaitCursor = true;
            btnLaunch.Enabled = false;
            progressBarBusy.Visible = true;
            statusLabel.Visible = true;

            // 3) Ustalamy tryb uruchomienia
            string mode = Configuration["Configuration:Mode"] ?? "steam";
            statusLabel.Text = mode == "epic"
                ? "Uruchamiam Epic…"
                : "Uruchamiam Steam…";

            // 4) Czyścimy log w RichTextBox (opcjonalnie)
            txtLegendaryLog.Clear();

            try
            {
                if (mode == "epic")
                {
                    var epicManager = new EpicVersionManager();

                    // 4a) Podpinamy event do przekazywania linii do RichTextBox
                    Action<string> handler = line =>
                    {
                        this.Invoke((Action)(() =>
                        {
                            txtLegendaryLog.AppendText(line + Environment.NewLine);
                            txtLegendaryLog.ScrollToCaret();
                        }));
                    };
                    epicManager.LegendaryOutput += handler;

                    // 4b) Wywołujemy Epic
                    await epicManager.HandleEpicGameAsync(modConfig);

                    // 4c) Odpinamy handler
                    epicManager.LegendaryOutput -= handler;
                }
                else
                {
                    // 5) Obsługa Steam / vanilla
                    string exePath = Path.Combine(modConfig.InstallPath, "Among Us.exe");
                    string steamAppIdPath = Path.Combine(modConfig.InstallPath, "steam_appid.txt");

                    try
                    {
                        File.WriteAllText(steamAppIdPath, "945360");

                        if (File.Exists(exePath))
                        {
                            Process.Start(new ProcessStartInfo("steam://") { UseShellExecute = true });
                            Process.Start(exePath);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Nie znaleziono pliku Among Us.exe w wybranej ścieżce.",
                                "Błąd",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Problem z utworzeniem pliku steam_appid.txt: {ex.Message}. " +
                            "Próba uruchomienia przez Steam URI.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        try
                        {
                            Process.Start(
                                new ProcessStartInfo("steam://rungameid/945360")
                                {
                                    UseShellExecute = true
                                });
                        }
                        catch (Exception uriEx)
                        {
                            MessageBox.Show(
                                $"Failed to launch game via Steam URI: {uriEx.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
            finally
            {
                // 6) Wyłączamy UI „busy”
                this.UseWaitCursor = false;
                btnLaunch.Enabled = true;
                progressBarBusy.Visible = false;
                statusLabel.Visible = false;
            }
        }

        private async void ModifyButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null)
                {
                    string mode = Configuration["Configuration:Mode"] ?? "steam"; // Default to steam
                    bool modificationSuccess = false;
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressLabel.Visible = true;
                    if (modConfig.ModType == "full")
                    {
                        if (mode == "steam")
                        {
                            ModManager manager = new ModManager(Configuration);
                            await manager.ModifyAsync(modConfig, modConfigs, progressBar, progressLabel, mode);
                            modificationSuccess = true;
                        }
                        else if (mode == "epic")
                        {
                            await ModifyEpicAndUpdateUI(modConfig);
                            modificationSuccess = true;
                        }
                    }
                    else if (modConfig.ModType == "dll")
                    {
                        var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath)).ToList();
                        using var modSelector = new ModSelectorForm(fullMods);
                        if (modSelector.ShowDialog() == DialogResult.OK)
                        {
                            var selectedMods = modSelector.SelectedMods;
                            ModManager manager = new ModManager(Configuration);
                            await manager.ModifyDllAsync(modConfig, selectedMods, progressBar, progressLabel);
                            modificationSuccess = true;
                        }
                    }

                    progressBar.Visible = false;
                    progressLabel.Visible = false;

                    if (modificationSuccess)
                    {
                        // Zaktualizuj UI i ikonę tylko po zakończeniu zadania
                        UpdateFormDisplay(modConfig);
                        RefreshSingleIcon(selectedIcon);
                    }
                }
                else
                {
                    MessageBox.Show("Nie można znaleźć konfiguracji dla wybranego moda.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano żadnej ikony do modyfikacji.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Wskaż plik Among Us.exe",
                Filter = "Plik wykonywalny Among Us|Among Us.exe"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFilePath = dialog.FileName;
                if (selectedFilePath != null && Path.GetFileName(selectedFilePath) == "Among Us.exe")
                {
                    var directoryName = Path.GetDirectoryName(selectedFilePath);
                    if (directoryName != null)
                    {
                        string detectedMode = DetectGameMode(directoryName);

                        var existingConfig = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs");
                        if (existingConfig != null)
                        {
                            existingConfig.InstallPath = directoryName.Replace('/', '\\');
                            existingConfig.AmongVersion = "2020.3.45.6687953";
                        }
                        else
                        {
                            var newConfig = new ModConfiguration
                            {
                                ModName = "AmongUs",
                                PngFileName = "Vanilla.png",
                                InstallPath = directoryName.Replace('/', '\\'),
                                GitHubRepoOrLink = null,
                                ModType = "Vanilla",
                                DllInstallPath = null,
                                ModVersion = "",
                                LastUpdated = null,
                                AmongVersion = "2020.3.45.6687953"
                            };
                            modConfigs.Add(newConfig);
                        }

                        // Zapis trybu w konfiguracji
                        Configuration["Configuration:Mode"] = detectedMode;
                        ConfigManager.SaveConfig(modConfigs);
                        ConfigManager.SaveConfigurationSetting("Mode", detectedMode);

                        MessageBox.Show($"Ścieżka do Among Us zapisana jako wersja {detectedMode}. Aplikacja zostanie ponownie uruchomiona.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Ponowne uruchomienie aplikacji
                        RestartApplication();
                    }
                    else
                    {
                        MessageBox.Show("Nie można uzyskać katalogu docelowego.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Proszę wybrać prawidłowy plik Among Us.exe.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string DetectGameMode(string directoryName)
        {
            // Sprawdzenie Epic
            if (Directory.Exists(Path.Combine(directoryName, ".egstore")))
            {
                return "epic";
            }

            // Domyślnie zakładamy Steam, jeśli brak bardziej specyficznych wskaźników dla Epic
            return "steam";
        }


        private void DisplayGameVersion(string path)
        {
            try
            {
                var exePath = Path.Combine(path, "Among Us.exe");
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                labelVersion.Text = "Wersja: " + versionInfo.FileVersion;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się odczytać wersji gry. " + ex.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelVersion.Text = "Wersja: Nieznana";
            }
        }

        

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null && btnDelete.Enabled)
                {
                    ModDelete.DeleteMod(modConfig, modConfigs);
                    UpdateFormDisplay(modConfig);
                    RefreshSingleIcon(selectedIcon);  // Odśwież tylko wybraną ikonę
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano żadnej ikony do usunięcia.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void UpdateModButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null)
                {
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = 0;
                    string mode = Configuration["Configuration:Mode"] ?? "steam"; // Default to steam

                    if (modConfig.ModType == "full")
                    {
                        if (mode == "steam")
                        {
                            await ModUpdates.UpdateModAsync(modConfig, modConfigs, progressBar, progressLabel, Configuration); // Użyj Configuration jako piątego argumentu
                        }
                        else if (mode == "epic")
                        {
                            ModDelete.DeleteMod(modConfig, modConfigs);
                            await ModifyEpicAndUpdateUI(modConfig);
                        }
                    }

                    progressBar.Visible = false;
                    UpdateFormDisplay(modConfig);
                    RefreshSingleIcon(selectedIcon);
                }
                else
                {
                    MessageBox.Show("Nie można znaleźć konfiguracji dla wybranego moda.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano żadnej ikony do aktualizacji.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddInstalledIcon(PictureBox gameIcon)
        {
            var installedImagePath = Path.Combine("Graphics", "installed.png");
            if (File.Exists(installedImagePath))
            {
                using (var installedImage = Image.FromFile(installedImagePath))
                using (var graphics = Graphics.FromImage(gameIcon.Image))
                {
                    graphics.DrawImage(installedImage, new Rectangle(0, 0, installedImage.Width, installedImage.Height));
                }
                gameIcon.Refresh();
            }
        }

        private void RefreshSingleIcon(PictureBox gameIcon)
        {
            string modName = gameIcon.Name.Replace("gameIcon_", "");
            var modConfig = modConfigs.FirstOrDefault(config => config.ModName == modName);

            bool isInstalled = false;

            if (modConfig != null)
            {
                if (modConfig.ModType == "full" || modConfig.ModType == "Vanilla")
                {
                    isInstalled = !string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath);
                }
                else if (modConfig.ModType == "dll")
                {
                    var fullMods = modConfigs
                        .Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath) && Directory.Exists(x.InstallPath))
                        .ToList();
                    isInstalled = fullMods.All(fullMod => File.Exists(Path.Combine(fullMod.InstallPath, modConfig.DllInstallPath ?? string.Empty, $"{modConfig.ModName}.dll")));
                }
            }

            Color color = isInstalled ? Color.Green : Color.Red;

            if (gameIcon.Image != null)
            {
                using (Graphics graphics = Graphics.FromImage(gameIcon.Image))
                {
                    Brush brush = new SolidBrush(color);
                    // Pasek na dole
                    graphics.FillRectangle(brush, new Rectangle(0, gameIcon.Height - 10, gameIcon.Width, 10));

                    // Pasek z prawej strony
                    graphics.FillRectangle(brush, new Rectangle(gameIcon.Width - 2, 0, 2, gameIcon.Height));
                }
                gameIcon.Refresh();
            }
        }

    }

}
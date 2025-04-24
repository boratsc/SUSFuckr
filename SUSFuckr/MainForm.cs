using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using Microsoft.Extensions.Configuration;

namespace SUSFuckr
{
    public partial class MainForm : Form
    {
        

        private List<ModConfiguration> modConfigs;
        private Label progressLabel;
        private MenuStrip menuStrip = new MenuStrip();
        private Panel overlayPanel = new Panel();
        private readonly IConfiguration Configuration;
        private readonly string appVersion = string.Empty;  // Dodaj pole appVersion

        private ToolTip toolTip;

        public MainForm()
        {
            InitializeComponent();
            CreateMenu();
                toolTip = new ToolTip();
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
            appVersion = Configuration["Configuration:CurrentVersion"] ?? "0.0.1"; // Inicjalizacja

            Text = $"SUSFuckr - przyjazny instalator mod�w {appVersion}"; // U�ycie `appVersion`

            Width = 640;
            Height = 520;
            Icon = new Icon("Graphics/icon.ico");

            toolTip = new ToolTip();

            modConfigs = ConfigManager.LoadConfig();
            Load += FormLoad;

            // Inicjalizacj� progressLabel
            progressLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(progressBar.Width / 2 - 50, progressBar.Height / 2 - 10),
            };
            progressBar.Controls.Add(progressLabel); // Dodaj progressLabel do paska post�pu

        }

        private void CreateMenu()
        {
            menuStrip = new MenuStrip();
            ToolStripMenuItem additionalActionsMenuItem = new ToolStripMenuItem("Dodatkowe akcje");
            ToolStripMenuItem fixBlackScreenItem = new ToolStripMenuItem("Napraw czarny ekran");
            fixBlackScreenItem.Click += new EventHandler(FixBlackScreenMenuItem_Click);
            additionalActionsMenuItem.DropDownItems.Add(fixBlackScreenItem);

            ToolStripMenuItem updateConfigItem = new ToolStripMenuItem("Aktualizuj konfiguracj�");
            updateConfigItem.Click += new EventHandler(UpdateConfigMenuItem_Click);
            additionalActionsMenuItem.DropDownItems.Add(updateConfigItem);

            menuStrip.Items.Add(additionalActionsMenuItem);

            ToolStripMenuItem infoMenuItem = new ToolStripMenuItem("Informacje");
            infoMenuItem.Click += new EventHandler(InfoMenuItem_Click);
            menuStrip.Items.Add(infoMenuItem);

            ToolStripMenuItem supportMenuItem = new ToolStripMenuItem("Donate na certyfikat");
            supportMenuItem.Click += (s, ev) => Process.Start(new ProcessStartInfo
            {
                FileName = "https://liberapay.com/boracik/donate",
                UseShellExecute = true
            });

            menuStrip.Items.Add(supportMenuItem);

            // Ustawienie tooltipa na hover nad supportMenuItem
            supportMenuItem.MouseHover += (s, ev) => toolTip.Show("Zbieram hajs, �eby Windows si� nie plu�, �e aplikacja jest niebezpieczna!", menuStrip, MousePosition.X - this.Location.X, MousePosition.Y - this.Location.Y, 2000);


            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }


        private async void UpdateConfigMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                // �ci�ganie nowego pliku konfiguracji
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

                MessageBox.Show("Konfiguracja zosta�a zaktualizowana. Aplikacja zostanie teraz ponownie uruchomiona.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Zamknij i otw�rz aplikacj� ponownie
                RestartApplication();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B��d podczas aktualizacji konfiguracji: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            ShowInfoOverlay();
        }

        private void ShowInfoOverlay()
        {
            contentPanel.Visible = false;
            overlayPanel = Information.CreateInfoOverlay(this, appVersion, RemoveInfoOverlay); // Poprawne u�ycie `appVersion`
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront();
        }

        private void RemoveInfoOverlay()
        {
            if (overlayPanel != null)
            {
                this.Controls.Remove(overlayPanel);
                contentPanel.Visible = true;  // Przywr�� widoczno�� contentPanel
            }
        }

        private void FixBlackScreenMenuItem_Click(object? sender, EventArgs e)
        {
            FixBlackScreen.ExecuteFix();
        }


        private void ReturnToMain()
        {
            return;
        }

        private void FormLoad(object? sender, EventArgs e)
        {
            // Najpierw pobierz informacje z konfiguracji
            var vanillaMod = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs");
            if (vanillaMod != null && !string.IsNullOrEmpty(vanillaMod.InstallPath) && Directory.Exists(vanillaMod.InstallPath)) // Sprawdzenie czy Vanilla jest zainstalowane
            {
                textBoxPath.Text = vanillaMod.InstallPath.Replace('/', '\\');
                labelVersion.Text = "Wersja: " + vanillaMod.AmongVersion;
                AddGameIcon(vanillaMod);  // Dodaj ikon� Vanilla

                var vanillaIcon = this.contentPanel.Controls.OfType<PictureBox>().FirstOrDefault(icon => icon.Name == $"gameIcon_{vanillaMod.ModName}");
                if (vanillaIcon != null)
                {
                    AddInstalledIcon(vanillaIcon);  // Dodaj grafika 'installed.png'
                }
            }
            else
            {
                var path = GameLocator.TryFindAmongUsPath();
                var version = path != null ? GetGameVersion(path) : "Nieznana";
                if (path != null)
                {
                    textBoxPath.Text = path.Replace('/', '\\');
                    labelVersion.Text = "Wersja: " + version;
                }
                else
                {
                    textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                    labelVersion.Text = "Wersja: Nieznana";
                }

                if (vanillaMod == null)
                {
                    vanillaMod = new ModConfiguration
                    {
                        ModName = "AmongUs",
                        PngFileName = "Vanilla.png",
                        InstallPath = path ?? string.Empty,
                        GitHubRepoOrLink = null,
                        ModType = "Vanilla",
                        DllInstallPath = null,
                        LastUpdated = null,
                        AmongVersion = version,
                    };
                    modConfigs.Add(vanillaMod);
                    ConfigManager.SaveConfig(modConfigs);
                    AddGameIcon(vanillaMod);

                    var vanillaIcon = this.contentPanel.Controls.OfType<PictureBox>().FirstOrDefault(icon => icon.Name == $"gameIcon_{vanillaMod.ModName}");
                    if (vanillaIcon != null)
                    {
                        AddInstalledIcon(vanillaIcon);
                    }
                }
            }

            ConfigureModComponents(modConfigs.Where(x => x.ModName != vanillaMod.ModName).ToList());
        }
       
        private void UpdateFormDisplay(ModConfiguration modConfig)
        {
            if (modConfig != null)
            {
                textBoxPath.Text = modConfig.InstallPath.Replace('/', '\\');
                labelVersion.Text = "Wersja: " + modConfig.AmongVersion;

                // Sprawd�, czy mod jest zainstalowany i dodaj ikon� "installed.png"
                if (!string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath))
                {
                    var modIcon = this.contentPanel.Controls.OfType<PictureBox>().FirstOrDefault(icon => icon.Name == $"gameIcon_{modConfig.ModName}");
                    if (modIcon != null)
                    {
                        AddInstalledIcon(modIcon);  // Dodaj grafik� 'installed.png' do ikony modu
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

        private PictureBox? selectedIcon = null; // Przechowuje aktualnie wybran� ikon�
        private Dictionary<PictureBox, Bitmap> originalImages = new Dictionary<PictureBox, Bitmap>(); // Przechowuje oryginalne obrazy

        private void ConfigureModComponents(List<ModConfiguration> configs)
        {
            int initialX = 94;
            int initialY = 20;
            int iconWidth = 64;
            int offsetX = 10;

            foreach (var config in configs)
            {
                try
                {
                    var imageFile = Path.Combine("Graphics", config.PngFileName);
                    if (!File.Exists(imageFile))
                    {
                        // Je�li plik ikony z konfiguracji nie istnieje, u�yj "Vanilla.png"
                        Console.WriteLine($"Pomijanie grafiki: {config.PngFileName} poniewa� plik nie istnieje.");
                        imageFile = Path.Combine("Graphics", "Vanilla.png");

                        // Wywal komunikat je�li "Vanilla.png" r�wnie� nie istnieje
                        if (!File.Exists(imageFile))
                        {
                            MessageBox.Show("Nie znaleziono domy�lnego pliku graficznego: Vanilla.png", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue; // Przeskocz ten mod w takim przypadku
                        }
                    }

                    Console.WriteLine($"Dodaj ikon� dla: {config.ModName}, PngFileName: {config.PngFileName}");

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

                    // Je�li mod jest zainstalowany, dodaj grafik� installed.png
                    if (!string.IsNullOrEmpty(config.InstallPath) && Directory.Exists(config.InstallPath))
                    {
                        AddInstalledIcon(gameIcon);
                    }


                    gameIcon.Click += GameIcon_Click;
                    this.contentPanel.Controls.Add(gameIcon);

                    var labelGame = new Label
                    {
                        Location = new Point(initialX, initialY + iconWidth),
                        Name = $"labelGame_{config.ModName}",
                        Size = new Size(iconWidth, 60),
                        AutoSize = false,
                        MaximumSize = new Size(iconWidth, 60),
                        Text = config.ModName,
                        TextAlign = ContentAlignment.TopCenter
                    };
                    this.contentPanel.Controls.Add(labelGame);
                    initialX += iconWidth + offsetX;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Problem w czasie �adowania: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                Console.WriteLine($"Nie uda�o si� odczyta� wersji gry.");
                return "Nieznana";
            }
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
                MessageBox.Show("Nie uda�o si� odczyta� wersji gry. " + ex.Message, "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelVersion.Text = "Wersja: Nieznana";
            }
        }




        private void GameIcon_Click(object? sender, EventArgs e)
        {
            var clickedIcon = sender as PictureBox;
            if (clickedIcon != null)
            {
                foreach (var icon in originalImages.Keys)
                {
                    if (icon != clickedIcon)
                    {
                        icon.Image = new Bitmap(originalImages[icon]);
                        if (icon.Image != null)
                        {
                            string tempModName = icon.Name.Replace("gameIcon_", ""); // U�yj innej nazwy dla lokalnej zmiennej
                            var tempModConfig = modConfigs.FirstOrDefault(config => config.ModName == tempModName);
                            if (tempModConfig != null && !string.IsNullOrEmpty(tempModConfig.InstallPath) && Directory.Exists(tempModConfig.InstallPath))
                            {
                                AddInstalledIcon(icon);
                            }
                        }
                        icon.Refresh();
                    }
                }

                if (clickedIcon.Image != null)
                {
                    using (Graphics graphics = Graphics.FromImage(clickedIcon.Image))
                    {
                        Brush brush = new SolidBrush(Color.Green);
                        graphics.FillEllipse(brush, new Rectangle(clickedIcon.Width - 16, clickedIcon.Height - 16, 16, 16));
                    }
                    clickedIcon.Refresh();
                }

                selectedIcon = clickedIcon;

                string clickedModName = clickedIcon.Name.Replace("gameIcon_", ""); // U�yj innej nazwy dla lokalnej zmiennej
                var clickedModConfig = modConfigs.FirstOrDefault(config => config.ModName == clickedModName);

                if (clickedModConfig != null && !string.IsNullOrEmpty(clickedModConfig.InstallPath) && Directory.Exists(clickedModConfig.InstallPath))
                {
                    AddInstalledIcon(clickedIcon);
                }

                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null)
                {
                    UpdateFormDisplay(modConfig);
                }
            }
        }


        private void LaunchButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                // Pobierz konfiguracj� moda odpowiadaj�cego za wybran� ikon�
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);

                if (modConfig != null)
                {
                    var exePath = Path.Combine(modConfig.InstallPath, "Among Us.exe");

                    if (File.Exists(exePath))
                    {
                        try
                        {
                            Process.Start(exePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to launch game: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie znaleziono pliku Among Us.exe w wybranej �cie�ce.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Brak wybranej wersji do uruchomienia.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano wersji gry do uruchomienia.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ModifyButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null)
                {
                    ModManager manager = new ModManager(Configuration);
                    bool modificationSuccess = false; // Flaga sukcesu operacji

                    if (modConfig.ModType == "full")
                    {
                        progressBar.Visible = true; // Poka� pasek post�pu
                        progressBar.Style = ProgressBarStyle.Continuous; // Zmie� na ci�g�y styl

                        await manager.ModifyAsync(modConfig, modConfigs, progressBar, progressLabel);
                        modificationSuccess = true;
                    }
                    else if (modConfig.ModType == "dll")
                    {
                        var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath)).ToList();
                        using var modSelector = new ModSelectorForm(fullMods);

                        if (modSelector.ShowDialog() == DialogResult.OK)
                        {
                            var selectedMods = modSelector.SelectedMods;

                            progressBar.Visible = true; // Poka� pasek post�pu dla DLL
                            await manager.ModifyDllAsync(modConfig, selectedMods, progressBar, progressLabel);
                            modificationSuccess = true;
                        }
                    }

                    progressBar.Visible = false; // Ukryj pasek post�pu po zako�czeniu
                    progressLabel.Visible = false; // Ukryj etykiet� po zako�czeniu pobierania

                    // Od�wie� formularz, je�li modyfikacja zako�czy�a si� sukcesem
                    if (modificationSuccess)
                    {
                        UpdateFormDisplay(modConfig);
                        contentPanel.Refresh();
                    }
                }
                else
                {
                    MessageBox.Show("Nie mo�na znale�� konfiguracji dla wybranego moda.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano �adnej ikony do modyfikacji.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Wska� katalog gry Among Us (Steam)";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = dialog.SelectedPath;
                if (File.Exists(Path.Combine(selectedPath, "Among Us.exe")))
                {
                    textBoxPath.Text = selectedPath.Replace('/', '\\');
                    DisplayGameVersion(selectedPath);
                }
                else
                {
                    MessageBox.Show("To nie wygl�da na folder gry Among Us.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    labelVersion.Text = "Wersja: Nieznana";
                }
            }
        }
        private void AddGameIcon(ModConfiguration config)
        {
            try
            {
                var gameIcon = new PictureBox
                {
                    Location = new Point(20, 20),
                    Name = $"gameIcon_{config.ModName}",
                    Size = new Size(64, 64),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Image = Image.FromFile(Path.Combine("Graphics", config.PngFileName)),
                    Cursor = Cursors.Hand
                };
                originalImages[gameIcon] = new Bitmap(gameIcon.Image);
                gameIcon.Click += GameIcon_Click;
                this.contentPanel.Controls.Add(gameIcon); // Dodaj do panelu

                var labelGame = new Label
                {
                    Location = new Point(20, 84),
                    Name = $"label{config.ModName}",
                    Size = new Size(64, 60),
                    AutoSize = false,
                    MaximumSize = new Size(64, 60),
                    Text = config.ModName,
                    TextAlign = ContentAlignment.TopCenter
                };
                this.contentPanel.Controls.Add(labelGame); // Dodaj do panelu
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Nie znaleziono pliku graficznego: {config.PngFileName}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    // Od�wie� formularz po usuni�ciu
                    UpdateFormDisplay(modConfig);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano �adnej ikony do usuni�cia.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    progressBar.Style = ProgressBarStyle.Continuous; // Zmie� na sta�y styl, by wy�wietla� post�p
                    progressBar.Value = 0; // Zresetuj pasek post�pu

                    await ModUpdates.UpdateModAsync(modConfig, modConfigs, progressBar, progressLabel, Configuration); // Przekazanie Configuration
                    progressBar.Visible = false; // Ukryj pasek post�pu po zako�czeniu

                    // Od�wie� formularz po aktualizacji
                    UpdateFormDisplay(modConfig);
                }
                else
                {
                    MessageBox.Show("Nie mo�na znale�� konfiguracji dla wybranego moda.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano �adnej ikony do aktualizacji.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                gameIcon.Refresh(); // Od�wie� dla pewno�ci wy�wietlenia
            }
        }
    }
}
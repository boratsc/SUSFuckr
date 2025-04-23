using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
namespace SUSFuckr
{
    public partial class MainForm : Form
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle bounds = contentPanel.Bounds;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(bounds.Left, bounds.Top, 20, 20, 180, 90);
                path.AddArc(bounds.Right - 20, bounds.Top, 20, 20, 270, 90);
                path.AddArc(bounds.Right - 20, bounds.Bottom - 20, 20, 20, 0, 90);
                path.AddArc(bounds.Left, bounds.Bottom - 20, 20, 20, 90, 90);
                path.CloseFigure();
                contentPanel.Region = new Region(path);
            }
        }
        private List<ModConfiguration> modConfigs;

        public MainForm()
        {
            InitializeComponent();
            Text = "SUSFuckr ver. 0.0.0.1";
            Width = 640;
            Height = 520;
            modConfigs = ConfigManager.LoadConfig();
            Load += FormLoad; // Dodaj wydarzenie ³adowania formularza
        }

        private void FormLoad(object? sender, EventArgs e)
        {
            // Najpierw pobierz informacje z konfiguracji
            var vanillaMod = modConfigs.FirstOrDefault(x => x.ModName == "AmongUs");
            if (vanillaMod != null && !string.IsNullOrEmpty(vanillaMod.InstallPath))
            {
                textBoxPath.Text = vanillaMod.InstallPath.Replace('/', '\\');
                labelVersion.Text = "Wersja gry: " + vanillaMod.AmongVersion;
            }
            else
            {
                // Dopiero potem poszukaj gry
                var path = GameLocator.TryFindAmongUsPath();
                var version = path != null ? GetGameVersion(path) : "Nieznana";

                if (path != null)
                {
                    textBoxPath.Text = path.Replace('/', '\\');
                    labelVersion.Text = "Wersja gry: " + version;
                }
                else
                {
                    textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                    labelVersion.Text = "Wersja gry: Nieznana";
                }

                // Aktualizuj lub dodaj Vanilla jeœli nie ma
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
                }
            }

            // Wyœwietl 'Vanilla' jako pierwsza
            if (vanillaMod != null)
            {
                AddGameIcon(vanillaMod);
            }
            ConfigureModComponents(modConfigs.Where(x => x.ModName != vanillaMod.ModName).ToList());
        }

        private void UpdateFormDisplay(ModConfiguration modConfig)
        {
            if (modConfig != null)
            {
                textBoxPath.Text = modConfig.InstallPath.Replace('/', '\\');
                labelVersion.Text = "Wersja gry: " + modConfig.AmongVersion;

                // Przycisk 'Uruchom' powinien byæ aktywny tylko dla modów typu "full" i istniejacy katalog
                bool isFullType = string.Equals(modConfig.ModType, "full", StringComparison.OrdinalIgnoreCase);
                bool isVanillaType = string.Equals(modConfig.ModType, "vanilla", StringComparison.OrdinalIgnoreCase);

                btnLaunch.Enabled = (isFullType || isVanillaType) &&
                                    !string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath);

                // SprawdŸ, czy mod jest zainstalowany i odpowiedni typ, lub jeœli jest dll, czy s¹ mody do usuniêcia
                bool isDllType = string.Equals(modConfig.ModType, "dll", StringComparison.OrdinalIgnoreCase);

                btnModify.Enabled = (isFullType || isDllType) &&
                                    (string.IsNullOrEmpty(modConfig.InstallPath) || !Directory.Exists(modConfig.InstallPath));

                btnDelete.Enabled = modConfigs.Any(m => string.Equals(m.ModType, "full", StringComparison.OrdinalIgnoreCase) &&
                                                        !string.IsNullOrEmpty(m.InstallPath) && Directory.Exists(m.InstallPath)) &&
                                    (isDllType || isFullType);
                btnUpdateMod.Enabled = isFullType && !string.IsNullOrEmpty(modConfig.InstallPath) && Directory.Exists(modConfig.InstallPath);

            }
            else
            {
                textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                labelVersion.Text = "Wersja gry: Nieznana";
                btnLaunch.Enabled = false;
                btnModify.Enabled = false;
                btnDelete.Enabled = false;
                btnUpdateMod.Enabled = false;
            }
        }

        private PictureBox selectedIcon = null; // Przechowuje aktualnie wybran¹ ikonê
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
                        // Jeœli plik ikony z konfiguracji nie istnieje, u¿yj "Vanilla.png"
                        Console.WriteLine($"Pomijanie grafiki: {config.PngFileName} poniewa¿ plik nie istnieje.");
                        imageFile = Path.Combine("Graphics", "Vanilla.png");

                        // Wywal komunikat jeœli "Vanilla.png" równie¿ nie istnieje
                        if (!File.Exists(imageFile))
                        {
                            MessageBox.Show("Nie znaleziono domyœlnego pliku graficznego: Vanilla.png", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue; // Przeskocz ten mod w takim przypadku
                        }
                    }

                    Console.WriteLine($"Dodaj ikonê dla: {config.ModName}, PngFileName: {config.PngFileName}");

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
                    MessageBox.Show($"Problem w czasie ³adowania: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string GetGameVersion(string path)
        {
            try
            {
                var exePath = Path.Combine(path, "Among Us.exe");
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                return versionInfo.FileVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Nie uda³o siê odczytaæ wersji gry: {ex.Message}");
                return "Nieznana";
            }
        }
        private void DisplayGameVersion(string path)
        {
            try
            {
                var exePath = Path.Combine(path, "Among Us.exe");
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                labelVersion.Text = "Wersja gry: " + versionInfo.FileVersion;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie uda³o siê odczytaæ wersji gry. " + ex.Message, "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                labelVersion.Text = "Wersja gry: Nieznana";
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

                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null)
                {
                    UpdateFormDisplay(modConfig);  // Zaktualizuj wyœwietlanie formularza i stan przycisku "Modyfikuj"
                }
            }
        }


        private void LaunchButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                // Pobierz konfiguracjê moda odpowiadaj¹cego za wybran¹ ikonê
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
                        MessageBox.Show("Nie znaleziono pliku Among Us.exe w wybranej œcie¿ce.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Brak wybranej wersji do uruchomienia.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano wersji gry do uruchomienia.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ModifyButton_Click(object sender, EventArgs e)
        {
            if (selectedIcon != null)
            {
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);
                if (modConfig != null)
                {
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    progressBar.MarqueeAnimationSpeed = 30;

                    ModManager manager = new ModManager();
                    bool modificationSuccess = false;  // Flaga sukcesu operacji

                    if (modConfig.ModType == "full")
                    {
                        await manager.ModifyAsync(modConfig, modConfigs);
                        modificationSuccess = true;
                    }
                    else if (modConfig.ModType == "dll")
                    {
                        var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath)).ToList();
                        using var modSelector = new ModSelectorForm(fullMods);
                        if (modSelector.ShowDialog() == DialogResult.OK)
                        {
                            var selectedMods = modSelector.SelectedMods;
                            await manager.ModifyDllAsync(modConfig, selectedMods);
                            modificationSuccess = true;
                        }
                    }

                    progressBar.Visible = false;

                    // Odœwie¿ formularz, jeœli modyfikacja zakoñczy³a siê sukcesem
                    if (modificationSuccess)
                    {
                        UpdateFormDisplay(modConfig);
                    }
                }
                else
                {
                    MessageBox.Show("Nie mo¿na znaleŸæ konfiguracji dla wybranego moda.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano ¿adnej ikony do modyfikacji.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Wska¿ katalog gry Among Us (Steam)";
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
                    MessageBox.Show("To nie wygl¹da na folder gry Among Us.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    labelVersion.Text = "Wersja gry: Nieznana";
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
                MessageBox.Show($"Nie znaleziono pliku graficznego: {config.PngFileName}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    // Odœwie¿ formularz po usuniêciu
                    UpdateFormDisplay(modConfig);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano ¿adnej ikony do usuniêcia.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    progressBar.Style = ProgressBarStyle.Marquee;
                    progressBar.MarqueeAnimationSpeed = 30;

                    await ModUpdates.UpdateModAsync(modConfig, modConfigs);

                    progressBar.Visible = false;

                    // Odœwie¿ formularz po aktualizacji
                    UpdateFormDisplay(modConfig);
                }
                else
                {
                    MessageBox.Show("Nie mo¿na znaleŸæ konfiguracji dla wybranego moda.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie wybrano ¿adnej ikony do aktualizacji.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace SUSFuckr
{
    public partial class MainForm : Form
    {

        private List<ModConfiguration> modConfigs;

        public MainForm()
        {
            InitializeComponent();
            Text = "SUSFuckr";
            Width = 600;
            Height = 400;

            // Za³aduj istniej¹c¹ konfiguracjê
            modConfigs = ConfigManager.LoadConfig();

            Load += FormLoad; // Dodaj wydarzenie ³adowania formularza
        }

        private void FormLoad(object? sender, EventArgs e)
        {
            // Szukaj niezmodyfikowanej wersji Among Us jeœli nie ma jej w konfiguracji
            var path = GameLocator.TryFindAmongUsPath();
            if (path != null)
            {
                textBoxPath.Text = path.Replace('/', '\\');
                DisplayGameVersion(path);
            }
            else
            {
                textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                labelVersion.Text = "Wersja gry: Nieznana";
            }

            var version = path != null ? GetGameVersion(path) : "Nieznana";
            var vanillaMod = modConfigs.FirstOrDefault(x => x.ModType == "Vanilla");
            if (vanillaMod == null || string.IsNullOrEmpty(vanillaMod.InstallPath))
            {
                modConfigs.Add(new ModConfiguration
                {
                    ModName = "AmongUs",
                    PngFileName = "Vanilla.png",
                    InstallPath = path ?? string.Empty,
                    GitHubRepoOrLink = null,
                    ModType = "Vanilla",
                    DllInstallPath = null,
                    LastUpdated = null,
                    AmongVersion = version,
                });
                ConfigManager.SaveConfig(modConfigs);
            }

            // Wyœwietl "vanilla" jako pierwsza
            vanillaMod = modConfigs.FirstOrDefault(x => x.ModType == "Vanilla");
            if (vanillaMod != null)
            {
                AddGameIcon(vanillaMod);
            }
            ConfigureModComponents(modConfigs.Where(x => x != vanillaMod).ToList());
        }

        private PictureBox selectedIcon = null; // Przechowuje aktualnie wybran¹ ikonê
        private Dictionary<PictureBox, Bitmap> originalImages = new Dictionary<PictureBox, Bitmap>(); // Przechowuje oryginalne obrazy

        private void ConfigureModComponents(List<ModConfiguration> configs)
        {
            int initialX = 94;
            int initialY = 20;
            int offsetX = 10;
            foreach (var config in configs)
            {
                try
                {
                    var imageFile = $"Graphics/{config.PngFileName}";
                    var gameIcon = new PictureBox
                    {
                        Location = new Point(initialX, initialY),
                        Name = $"gameIcon_{config.ModName}",
                        Size = new Size(64, 64),
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        Image = Image.FromFile(imageFile),
                        Cursor = Cursors.Hand
                    };
                    originalImages[gameIcon] = new Bitmap(gameIcon.Image); // Zapisz oryginalny obrazek
                    gameIcon.Click += GameIcon_Click;
                    this.Controls.Add(gameIcon);

                    var labelGame = new Label
                    {
                        Location = new Point(initialX, initialY + 64),
                        Name = $"labelGame_{config.ModName}",
                        Size = new Size(64, 60),
                        AutoSize = false,
                        MaximumSize = new Size(64, 60),
                        Text = config.ModName,
                        TextAlign = ContentAlignment.TopCenter
                    };
                    this.Controls.Add(labelGame);
                }
                catch (FileNotFoundException)
                {
                    MessageBox.Show($"Nie znaleziono pliku graficznego: {config.PngFileName}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                initialX += 64 + offsetX;
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
                Console.WriteLine($"Nie uda³o siê odczytaæ wersji gry: {ex.Message}");
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
                // Usuñ zielone znaczniki z innych ikonek
                foreach (var icon in originalImages.Keys)
                {
                    if (icon != clickedIcon)
                    {
                        icon.Image = new Bitmap(originalImages[icon]); // Przywróæ oryginalny obrazek
                        icon.Refresh();
                    }
                }

                // Dodanie zielonego znacznika do wybranej ikony
                if (clickedIcon.Image != null)
                {
                    using (Graphics graphics = Graphics.FromImage(clickedIcon.Image))
                    {
                        Brush brush = new SolidBrush(Color.Green);
                        graphics.FillEllipse(brush, new Rectangle(clickedIcon.Width - 16, clickedIcon.Height - 16, 16, 16));
                    }
                    clickedIcon.Refresh();
                }

                selectedIcon = clickedIcon; // Aktualizuj obecnie wybran¹ ikonê

                // Pobierz konfiguracjê moda odpowiadaj¹cego za wybran¹ ikonê
                var modConfig = modConfigs.FirstOrDefault(config => $"gameIcon_{config.ModName}" == selectedIcon.Name);

                if (modConfig != null)
                {
                    // Aktywuj przycisk tylko dla typów Full i Vanilla
                    btnLaunch.Enabled = modConfig.ModType == "full" || modConfig.ModType == "Vanilla";
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
                    Image = Image.FromFile($"Graphics/{config.PngFileName}"),
                    Cursor = Cursors.Hand
                };
                originalImages[gameIcon] = new Bitmap(gameIcon.Image);
                gameIcon.Click += GameIcon_Click;
                this.Controls.Add(gameIcon);

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
                this.Controls.Add(labelGame);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Nie znaleziono pliku graficznego: {config.PngFileName}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
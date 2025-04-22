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
            Text = "SUSFuckr";
            Width = 620;
            Height = 440;
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

        private void UpdateFormDisplay(ModConfiguration config)
        {
            if (config != null)
            {
                textBoxPath.Text = config.InstallPath.Replace('/', '\\');
                labelVersion.Text = "Wersja gry: " + config.AmongVersion;
            }
            else
            {
                textBoxPath.Text = "Nie znaleziono Among Us automatycznie.";
                labelVersion.Text = "Wersja gry: Nieznana";
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
    }
}
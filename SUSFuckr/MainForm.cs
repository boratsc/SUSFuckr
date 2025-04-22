using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SUSFuckr
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Text = "SUSFuckr";
            Width = 600;
            Height = 400; // Zwiêkszona wysokoœæ, aby zmieœciæ dodatkowe elementy
            ModConfiguration loadedConfig = ConfigManager.LoadConfig();

            Load += (s, e) =>
            {
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
            };
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

        private void GameIcon_Click(object sender, EventArgs e)
        {
            if (gameIcon.Image != null)
            {
                using (Graphics graphics = Graphics.FromImage(gameIcon.Image))
                {
                    Brush brush = new SolidBrush(Color.Green);
                    graphics.FillEllipse(brush, new Rectangle(gameIcon.Width - 16, gameIcon.Height - 16, 16, 16));
                }
                gameIcon.Refresh();
            }
        }

        private void LaunchButton_Click(object sender, EventArgs e)
        {
            var path = textBoxPath.Text;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(Path.Combine(path, "Among Us.exe")))
            {
                Process.Start(Path.Combine(path, "Among Us.exe"));
            }
            else
            {
                MessageBox.Show("Nie znaleziono pliku Among Us.exe w wybranej œcie¿ce.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}
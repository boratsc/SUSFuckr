using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace SUSFuckr
{
    public static class ModConfigHandler
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null");
        }

        public static void SaveLocalConfig()
        {
            string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            string destinationDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody", $"Konfiguracje{DateTime.Now:yyyyMMddHHmmss}.zip");
            System.IO.Compression.ZipFile.CreateFromDirectory(sourceDir, destinationDir);
            MessageBox.Show("Konfiguracja zosta³a zapisana lokalnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void LoadLocalConfig()
        {
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody");
            string[] files = Directory.GetFiles(configDir, "*.zip");

            if (files.Length == 0)
            {
                MessageBox.Show("Nie znaleziono zapisanych konfiguracji.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (files.Length == 1)
            {
                LoadConfigFromFile(files[0]);
            }
            else
            {
                using OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = configDir,
                    Filter = "ZIP files (*.zip)|*.zip",
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadConfigFromFile(openFileDialog.FileName);
                }
            }
        }

        public static async Task SaveServerConfigAsync()
        {
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody");
            string[] files = Directory.GetFiles(configDir, "*.zip");

            if (files.Length == 0)
            {
                MessageBox.Show("Najpierw zapisz konfiguracjê lokalnie.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string filePath = files[0];
            string hash = Guid.NewGuid().ToString("N");
            string serverUrl = $"{_configuration["Configuration:BaseUrl"]}:{_configuration["Configuration:ApiPort"]}{_configuration["Configuration:UploadEndpoint"]}";

            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();
            using var fs = File.OpenRead(filePath);

            content.Add(new StreamContent(fs), "file", hash);
            var response = await client.PostAsync(serverUrl, content);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Konfiguracja zosta³a zapisana na serwerze. Twój kod: {hash}", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("B³¹d podczas zapisu.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static async Task LoadServerConfigAsync()
        {
            string hash = Prompt.ShowDialog("Podaj kod konfiguracji:", "Wczytaj konfiguracjê z serwera");
            if (string.IsNullOrWhiteSpace(hash)) return;

            string serverUrl = $"{_configuration["Configuration:BaseUrl"]}:{_configuration["Configuration:ApiPort"]}{_configuration["Configuration:DownloadEndpoint"]}/{hash}.zip";
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{hash}.zip");

            using var client = new HttpClient();
            var response = await client.GetAsync(serverUrl);
            if (response.IsSuccessStatusCode)
            {
                using var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fs);
                LoadConfigFromFile(tempFilePath);
            }
            else
            {
                MessageBox.Show("Nie uda³o siê pobraæ konfiguracji.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LoadConfigFromFile(string filePath)
        {
            string destinationDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            ZipFile.ExtractToDirectory(filePath, destinationDir, true);
            MessageBox.Show("Konfiguracja zosta³a wczytana.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form
            {
                Width = 500,
                Height = 150,
                Text = caption
            };
            Label textLabel = new Label { Left = 50, Top = 20, Text = text };
            TextBox textBox = new TextBox { Left = 50, Top = 50, Width = 400 };
            Button confirmation = new Button { Text = "Ok", Left = 350, Top = 70, Width = 100 };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }
    }
}
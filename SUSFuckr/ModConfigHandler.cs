using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Net.Sockets;
using Newtonsoft.Json; // Upewnij siê, ¿e dodasz odpowiedni¹ przestrzeñ do u¿ycia JsonConvert

namespace SUSFuckr
{
    public static class ModConfigHandler
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null");
        }

        // Metoda do zapisu konfiguracji lokalnej
        public static void SaveLocalConfig()
        {
            string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody", "Konfiguracje");

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Okno dialogowe pozwalaj¹ce u¿ytkownikowi wpisaæ nazwê konfiguracji
            string configName = Prompt.ShowDialog("Wpisz nazwê konfiguracji:", "Nazwa konfiguracji");

            // U¿ycie nazwy konfiguracji lub daty jeœli brak nazwy
            string zipFileName = string.IsNullOrWhiteSpace(configName) ? $"Konfiguracja z dnia - {DateTime.Now:yyyyMMddHHmmss}.zip" : $"{configName}.zip";
            string destinationPath = Path.Combine(configDir, zipFileName);

            using (var zipStream = new FileStream(destinationPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var filePath in Directory.GetFiles(sourceDir, "Saved Settings *.txt"))
                {
                    archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                }
            }

            MessageBox.Show("Konfiguracja zosta³a zapisana lokalnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Metoda do ³adowania konfiguracji lokalnej
        public static void LoadLocalConfig()
        {
            string configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Among Us - mody", "Konfiguracje");
            if (!Directory.Exists(configDir))
            {
                MessageBox.Show("Nie znaleziono katalogu konfiguracji.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
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
                using var openFileDialog = new OpenFileDialog
                {
                    InitialDirectory = configDir,
                    Filter = "ZIP files (*.zip)|*.zip"
                };
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadConfigFromFile(openFileDialog.FileName);
                }
            }
        }

        public static async Task SaveServerConfigAsync()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration has not been initialized. Call Initialize() method first.");
            }
            string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            string tempDir = Path.Combine(Path.GetTempPath(), "AmongUsMods");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            string hash = Guid.NewGuid().ToString("N");
            string hashFileName = $"{hash}.zip";
            string tempFilePath = Path.Combine(tempDir, hashFileName);
            try
            {
                var filesToZip = Directory.GetFiles(sourceDir, "Saved Settings *.txt");
                if (filesToZip.Length == 0)
                {
                    MessageBox.Show("No files available to zip.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                using (var zipStream = new FileStream(tempFilePath, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (var filePath in filesToZip)
                    {
                        archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Wyst¹pi³ b³¹d podczas tworzenia pliku ZIP: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show(errorMessage, "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var baseUrl = _configuration["Configuration:BaseUrl"];
            var apiPort = _configuration["Configuration:ApiPort"];
            var uploadEndpoint = _configuration["Configuration:UploadEndpoint"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiPort) || string.IsNullOrWhiteSpace(uploadEndpoint))
            {
                MessageBox.Show("Configuration contains null or whitespace values. Ensure BaseUrl, ApiPort, and UploadEndpoint are correctly set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string serverUrl = $"{baseUrl.TrimEnd('/')}" + $":{apiPort}/" + $"{uploadEndpoint.TrimStart('/')}";
            Console.WriteLine("Server URL: " + serverUrl);
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) => true;
                using var client = new HttpClient(handler);
                using var content = new MultipartFormDataContent();
                using var fs = File.OpenRead(tempFilePath);
                content.Add(new StreamContent(fs), "file", Path.GetFileName(tempFilePath));
                var response = await client.PostAsync(serverUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    ShowHashDialog(hash);  // Wyœwietl hash w oknie dialogowym
                    AddConfigToJSON(hash); // Dodaj hash do pliku JSON
                    MessageBox.Show("Konfiguracja zosta³a zapisana na serwerze.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("B³¹d podczas zapisu. Kod statusu: " + response.StatusCode, "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Wyst¹pi³ b³¹d przy zapisywaniu konfiguracji: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wyst¹pi³ b³¹d przy zapisywaniu konfiguracji: {ex.Message}", "B³¹d HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Wyst¹pi³ nieoczekiwany b³¹d: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wyst¹pi³ nieoczekiwany b³¹d: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Usunêcie tymczasowego folderu
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                    Console.WriteLine("Tymczasowy folder zosta³ usuniêty.");
                }
            }
        }

        public static async Task LoadServerConfigAsync()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Configuration has not been initialized. Call Initialize() method first.");
            }
            string jsonFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SUSFuckr", "touConfigsBase.json");
            List<dynamic> configs = new List<dynamic>();
            string hash = string.Empty;
            if (File.Exists(jsonFile))
            {
                var json = File.ReadAllText(jsonFile);
                configs = JsonConvert.DeserializeObject<List<dynamic>>(json) ?? new List<dynamic>();
            }
            // Tworzenie okna dialogowego z textbox, label i select
            Form form = new Form()
            {
                Width = 500,
                Height = 250,
                Text = "Za³aduj konfiguracjê"
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = "Podaj kod konfiguracji:" };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Label selectLabel = new Label
            {
                Left = 50,
                Top = 80,
                Width = 400, // Zwiêkszona szerokoœæ
                AutoSize = true, // Automatyczne dopasowanie do tekstu
                Text = "Lub wybierz z wczeœniej przes³anych:"
            };
            ComboBox comboBox = new ComboBox() { Left = 50, Top = 105, Width = 400 };
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            if (configs.Any())
            {
                foreach (var c in configs)
                {
                    comboBox.Items.Add($"{c.date} - {c.hash}");
                }
                comboBox.SelectedIndex = 0;
            }
            Button confirmation = new Button() { Text = "OK", Left = 350, Width = 100, Top = 150 };
            confirmation.Click += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    hash = textBox.Text.Trim();
                }
                else if (comboBox.SelectedIndex >= 0)
                {
                    var selectedItem = comboBox?.SelectedItem?.ToString();
                    hash = selectedItem?.Split('-').Last().Trim(); // Wyci¹gnij tylko hash
                }
                form.DialogResult = DialogResult.OK;
                form.Close();
            };
            form.Controls.Add(textLabel);
            form.Controls.Add(textBox);
            form.Controls.Add(selectLabel); // Dodawanie label do formularza
            form.Controls.Add(comboBox);
            form.Controls.Add(confirmation);
            if (form.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(hash)) return;
            // Kontynuacja procesu ³adowania konfiguracji...
            var baseUrl = _configuration["Configuration:BaseUrl"];
            var apiPort = _configuration["Configuration:ApiPort"];
            var downloadEndpoint = _configuration["Configuration:DownloadEndpoint"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiPort) || string.IsNullOrWhiteSpace(downloadEndpoint))
            {
                MessageBox.Show("Configuration contains null values. Ensure BaseUrl, ApiPort, and DownloadEndpoint are correctly set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string serverUrl = $"{baseUrl.TrimEnd('/')}" + $":{apiPort}/" + $"{downloadEndpoint.TrimStart('/')}/{hash}.zip";
            Console.WriteLine("Server URL: " + serverUrl);
            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{hash}.zip");
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) => true;
                using var client = new HttpClient(handler);
                var response = await client.GetAsync(serverUrl);
                if (response.IsSuccessStatusCode)
                {
                    using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                    LoadConfigFromFile(tempFilePath);
                    MessageBox.Show("Konfiguracja z serwera zosta³a pomyœlnie wczytana.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Nie uda³o siê pobraæ konfiguracji z serwera. Kod statusu: {response.StatusCode}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Wyst¹pi³ b³¹d przy pobieraniu konfiguracji: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wyst¹pi³ b³¹d przy pobieraniu konfiguracji: {ex.Message}", "B³¹d HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (SocketException ex)
            {
                string errorMessage = $"Nie uda³o siê po³¹czyæ z serwerem. Szczegó³y: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Nie uda³o siê po³¹czyæ z serwerem. Szczegó³y: {ex.Message}", "B³¹d po³¹czenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Wyst¹pi³ nieoczekiwany b³¹d: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wyst¹pi³ nieoczekiwany b³¹d: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LoadConfigFromFile(string filePath)
        {
            string destinationDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            using (var archive = ZipFile.OpenRead(filePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.StartsWith("Saved Settings") && entry.Name.EndsWith(".txt"))
                    {
                        entry.ExtractToFile(Path.Combine(destinationDir, entry.Name), true);
                    }
                }
            }
            MessageBox.Show("Konfiguracja zosta³a wczytana.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void LogErrorToFile(string message)
        {
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SUSFuckr", "error.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
            using (var writer = new StreamWriter(logFilePath, append: true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        // Dialog wyœwietlaj¹cy hash
        public static void ShowHashDialog(string hash)
        {
            Form prompt = new Form
            {
                Width = 400,
                Height = 200,
                Text = "Hash Has³a"
            };
            Label textLabel = new Label
            {
                Left = 50,
                Top = 20,
                Width = 300,
                Text = "Twój kod: " + hash
            };
            TextBox textBox = new TextBox
            {
                Left = 50,
                Top = 50,
                Width = 300,
                Text = hash,
                ReadOnly = true
            };
            Button confirmation = new Button
            {
                Text = "OK",
                Left = 150,
                Width = 100,
                Top = 120
            };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.ShowDialog();
        }

        private static void AddConfigToJSON(string hash)
        {
            string jsonFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SUSFuckr", "touConfigsBase.json");
            var newEntry = new
            {
                hash = hash,
                date = DateTime.Now.ToString("yyyy-MM-dd, HH:mm")
            };
            List<dynamic> configList;
            if (File.Exists(jsonFile))
            {
                var json = File.ReadAllText(jsonFile);
                configList = JsonConvert.DeserializeObject<List<dynamic>>(json) ?? new List<dynamic>();
            }
            else
            {
                configList = new List<dynamic>();
            }
            configList.Add(newEntry);
            File.WriteAllText(jsonFile, JsonConvert.SerializeObject(configList, Newtonsoft.Json.Formatting.Indented));
        }

        public static void LoadLocalTxtConfig()
        {
            // Œcie¿ka docelowa do zapisania konfiguracji
            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");

            // U¿ywamy okna dialogowego, ¿eby u¿ytkownik móg³ wybraæ plik
            using var openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz plik konfiguracyjny",
                Filter = "TXT files (*.txt)|*.txt"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFilePath = openFileDialog.FileName;

                // Œcie¿ka docelowa do zapisania pliku w Among Us
                string destinationFilePath = Path.Combine(targetDir, Path.GetFileName(selectedFilePath));

                try
                {
                    File.Copy(selectedFilePath, destinationFilePath, overwrite: true);
                    MessageBox.Show("Konfiguracja zosta³a wczytana z pliku txt.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"B³¹d podczas ³adowania konfiguracji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
            Button confirmation = new Button { Text = "OK", Left = 350, Top = 70, Width = 100 };
            confirmation.Click += (sender, e) => { prompt.DialogResult = DialogResult.OK; prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation; // Umo¿liwia ENTER jako przycisk OK
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }
    }
}
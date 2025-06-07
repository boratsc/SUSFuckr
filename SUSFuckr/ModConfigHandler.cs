using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Net.Sockets;
using Newtonsoft.Json; // Upewnij się, że dodasz odpowiednią przestrzeń do użycia JsonConvert

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
            string configDir = Path.Combine(PathSettings.ModsInstallPath, "Konfiguracje");

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            // Okno dialogowe pozwalające użytkownikowi wpisać nazwę konfiguracji
            string configName = Prompt.ShowDialog("Wpisz nazwę konfiguracji:", "Nazwa konfiguracji");

            // Użycie nazwy konfiguracji lub daty jeśli brak nazwy
            string zipFileName = string.IsNullOrWhiteSpace(configName) ? $"Konfiguracja z dnia - {DateTime.Now:yyyyMMddHHmmss}.zip" : $"{configName}.zip";
            string destinationPath = Path.Combine(configDir, zipFileName);

            using (var zipStream = new FileStream(destinationPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var filePath in Directory.GetFiles(sourceDir, "*.txt"))
                {
                    archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                }
            }

            MessageBox.Show("Konfiguracja została zapisana lokalnie.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Metoda do ładowania konfiguracji lokalnej
        public static void LoadLocalConfig()
        {
            string configDir = Path.Combine(PathSettings.ModsInstallPath, "Konfiguracje");
            if (!Directory.Exists(configDir))
            {
                MessageBox.Show("Nie znaleziono katalogu konfiguracji.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string[] files = Directory.GetFiles(configDir, "*.zip");
            if (files.Length == 0)
            {
                MessageBox.Show("Nie znaleziono zapisanych konfiguracji.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var filesToZip = Directory.GetFiles(sourceDir, "*.txt");
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
                string errorMessage = $"Wystąpił błąd podczas tworzenia pliku ZIP: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show(errorMessage, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                string downloadToken = SecretProvider.GetDownloadToken();
                client.DefaultRequestHeaders.Add("Authorization", downloadToken);
                using var content = new MultipartFormDataContent();
                using var fs = File.OpenRead(tempFilePath);
                content.Add(new StreamContent(fs), "file", Path.GetFileName(tempFilePath));
                var response = await client.PostAsync(serverUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    ShowHashDialog(hash);  // Wyświetl hash w oknie dialogowym
                    AddConfigToJSON(hash); // Dodaj hash do pliku JSON
                    MessageBox.Show("Konfiguracja została zapisana na serwerze.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Błąd podczas zapisu. Kod statusu: " + response.StatusCode, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Wystąpił błąd przy zapisywaniu konfiguracji: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wystąpił błąd przy zapisywaniu konfiguracji: {ex.Message}", "Błąd HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Wystąpił nieoczekiwany błąd: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wystąpił nieoczekiwany błąd: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Usunęcie tymczasowego folderu
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                    Console.WriteLine("Tymczasowy folder został usunięty.");
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
                Text = "Załaduj konfigurację"
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = "Podaj kod konfiguracji:" };
            TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
            Label selectLabel = new Label
            {
                Left = 50,
                Top = 80,
                Width = 400, // Zwiększona szerokość
                AutoSize = true, // Automatyczne dopasowanie do tekstu
                Text = "Lub wybierz z wcześniej przesłanych:"
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
                    hash = selectedItem?.Split('-')?.LastOrDefault()?.Trim() ?? string.Empty; // Wyciągnij tylko hash
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
            // Kontynuacja procesu ładowania konfiguracji...
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
                string downloadToken = SecretProvider.GetDownloadToken();
                client.DefaultRequestHeaders.Add("Authorization", downloadToken);
                var response = await client.GetAsync(serverUrl);
                if (response.IsSuccessStatusCode)
                {
                    using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fs);
                    }
                    LoadConfigFromFile(tempFilePath);
                    MessageBox.Show("Konfiguracja z serwera została pomyślnie wczytana.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Nie udało się pobrać konfiguracji z serwera. Kod statusu: {response.StatusCode}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                string errorMessage = $"Wystąpił błąd przy pobieraniu konfiguracji: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wystąpił błąd przy pobieraniu konfiguracji: {ex.Message}", "Błąd HTTP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (SocketException ex)
            {
                string errorMessage = $"Nie udało się połączyć z serwerem. Szczegóły: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Nie udało się połączyć z serwerem. Szczegóły: {ex.Message}", "Błąd połączenia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Wystąpił nieoczekiwany błąd: {ex}";
                Console.WriteLine(errorMessage);
                LogErrorToFile(errorMessage);
                MessageBox.Show($"Wystąpił nieoczekiwany błąd: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LoadConfigFromFile(string filePath)
        {
            string destinationDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            using (var archive = ZipFile.OpenRead(filePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".txt"))
                    {
                        entry.ExtractToFile(Path.Combine(destinationDir, entry.Name), true);
                    }
                }
            }
            MessageBox.Show("Konfiguracja została wczytana.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        // Dialog wyświetlający hash
        public static void ShowHashDialog(string hash)
        {
            Form prompt = new Form
            {
                Width = 400,
                Height = 200,
                Text = "Hash Hasła"
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
            // Ścieżka docelowa do zapisania konfiguracji
            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");

            // Używamy okna dialogowego, żeby użytkownik mógł wybrać plik
            using var openFileDialog = new OpenFileDialog
            {
                Title = "Wybierz plik konfiguracyjny",
                Filter = "TXT files (*.txt)|*.txt"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFilePath = openFileDialog.FileName;

                // Ścieżka docelowa do zapisania pliku w Among Us
                string destinationFilePath = Path.Combine(targetDir, Path.GetFileName(selectedFilePath));

                try
                {
                    File.Copy(selectedFilePath, destinationFilePath, overwrite: true);
                    MessageBox.Show("Konfiguracja została wczytana z pliku txt.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas ładowania konfiguracji: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void ChangePresetNames()
        {
            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\Innersloth\Among Us");
            var txtFiles = Directory.GetFiles(targetDir, "*.txt");

            int baseHeight = 100; // Podstawowa wysokość (np. górna część okna)
            int itemHeight = 25; // Wysokość każdego zestawu Label i TextBox
            int maxWindowHeight = (Screen.PrimaryScreen?.WorkingArea.Height ?? 800) - 100; // Maksymalna wysokość okna, np. 100 pixeli mniej niż wysokość ekranu 


            int calculatedHeight = baseHeight + (txtFiles.Length * itemHeight) + 60; // 70 pixeli to miejsce na przycisk Zapisz
            int finalWindowHeight = Math.Min(calculatedHeight, maxWindowHeight);  // Finalna wysokość ograniczona do maxWindowHeight

            Form form = new Form
            {
                Width = 350,
                Height = finalWindowHeight,
                Text = "Zmień nazwy presetów lobby"
            };

            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            int verticalOffset = 10;

            Dictionary<TextBox, string> fileTextBoxMap = new Dictionary<TextBox, string>();

            foreach (var file in txtFiles)
            {
                string fullFileName = Path.GetFileName(file);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file); // Usuń rozszerzenie .txt

                Label label = new Label
                {
                    Text = $"{fileNameWithoutExtension} →",
                    Left = 10,
                    Top = verticalOffset,
                    AutoSize = true
                };

                TextBox textBox = new TextBox
                {
                    Text = fileNameWithoutExtension, // Pokaż nazwę bez .txt
                    Left = label.Right + 28,
                    Top = verticalOffset,
                    Width = 180, // Mniejsza szerokość TextBox
                    MaxLength = 20 // Ograniczenie ilości znaków
                };

                fileTextBoxMap[textBox] = file;

                panel.Controls.Add(label);
                panel.Controls.Add(textBox);

                verticalOffset += textBox.Height + 10;
            }

            Button saveButton = new Button
            {
                Text = "Zapisz",
                Left = form.Width - 100,
                Width = 80,
                Top = finalWindowHeight - 60, // Pozycja dopasowana do wysokości okna
                Dock = DockStyle.Bottom
            };

            saveButton.Click += (sender, e) =>
            {
                foreach (var entry in fileTextBoxMap)
                {
                    var textBox = entry.Key;
                    var originalFilePath = entry.Value;

                    string newFileNameWithoutExtension = textBox.Text.Trim();
                    string newFileName = newFileNameWithoutExtension + ".txt"; // Dodaj rozszerzenie .txt
                    if (!string.IsNullOrEmpty(newFileNameWithoutExtension) && newFileName != Path.GetFileName(originalFilePath))
                    {
                        string newFilePath = Path.Combine(targetDir, newFileName);
                        try
                        {
                            File.Move(originalFilePath, newFilePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Błąd podczas zmiany nazwy pliku {originalFilePath}: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                MessageBox.Show("Zmiany zostały zapisane.", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Close();
            };

            form.Controls.Add(panel);
            form.Controls.Add(saveButton);
            form.ShowDialog();
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
            prompt.AcceptButton = confirmation; // Umożliwia ENTER jako przycisk OK
            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }
    }
}
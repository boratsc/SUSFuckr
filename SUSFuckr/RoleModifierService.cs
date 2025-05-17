using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;

namespace SUSFuckr
{
    // Klasa modelu przeniesiona do tego samego pliku i przestrzeni nazw
    public class Ability
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }

    public class RoleModifier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string ModName { get; set; }
        public List<Ability> Abilities { get; set; } // DODAJ TO
    }

    // Klasa serwisu w tej samej przestrzeni nazw
    public class RoleModifierService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://susfuckr.boracik.pl/api/roles-modifiers";

        public RoleModifierService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<RoleModifier>> GetRoleModifiersAsync(int? modId = null, string name = null)
        {
            string url = BaseUrl;

            if (modId.HasValue || !string.IsNullOrEmpty(name))
            {
                url += "?";
                if (modId.HasValue)
                {
                    url += $"Id={modId.Value}";
                    if (!string.IsNullOrEmpty(name))
                    {
                        url += "&";
                    }
                }

                if (!string.IsNullOrEmpty(name))
                {
                    url += $"Name={Uri.EscapeDataString(name)}";
                }
            }

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<List<RoleModifier>>(content, options);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<RoleModifier>();
                }
                throw;
            }
        }
    }

    // Klasa formularza wyszukiwania w tej samej przestrzeni nazw
    public class RoleSearchForm : Form
    {
        private TextBox searchBox;
        private ComboBox categoryFilter;
        private ComboBox typeFilter;
        private ListView resultsList;
        private RichTextBox descriptionBox;
        private Button searchButton;
        private Label statusLabel;

        private readonly RoleModifierService _service;
        private readonly int? _modId;
        private readonly string _modName;
        private List<RoleModifier> _allRoles;
        private int _sortColumn = -1;
        private bool _sortAscending = true;
        private List<RoleModifier> _filteredRoles = new List<RoleModifier>();

        public RoleSearchForm(int? modId = null, string modName = null)
        {
            _service = new RoleModifierService();
            _modId = modId;
            _modName = modName;

            InitializeComponent();
            this.Load += RoleSearchForm_Load;
        }

        private void InitializeComponent()
        {
            this.Text = _modName != null ? $"Wyszukiwarka ról - {_modName}" : "Wyszukiwarka ról";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // Tworzymy g³ówny uk³ad formularza
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };

            // Zmniejszamy wysokoœæ panelu wyszukiwania
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); 
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Panel wyszukiwania
            Panel searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            searchBox = new TextBox
            {
                Location = new Point(10, 20),
                Width = 200,
                PlaceholderText = "Wpisz nazwê roli..."
            };
            searchBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true; // Zapobiega sygna³owi dŸwiêkowemu
                    ApplyFilters();
                }
            };
            searchBox.TextChanged += (s, e) => {
                ApplyFilters();
            };

            searchPanel.Controls.Add(searchBox);

            categoryFilter = new ComboBox
            {
                Location = new Point(220, 20),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            categoryFilter.Items.Add("Wszystkie kategorie");
            // Tutaj dodaj obs³ugê zdarzenia SelectedIndexChanged
            categoryFilter.SelectedIndexChanged += (s, e) => {
                ApplyFilters();
            };
            searchPanel.Controls.Add(categoryFilter);

            typeFilter = new ComboBox
            {
                Location = new Point(350, 20),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            typeFilter.Items.Add("Wszystkie typy");
            // Tutaj dodaj obs³ugê zdarzenia SelectedIndexChanged
            typeFilter.SelectedIndexChanged += (s, e) => {
                ApplyFilters();
            };
            searchPanel.Controls.Add(typeFilter);

            searchButton = new Button
            {
                Text = "Szukaj",
                Location = new Point(480, 19),
                Width = 80
            };
            searchButton.Click += SearchButton_Click;
            searchPanel.Controls.Add(searchButton);

            statusLabel = new Label
            {
                AutoSize = true,
                Location = new Point(570, 23),
                Text = "£adowanie..."
            };
            searchPanel.Controls.Add(statusLabel);

            // Dodajemy panel wyszukiwania do pierwszego wiersza
            mainLayout.Controls.Add(searchPanel, 0, 0);

            // SplitContainer dla wyników i opisu
            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 200
            };

            this.Load += (s, e) => {
                // Ustaw pocz¹tkowy podzia³ - 40% dla listy, 60% dla opisu
                splitContainer.SplitterDistance = (int)(splitContainer.Width * 0.4);
            };

            // Dodaj obs³ugê zdarzenia Resize dla SplitContainer
            splitContainer.SizeChanged += (s, e) => {
                // Zachowaj proporcjê 40/60 przy zmianie rozmiaru
                splitContainer.SplitterDistance = (int)(splitContainer.Width * 0.4);
            };

            // Lista wyników
            resultsList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            resultsList.ColumnClick += ResultsList_ColumnClick;
            resultsList.Columns.Add("Nazwa", 150);
            resultsList.Columns.Add("Kategoria", 100);
            resultsList.Columns.Add("Typ", 100);

            resultsList.Columns[0].Width = 100;  // Nazwa - wê¿sza kolumna
            resultsList.Columns[1].Width = 80;   // Kategoria - wê¿sza kolumna
            resultsList.Columns[2].Width = 80;   // Typ - wê¿sza kolumna

            // Mo¿emy te¿ dodaæ obs³ugê zdarzenia Resize, aby dostosowaæ szerokoœci kolumn
            resultsList.Resize += (s, e) => {
                int totalWidth = resultsList.ClientSize.Width - 20; // Odejmujemy margines
                resultsList.Columns[0].Width = (int)(totalWidth * 0.4); // 40% szerokoœci
                resultsList.Columns[1].Width = (int)(totalWidth * 0.3); // 30% szerokoœci
                resultsList.Columns[2].Width = (int)(totalWidth * 0.3); // 30% szerokoœci
            };

            resultsList.SelectedIndexChanged += ResultsList_SelectedIndexChanged;
            splitContainer.Panel1.Controls.Add(resultsList);

            // Panel opisu
            descriptionBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None
            };
            
            splitContainer.Panel2.Controls.Add(descriptionBox);

            // Dodajemy SplitContainer do drugiego wiersza
            mainLayout.Controls.Add(splitContainer, 0, 1);

            // Dodajemy g³ówny uk³ad do formularza
            this.Controls.Add(mainLayout);
        }


        private async void RoleSearchForm_Load(object sender, EventArgs e)
        {
            await LoadRolesAsync();

            // Dodajemy obs³ugê zdarzeñ dopiero po za³adowaniu danych
            searchBox.KeyDown += (s, ev) => {
                if (ev.KeyCode == Keys.Enter)
                {
                    ev.SuppressKeyPress = true;
                    ApplyFilters();
                }
            };

            categoryFilter.SelectedIndexChanged += (s, ev) => {
                ApplyFilters();
            };

            typeFilter.SelectedIndexChanged += (s, ev) => {
                ApplyFilters();
            };
        }

        private async Task LoadRolesAsync()
        {
            try
            {
                statusLabel.Text = "£adowanie...";
                _allRoles = await _service.GetRoleModifiersAsync(_modId);
                _filteredRoles = new List<RoleModifier>(_allRoles);

                // Populate filters
                PopulateFilters();

                // Display all roles initially
                DisplayRoles(_allRoles);

                statusLabel.Text = $"Za³adowano {_allRoles.Count} ról/modyfikatorów";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "B³¹d ³adowania";
                MessageBox.Show($"Wyst¹pi³ b³¹d podczas ³adowania danych: {ex.Message}",
                    "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateFilters()
        {
            // Remember selected items
            var selectedCategory = categoryFilter.SelectedItem;
            var selectedType = typeFilter.SelectedItem;

            // Clear and repopulate
            categoryFilter.Items.Clear();
            typeFilter.Items.Clear();

            categoryFilter.Items.Add("Wszystkie kategorie");
            typeFilter.Items.Add("Wszystkie typy");

            var categories = _allRoles.Select(r => r.Category).Distinct().OrderBy(c => c);
            var types = _allRoles.Select(r => r.Type).Distinct().OrderBy(t => t);

            foreach (var category in categories)
            {
                categoryFilter.Items.Add(category);
            }

            foreach (var type in types)
            {
                typeFilter.Items.Add(type);
            }

            // Restore selection or select first item
            categoryFilter.SelectedItem = selectedCategory != null && categoryFilter.Items.Contains(selectedCategory)
                ? selectedCategory
                : categoryFilter.Items[0];

            typeFilter.SelectedItem = selectedType != null && typeFilter.Items.Contains(selectedType)
                ? selectedType
                : typeFilter.Items[0];
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allRoles == null || categoryFilter.SelectedItem == null || typeFilter.SelectedItem == null)
                return;

            var searchText = searchBox.Text.Trim().ToLower();
            var categoryText = categoryFilter.SelectedItem.ToString();
            var typeText = typeFilter.SelectedItem.ToString();

            _filteredRoles = _allRoles.Where(role =>
                (string.IsNullOrEmpty(searchText) || role.Name.ToLower().Contains(searchText)) &&
                (categoryText == "Wszystkie kategorie" || role.Category == categoryText) &&
                (typeText == "Wszystkie typy" || role.Type == typeText)
            ).ToList();

            SortAndDisplayRoles();
            statusLabel.Text = $"Znaleziono {_filteredRoles.Count} wyników";
        }

        private void SortAndDisplayRoles()
        {
            if (_sortColumn >= 0)
            {
                Func<RoleModifier, object> keySelector = _sortColumn switch
                {
                    0 => r => r.Name,
                    1 => r => r.Category,
                    2 => r => r.Type,
                    _ => r => r.Name
                };

                if (_sortAscending)
                    _filteredRoles = _filteredRoles.OrderBy(keySelector).ToList();
                else
                    _filteredRoles = _filteredRoles.OrderByDescending(keySelector).ToList();
            }

            DisplayRoles(_filteredRoles);
        }

        private void ResultsList_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }
            SortAndDisplayRoles();
        }


        private void DisplayRoles(List<RoleModifier> roles)
        {
            resultsList.Items.Clear();
            descriptionBox.Clear();

            foreach (var role in roles)
            {
                var item = new ListViewItem(role.Name);
                item.SubItems.Add(role.Category);
                item.SubItems.Add(role.Type);
                // Usuniêto dodawanie ModName
                item.Tag = role;
                resultsList.Items.Add(item);
            }

            if (resultsList.Items.Count > 0)
            {
                resultsList.Items[0].Selected = true;
            }
        }

        private void ResultsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (resultsList.SelectedItems.Count > 0)
            {
                var role = (RoleModifier)resultsList.SelectedItems[0].Tag;
                descriptionBox.Clear();

                // Add title
                descriptionBox.SelectionFont = new Font(descriptionBox.Font.FontFamily, 14, FontStyle.Bold);
                descriptionBox.AppendText($"{role.Name}\n\n");

                // Add category and type
                descriptionBox.SelectionFont = new Font(descriptionBox.Font.FontFamily, 10, FontStyle.Italic);
                descriptionBox.AppendText($"Kategoria: {role.Category ?? ""}\n");
                descriptionBox.AppendText($"Typ: {role.Type ?? ""}\n");
                descriptionBox.AppendText($"Mod: {role.ModName ?? ""}\n\n");

                // Add description
                descriptionBox.SelectionFont = new Font(descriptionBox.Font.FontFamily, 10, FontStyle.Regular);
                descriptionBox.AppendText(role.Description?.Replace("\\n", "\n").Replace("\\r", "\r") ?? "");

                // --- DODAJ WYŒWIETLANIE ABILITIES ---
                if (role.Abilities != null && role.Abilities.Count > 0)
                {
                    descriptionBox.AppendText("\n\nZdolnoœci:\n");
                    foreach (var ability in role.Abilities)
                    {
                        try
                        {
                            // Ikonka (jeœli jest)
                            if (!string.IsNullOrEmpty(ability.Icon))
                            {
                                using (var wc = new System.Net.WebClient())
                                {
                                    byte[] bytes = wc.DownloadData(ability.Icon);
                                    using (var ms = new System.IO.MemoryStream(bytes))
                                    {
                                        using (var img = Image.FromStream(ms))
                                        {
                                            // Skalowanie do 24x24 px
                                            using (var scaled = new Bitmap(img, new Size(24, 24)))
                                            {
                                                Clipboard.SetImage(scaled); // workaround na wklejanie obrazka do RichTextBox
                                                descriptionBox.ReadOnly = false;
                                                descriptionBox.Paste();
                                                descriptionBox.ReadOnly = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Jeœli nie uda siê pobraæ obrazka, pomiñ
                        }

                        // Nazwa ability
                        descriptionBox.SelectionFont = new Font(descriptionBox.Font.FontFamily, 10, FontStyle.Bold);
                        descriptionBox.AppendText($" {ability.Name}\n");

                        // Opis ability
                        if (!string.IsNullOrWhiteSpace(ability.Description))
                        {
                            descriptionBox.SelectionFont = new Font(descriptionBox.Font.FontFamily, 10, FontStyle.Regular);
                            descriptionBox.AppendText($"{ability.Description.Replace("\\n", "\n").Replace("\\r", "\r")}\n");
                        }
                    }
                }
            }
        }
    }
}   
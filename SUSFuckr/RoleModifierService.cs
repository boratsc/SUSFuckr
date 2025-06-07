using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;

namespace SUSFuckr
{
    public class RoleInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ModifierInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ModName { get; set; } = string.Empty;
        public List<string> Abilities { get; set; } = new List<string>();
    }

    public static class RoleModifierService
    {
        private static List<ModifierInfo>? cachedRoles = null;

        public static List<ModifierInfo> GetRolesForMod(int modId)
        {
            try
            {
                if (cachedRoles == null)
                {
                    cachedRoles = LoadRolesFromServer();
                }

                if (cachedRoles == null)
                {
                    return new List<ModifierInfo>();
                }

                // Mapowanie ID modów na nazwy modów
                var modIdToName = new Dictionary<int, string>
                {
                    { 1, "Town of Us" },
                    { 4, "The Other Roles" },
                    { 7, "Syzyfowy ToU" },
                    { 2, "ToU - Wygon" },
                    { 6, "Town of Host" }
                };

                if (!modIdToName.TryGetValue(modId, out string? modName))
                {
                    return new List<ModifierInfo>();
                }

                return cachedRoles.Where(r => r.ModName == modName).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas pobierania ról: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<ModifierInfo>();
            }
        }

        private static List<ModifierInfo>? LoadRolesFromServer()
        {
            try
            {
                string url = "https://susfuckr.boracik.pl/susfuckr/roles/roles.json";
                using (var client = new System.Net.Http.HttpClient())
                {
                    var response = client.GetStringAsync(url).Result;
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<ModifierInfo>>(response, options) ?? new List<ModifierInfo>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie uda³o siê pobraæ danych o rolach: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }

    public partial class RoleSearchForm : Form
    {
        private int modId;
        private string modName;
        private TextBox searchBox = null!;
        private ComboBox categoryFilter = null!;
        private ComboBox typeFilter = null!;
        private ListView resultsList = null!;
        private RichTextBox descriptionBox = null!;
        private Button searchButton = null!;
        private Label statusLabel = null!;
        private List<ModifierInfo> _allRoles = null!;

        public RoleSearchForm(int modId, string modName)
        {
            this.modId = modId;
            this.modName = modName;
            InitializeComponent();
            Load += RoleSearchForm_Load;
        }

        private void RoleSearchForm_Load(object? sender, EventArgs e)
        {
            _allRoles = RoleModifierService.GetRolesForMod(modId);
            PopulateFilters();
            PerformSearch();
            statusLabel.Text = $"Znaleziono {_allRoles.Count} ról/modyfikatorów dla {modName}";
        }

        private void InitializeComponent()
        {
            this.Text = $"Role i modyfikatory - {modName}";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Search box
            var searchLabel = new Label
            {
                Text = "Szukaj:",
                Location = new Point(10, 15),
                AutoSize = true
            };
            searchBox = new TextBox
            {
                Location = new Point(60, 12),
                Width = 200
            };

            // Category filter
            var categoryLabel = new Label
            {
                Text = "Kategoria:",
                Location = new Point(280, 15),
                AutoSize = true
            };
            categoryFilter = new ComboBox
            {
                Location = new Point(340, 12),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Type filter
            var typeLabel = new Label
            {
                Text = "Typ:",
                Location = new Point(480, 15),
                AutoSize = true
            };
            typeFilter = new ComboBox
            {
                Location = new Point(510, 12),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Search button
            searchButton = new Button
            {
                Text = "Szukaj",
                Location = new Point(630, 10),
                Width = 80
            };
            searchButton.Click += SearchButton_Click;

            // Results list
            resultsList = new ListView
            {
                Location = new Point(10, 50),
                Size = new Size(760, 300),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            resultsList.Columns.Add("Nazwa", 200);
            resultsList.Columns.Add("Kategoria", 120);
            resultsList.Columns.Add("Typ", 100);
            resultsList.Columns.Add("Mod", 120);
            resultsList.Columns.Add("Abilities", 220);

            resultsList.ColumnClick += ResultsList_ColumnClick;
            resultsList.SelectedIndexChanged += ResultsList_SelectedIndexChanged;

            // Description box
            var descLabel = new Label
            {
                Text = "Opis:",
                Location = new Point(10, 360),
                AutoSize = true
            };
            descriptionBox = new RichTextBox
            {
                Location = new Point(10, 380),
                Size = new Size(760, 150),
                ReadOnly = true
            };

            // Status label
            statusLabel = new Label
            {
                Location = new Point(10, 540),
                AutoSize = true,
                Text = "£adowanie..."
            };

            this.Controls.AddRange(new Control[] {
                searchLabel, searchBox,
                categoryLabel, categoryFilter,
                typeLabel, typeFilter,
                searchButton, resultsList,
                descLabel, descriptionBox,
                statusLabel
            });
        }

        private void SearchButton_Click(object? sender, EventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            var filteredRoles = _allRoles.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchBox.Text))
            {
                filteredRoles = filteredRoles.Where(r =>
                    r.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase) ||
                    r.Description.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase));
            }

            // Apply category filter
            if (categoryFilter.SelectedItem?.ToString() != "Wszystkie")
            {
                string? selectedCategory = categoryFilter.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedCategory))
                {
                    filteredRoles = filteredRoles.Where(r => r.Category == selectedCategory);
                }
            }

            // Apply type filter
            if (typeFilter.SelectedItem?.ToString() != "Wszystkie")
            {
                string? selectedType = typeFilter.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedType))
                {
                    filteredRoles = filteredRoles.Where(r => r.Type == selectedType);
                }
            }

            UpdateResultsList(filteredRoles.ToList());
        }

        private void ResultsList_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            // Simple sorting implementation
            var items = resultsList.Items.Cast<ListViewItem>().ToList();
            items.Sort((x, y) => string.Compare(x.SubItems[e.Column].Text, y.SubItems[e.Column].Text));

            resultsList.Items.Clear();
            resultsList.Items.AddRange(items.ToArray());
        }

        private void ResultsList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (resultsList.SelectedItems.Count > 0)
            {
                var selectedItem = resultsList.SelectedItems[0];
                string roleName = selectedItem.Text;
                var role = _allRoles.FirstOrDefault(r => r.Name == roleName);
                if (role != null)
                {
                    descriptionBox.Text = role.Description;
                }
            }
        }

        private void PopulateFilters()
        {
            // Populate category filter
            var categories = _allRoles.Select(r => r.Category).Distinct().OrderBy(c => c).ToList();
            categoryFilter.Items.Add("Wszystkie");
            categoryFilter.Items.AddRange(categories.ToArray());
            categoryFilter.SelectedIndex = 0;

            // Populate type filter
            var types = _allRoles.Select(r => r.Type).Distinct().OrderBy(t => t).ToList();
            typeFilter.Items.Add("Wszystkie");
            typeFilter.Items.AddRange(types.ToArray());
            typeFilter.SelectedIndex = 0;
        }

        private void UpdateResultsList(List<ModifierInfo> roles)
        {
            resultsList.Items.Clear();

            foreach (var role in roles)
            {
                var item = new ListViewItem(role.Name);
                item.SubItems.Add(role.Category);
                item.SubItems.Add(role.Type);
                item.SubItems.Add(role.ModName);
                item.SubItems.Add(string.Join(", ", role.Abilities));

                resultsList.Items.Add(item);
            }

            statusLabel.Text = $"Znaleziono {roles.Count} z {_allRoles.Count} ról/modyfikatorów";
        }
    }

    public static class RoleIconService
    {
        private static readonly Dictionary<string, string> IconCache = new Dictionary<string, string>();

        public static string GetRoleIcon(string roleName)
        {
            if (IconCache.TryGetValue(roleName, out string? cachedIcon))
            {
                return cachedIcon;
            }

            try
            {
                // Try to download icon from server
                string iconUrl = $"https://susfuckr.boracik.pl/susfuckr/icons/{roleName.Replace(" ", "_").ToLower()}.png";

                using (var client = new System.Net.Http.HttpClient())
                {
                    var response = client.GetAsync(iconUrl).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var iconData = response.Content.ReadAsByteArrayAsync().Result;
                        string base64Icon = Convert.ToBase64String(iconData);
                        IconCache[roleName] = base64Icon;
                        return base64Icon;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show to user for icons
                System.Diagnostics.Debug.WriteLine($"Failed to load icon for {roleName}: {ex.Message}");
            }

            // Return empty string if icon not found
            IconCache[roleName] = string.Empty;
            return string.Empty;
        }

        public static Image? GetRoleImageFromBase64(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return null;

            try
            {
                byte[] imageData = Convert.FromBase64String(base64Data);
                using (var ms = new System.IO.MemoryStream(imageData))
                {
                    return Image.FromStream(ms);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SUSFuckr
{
	public partial class ModSelectorForm : Form
	{
		public List<ModConfiguration> SelectedMods { get; private set; } = new List<ModConfiguration>();
		private Dictionary<string, ModConfiguration> modDictionary = new Dictionary<string, ModConfiguration>();

		public ModSelectorForm(List<ModConfiguration> availableMods)
		{
			InitializeComponent();

			AvailableModsListBox.SelectionMode = SelectionMode.MultiExtended;
			// Dodaj tylko zainstalowane mody typu "full"
			foreach (var mod in availableMods.Where(m => m.ModType == "full" && !string.IsNullOrEmpty(m.InstallPath) && Directory.Exists(m.InstallPath)))
			{
				modDictionary.Add(mod.ModName, mod);
				AvailableModsListBox.Items.Add(mod.ModName);
			}
            for (int i = 0; i < AvailableModsListBox.Items.Count; i++)
            {
                AvailableModsListBox.SetSelected(i, true);
            }
        }

		private void btnOk_Click(object sender, EventArgs e)
		{
			// Przechodzimy przez wybrane nazwy mod�w i odczytujemy pe�n� konfiguracj� z Dictionary
			foreach (string selectedName in AvailableModsListBox.SelectedItems)
			{
                if (modDictionary.TryGetValue(selectedName, out ModConfiguration? modConfig))
                {
                    // Kod, kt�ry korzysta z modConfig, powinien sprawdzi�, czy modConfig nie jest null
                    if (modConfig != null)
                    {
                        SelectedMods.Add(modConfig);
                    }
                }
            }

			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
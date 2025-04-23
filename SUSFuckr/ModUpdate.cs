using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SUSFuckr
{
    public class ModUpdates
    {
        public static async Task UpdateModAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs, ProgressBar progressBar)
        {
            try
            {
                if (modConfig.ModType == "full")
                {
                    progressBar.Visible = true; // Poka� pasek post�pu na pocz�tku
                    progressBar.Style = ProgressBarStyle.Continuous;

                    ModDelete.DeleteMod(modConfig, modConfigs); // Usuni�cie moda

                    ModManager manager = new ModManager();
                    await manager.ModifyAsync(modConfig, modConfigs, progressBar); // Modyfikowanie moda z ProgressBar

                    progressBar.Visible = false; // Ukryj pasek post�pu po zako�czeniu modyfikacji

                    MessageBox.Show($"Mod '{modConfig.ModName}' zosta� pomy�lnie zaktualizowany.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wyst�pi� b��d podczas aktualizacji: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
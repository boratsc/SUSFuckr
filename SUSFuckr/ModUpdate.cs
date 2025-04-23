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
                    progressBar.Visible = true; // Poka¿ pasek postêpu na pocz¹tku
                    progressBar.Style = ProgressBarStyle.Continuous;

                    ModDelete.DeleteMod(modConfig, modConfigs); // Usuniêcie moda

                    ModManager manager = new ModManager();
                    await manager.ModifyAsync(modConfig, modConfigs, progressBar); // Modyfikowanie moda z ProgressBar

                    progressBar.Visible = false; // Ukryj pasek postêpu po zakoñczeniu modyfikacji

                    MessageBox.Show($"Mod '{modConfig.ModName}' zosta³ pomyœlnie zaktualizowany.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wyst¹pi³ b³¹d podczas aktualizacji: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
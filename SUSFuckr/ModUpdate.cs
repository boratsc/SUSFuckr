using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SUSFuckr
{
    public class ModUpdates
    {
        public static async Task UpdateModAsync(
            ModConfiguration modConfig,
            List<ModConfiguration> modConfigs,
            ProgressBar progressBar,
            Label progressLabel) // Dodanie labelu
        {
            try
            {
                if (modConfig.ModType == "full")
                {
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Continuous;

                    ModDelete.DeleteMod(modConfig, modConfigs);

                    ModManager manager = new ModManager();
                    await manager.ModifyAsync(modConfig, modConfigs, progressBar, progressLabel); // Przekazanie label

                    progressBar.Visible = false;

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
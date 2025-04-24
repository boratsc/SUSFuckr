using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace SUSFuckr
{
    public class ModUpdates
    {
        public static async Task UpdateModAsync(
            ModConfiguration modConfig,
            List<ModConfiguration> modConfigs,
            ProgressBar progressBar,
            Label progressLabel,
            IConfiguration configuration) // Dodanie IConfiguration jako parametru
        {
            try
            {
                if (modConfig.ModType == "full")
                {
                    progressBar.Visible = true;
                    progressBar.Style = ProgressBarStyle.Continuous;
                    ModDelete.DeleteMod(modConfig, modConfigs);
                    ModManager manager = new ModManager(configuration); // Przekazanie configuration
                    await manager.ModifyAsync(modConfig, modConfigs, progressBar, progressLabel);
                    progressBar.Visible = false;
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
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

                    // Usuni�cie istniej�cego moda przed aktualizacj�
                    ModDelete.DeleteMod(modConfig, modConfigs);

                    // Inicjalizacja ModManager z konfiguracj�
                    ModManager modManager = new ModManager(configuration);

                    // U�ycie mode z konfiguracji
                    string mode = configuration["Configuration:Mode"] ?? "steam"; // lub "epic", zale�nie od ustawie�

                    // Wywo�anie ModifyAsync z prawid�owym argumentem mode
                    await modManager.ModifyAsync(modConfig, modConfigs, progressBar, progressLabel, mode);

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
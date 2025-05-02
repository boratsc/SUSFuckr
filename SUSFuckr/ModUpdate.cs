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

                    // Usuniêcie istniej¹cego moda przed aktualizacj¹
                    ModDelete.DeleteMod(modConfig, modConfigs);

                    // Inicjalizacja ModManager z konfiguracj¹
                    ModManager modManager = new ModManager(configuration);

                    // U¿ycie mode z konfiguracji
                    string mode = configuration["Configuration:Mode"] ?? "steam"; // lub "epic", zale¿nie od ustawieñ

                    // Wywo³anie ModifyAsync z prawid³owym argumentem mode
                    await modManager.ModifyAsync(modConfig, modConfigs, progressBar, progressLabel, mode);

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
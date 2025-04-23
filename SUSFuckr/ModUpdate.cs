using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SUSFuckr
{
    public class ModUpdates
    {
        public static async Task UpdateModAsync(ModConfiguration modConfig, List<ModConfiguration> modConfigs)
        {
            try
            {
                if (modConfig.ModType == "full")
                {
                    ModDelete.DeleteMod(modConfig, modConfigs); // Usuniêcie moda

                    ModManager manager = new ModManager();
                    await manager.ModifyAsync(modConfig, modConfigs); // Modyfikowanie moda

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
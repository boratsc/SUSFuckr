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
                    ModDelete.DeleteMod(modConfig, modConfigs); // Usuni�cie moda

                    ModManager manager = new ModManager();
                    await manager.ModifyAsync(modConfig, modConfigs); // Modyfikowanie moda

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
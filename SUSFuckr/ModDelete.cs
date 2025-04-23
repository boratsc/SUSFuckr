using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SUSFuckr
{
    public class ModDelete
    {
        public static void DeleteMod(ModConfiguration modConfig, List<ModConfiguration> modConfigs)
        {
            if (modConfig.ModType == "full")
            {
                DeleteFullMod(modConfig, modConfigs);
            }
            else if (modConfig.ModType == "dll")
            {
                DeleteDllMod(modConfig, modConfigs);
            }
        }

        private static void DeleteFullMod(ModConfiguration modConfig, List<ModConfiguration> modConfigs)
        {
            try
            {
                if (Directory.Exists(modConfig.InstallPath))
                {
                    Directory.Delete(modConfig.InstallPath, true);
                    modConfig.InstallPath = string.Empty; // Aktualizacja konfiguracji
                    ConfigManager.SaveConfig(modConfigs);
                    MessageBox.Show($"Mod '{modConfig.ModName}' zosta³ pomyœlnie usuniêty.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wyst¹pi³ b³¹d podczas usuwania: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void DeleteDllMod(ModConfiguration modConfig, List<ModConfiguration> modConfigs)
        {
            var fullMods = modConfigs.Where(x => x.ModType == "full" && !string.IsNullOrEmpty(x.InstallPath)).ToList();
            using var modSelector = new ModSelectorForm(fullMods);
            if (modSelector.ShowDialog() == DialogResult.OK)
            {
                var selectedMods = modSelector.SelectedMods;
                foreach (var fullMod in selectedMods)
                {
                    string dllPath = Path.Combine(fullMod.InstallPath, modConfig.DllInstallPath ?? string.Empty, $"{modConfig.ModName}.dll");
                    if (File.Exists(dllPath))
                    {
                        try
                        {
                            File.Delete(dllPath);
                            MessageBox.Show($"Mod DLL '{modConfig.ModName}' zosta³ pomyœlnie usuniêty z '{fullMod.ModName}'.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Wyst¹pi³ b³¹d podczas usuwania DLL: {ex.Message}", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                modConfig.InstallPath = string.Empty; // Aktualizacja konfiguracji
                ConfigManager.SaveConfig(modConfigs);
            }
        }
    }
}
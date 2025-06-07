using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SUSFuckr
{
    public static class ConfigUpdater
    {
        public static void CompareAndMergeConfigurations(string tempFilePath)
        {
            var newConfigs = JsonSerializer.Deserialize<List<ModConfiguration>>(File.ReadAllText(tempFilePath)) ?? new List<ModConfiguration>();
            var existingConfigs = ConfigManager.LoadConfig();
            var newConfigIds = new HashSet<int>(newConfigs.Select(c => c.Id));

            // Usuñ istniej¹ce konfiguracje, których ID zniknê³o w nowym zestawie
            existingConfigs.RemoveAll(c => !newConfigIds.Contains(c.Id));

            foreach (var newConfig in newConfigs)
            {
                var existingConfig = existingConfigs.FirstOrDefault(c => c.Id == newConfig.Id);
                if (existingConfig != null)
                {
                    // Aktualizuj istniej¹c¹ konfiguracjê, uwzglêdniaj¹c ModName i pomijaj¹c InstallPath
                    UpdateExistingConfig(existingConfig, newConfig);
                }
                else
                {
                    // Dodaj now¹ konfiguracjê, jeœli nie istnieje
                    existingConfigs.Add(newConfig);
                }
            }
            ConfigManager.SaveConfig(existingConfigs);
        }

        private static void UpdateExistingConfig(ModConfiguration existingConfig, ModConfiguration newConfig)
        {
            existingConfig.PngFileName = newConfig.PngFileName;
            existingConfig.GitHubRepoOrLink = newConfig.GitHubRepoOrLink;
            existingConfig.EpicGitHubRepoOrLink = newConfig.EpicGitHubRepoOrLink; 
            existingConfig.ModType = newConfig.ModType;
            existingConfig.DllInstallPath = newConfig.DllInstallPath;
            existingConfig.ModVersion = newConfig.ModVersion;
            existingConfig.LastUpdated = newConfig.LastUpdated;
            existingConfig.AmongVersion = newConfig.AmongVersion;
            existingConfig.Description = newConfig.Description;
            existingConfig.ModName = newConfig.ModName;
        }
    }
}

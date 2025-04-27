using System;
using System.IO;
using System.Text.Json;

namespace SUSFuckr
{
    public class ModConfiguration
    {
        public int Id { get; set; } // Dodajemy pole Id
        public string ModName { get; set; } = string.Empty;
        public string PngFileName { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
        public string? GitHubRepoOrLink { get; set; } = string.Empty;
        public string ModType { get; set; } = string.Empty;
        public string? DllInstallPath { get; set; }
        public string ModVersion { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; } = DateTime.Now;
        public string AmongVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Dodajemy pole Description
    }

    public static class ConfigManager
    {
        private static readonly string configFilePath = Path.Combine(
           AppDomain.CurrentDomain.BaseDirectory, // U¿yj lokalizacji pliku wykonywalnego
           "config.json");

        public static List<ModConfiguration> LoadConfig()
        {
            if (!File.Exists(configFilePath))
                return new List<ModConfiguration>();

            var json = File.ReadAllText(configFilePath);
            return JsonSerializer.Deserialize<List<ModConfiguration>>(json) ?? new List<ModConfiguration>();
        }

        public static void SaveConfig(List<ModConfiguration> configs)
        {
            var dir = Path.GetDirectoryName(configFilePath) ?? string.Empty;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, json);
        }
    }
}
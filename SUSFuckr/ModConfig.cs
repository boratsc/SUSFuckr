using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SUSFuckr
{
    public class ModConfiguration
    {
        public int Id { get; set; }
        public string ModName { get; set; } = string.Empty;
        public string PngFileName { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
        public string GitHubRepoOrLink { get; set; } = string.Empty;
        public string EpicGitHubRepoOrLink { get; set; } = string.Empty; // Nowe pole
        public string ModType { get; set; } = string.Empty;
        public string? DllInstallPath { get; set; }
        public string ModVersion { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string AmongVersion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public static class ConfigManager
    {
        private static readonly string exeDir = Path.GetDirectoryName(Environment.ProcessPath)!;
        private static readonly string configFilePath = Path.Combine(exeDir, "config.json");
        private static readonly string appSettingsFilePath = Path.Combine(exeDir, "appsettings.json");
        private static readonly string configApiUrl = "https://susfuckr.boracik.pl/api/config";

        public static List<ModConfiguration> LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);
                return JsonSerializer.Deserialize<List<ModConfiguration>>(json) ?? new List<ModConfiguration>();
            }

            // Jeœli plik nie istnieje, pobierz z API
            return Task.Run(() => FetchConfigFromApiAsync()).Result;
        }

        private static async Task<List<ModConfiguration>> FetchConfigFromApiAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(configApiUrl);
                    return JsonSerializer.Deserialize<List<ModConfiguration>>(response) ?? new List<ModConfiguration>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching config from API: {ex.Message}");
                    return new List<ModConfiguration>();
                }
            }
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

        public static void SaveConfigurationSetting(string key, string value)
        {
            var json = File.ReadAllText(appSettingsFilePath);
            var jsonObj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
            if (jsonObj != null && jsonObj.ContainsKey("Configuration"))
            {
                var configuration = jsonObj["Configuration"];
                if (configuration.ContainsKey(key))
                {
                    configuration[key] = value;
                }
                else
                {
                    configuration.Add(key, value);
                }

                var updatedJson = JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(appSettingsFilePath, updatedJson);
            }
        }
    }
}
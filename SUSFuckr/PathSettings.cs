using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace SUSFuckr
{
	public static class PathSettings
	{
		private static string _modsInstallPath = string.Empty;
		private static readonly string _defaultModsPath;
        private static readonly string _configFilePath = Path.Combine(
			Path.GetDirectoryName(Environment.ProcessPath)!,
			"appsettings.json");

        static PathSettings()
		{
			var config = new ConfigurationBuilder()
				.AddJsonFile(_configFilePath, optional: true, reloadOnChange: true)
				.Build();

			_defaultModsPath = Environment.ExpandEnvironmentVariables(
				config["AppSettings:DefaultModsPath"] ?? "%APPDATA%\\Among Us - Mody");

			_modsInstallPath = config["AppSettings:ModsInstallPath"] ?? string.Empty;

			// Jeœli œcie¿ka nie jest ustawiona, u¿yj domyœlnej
			if (string.IsNullOrEmpty(_modsInstallPath))
			{
				_modsInstallPath = _defaultModsPath;
			}
		}

		public static string ModsInstallPath
		{
			get => _modsInstallPath;
			set
			{
				_modsInstallPath = value;
				SavePathToConfig();
			}
		}

		public static string DefaultModsPath => _defaultModsPath;

        private static void SavePathToConfig()
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json) as Newtonsoft.Json.Linq.JObject;

                if (jsonObj == null)
                {
                    jsonObj = new Newtonsoft.Json.Linq.JObject();
                }

                if (jsonObj["AppSettings"] == null)
                {
                    jsonObj["AppSettings"] = new Newtonsoft.Json.Linq.JObject();
                }

                jsonObj["AppSettings"]!["ModsInstallPath"] = _modsInstallPath;

                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(_configFilePath, output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d podczas zapisywania œcie¿ki do konfiguracji: {ex.Message}");
                // Mo¿na dodaæ logowanie b³êdu
            }
        }


        public static void ResetToDefault()
		{
			ModsInstallPath = _defaultModsPath;
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SUSFuckr;

namespace SUSFuckr
{
    public static class Diagnostics
    {
        // nazwy plików, których nie chcemy logowaæ
        private static readonly HashSet<string> _excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Mini.RegionInstall.dll",
            "Reactor.dll",
            "touhats.bundle",
            "touhats.catalog"
        };

        public static void LogModsAndPlugins()
        {
            // 1) zainstalowane mody wg config.json
            var configs = ConfigManager.LoadConfig();
            UIOutput.Write("=== Zainstalowane mody (config.json) ===");
            foreach (var cfg in configs.Where(c => !string.IsNullOrWhiteSpace(c.InstallPath)))
            {
                UIOutput.Write(cfg.ModName);
            }
            UIOutput.Write(string.Empty);

            // 2) „rêczne” mody w folderze ModsInstallPath
            var modsRoot = PathSettings.ModsInstallPath
                           ?? PathSettings.DefaultModsPath;

            UIOutput.Write($"=== Katalogi w folderze: {modsRoot} ===");
            if (Directory.Exists(modsRoot))
            {
                foreach (var dir in Directory.EnumerateDirectories(modsRoot))
                {
                    // wyœwietlamy sam¹ nazwê katalogu
                    UIOutput.Write(Path.GetFileName(dir));
                }
            }
            else
            {
                UIOutput.Write($"Folder nie istnieje: {modsRoot}");
            }

            UIOutput.Write(string.Empty);

            // 3) wyszukiwanie DLL/.bundle/.catalog w BepInEx\plugins
            UIOutput.Write("=== Nie-standardowe pluginy w BepInEx\\plugins ===");
            if (Directory.Exists(modsRoot))
            {
                foreach (var modDir in Directory.GetDirectories(modsRoot))
                {
                    var modName = Path.GetFileName(modDir);
                    var pluginDir = Path.Combine(modDir, "BepInEx", "plugins");
                    if (!Directory.Exists(pluginDir))
                        continue;

                    foreach (var file in Directory.EnumerateFiles(pluginDir))
                    {
                        var fn = Path.GetFileName(file);
                        var ext = Path.GetExtension(fn).ToLowerInvariant();
                        if (_excluded.Contains(fn))
                            continue;

                        if (ext == ".dll" || ext == ".bundle" || ext == ".catalog")
                            UIOutput.Write($"{modName}\\{fn}");
                    }
                }
            }

            UIOutput.Write("=== Koniec diagnostyki ===");
        }
    }
}
using System;
using System.IO;
using System.Windows.Forms;

namespace SUSFuckr
{
    public static class FixBlackScreen
    {
        public static void ExecuteFix()
        {
            DialogResult result = MessageBox.Show("Czy jeste� pewny, �e chcesz zrestartowa� ustawienia gry?", "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string targetDirectory = Path.Combine(userProfile, @"AppData\LocalLow\Innersloth\Among Us");

                try
                {
                    // Usuni�cie zawarto�ci poza plikami .txt i regionInfo.json
                    foreach (var filePath in Directory.GetFiles(targetDirectory))
                    {
                        string fileName = Path.GetFileName(filePath);

                        // Zachowaj wszystkie pliki .txt i regionInfo.json
                        bool isTxtFile = fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
                        bool isRegionInfo = string.Equals(fileName, "regionInfo.json", StringComparison.OrdinalIgnoreCase);

                        if (!isTxtFile && !isRegionInfo)
                        {
                            File.Delete(filePath);
                        }
                    }

                    foreach (var directoryPath in Directory.GetDirectories(targetDirectory))
                    {
                        Directory.Delete(directoryPath, true);
                    }

                    MessageBox.Show("Ustawienia gry zosta�y zresetowane.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wyst�pi� b��d podczas resetowania ustawie�: {ex.Message}", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SUSFuckr
{
    public static class Information
    {
        public static Panel CreateInfoOverlay(Form mainForm, string appVersion, Action removeOverlay)
        {
            Panel overlayPanel = new Panel
            {
                Size = mainForm.ClientSize,
                Location = new Point(0, mainForm.MainMenuStrip?.Bottom ?? 0),
                BackColor = Color.LightGray
            };

            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent,
                Dock = DockStyle.Top,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };

            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Dodaj wiersze do tabeli
            tableLayout.Controls.Add(CreateBoldLabel("Wersja:"), 0, 0);
            tableLayout.Controls.Add(new Label
            {
                Text = appVersion,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 0);

            tableLayout.Controls.Add(CreateBoldLabel("G³ówny Programista:"), 0, 1);
            tableLayout.Controls.Add(new Label
            {
                Text = "ChatGPT",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 1);

            tableLayout.Controls.Add(CreateBoldLabel("Pomocnik programisty:"), 0, 2);
            tableLayout.Controls.Add(new Label
            {
                Text = "boracik",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 2);
            tableLayout.Controls.Add(CreateBoldLabel("Testerzy:"), 0, 3);
            tableLayout.Controls.Add(new Label
            {
                Text = "Spo³ecznoœæ Psychopatyczna",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, 3);
            overlayPanel.Controls.Add(tableLayout);

            // Wyœrodkowanie ikon spo³ecznoœciowych i przycisku „Wróæ” w panelu
            Panel socialPanel = new Panel
            {
                Size = new Size(280, 80),
                Location = new Point((overlayPanel.Width - 220) / 2, 150),
                BackColor = Color.Transparent
            };

            Button returnButton = new Button
            {
                Text = "Wróæ",
                AutoSize = true,
                Location = new Point((socialPanel.Width -120 ) / 2, 50),
            };
            returnButton.Click += (s, ev) => removeOverlay();
            socialPanel.Controls.Add(returnButton);

            PictureBox discordIcon = CreateLinkIcon("Graphics/discord.png", "https://discord.gg/psychopaci", new Point(0, 0));
            PictureBox youtubeIcon = CreateLinkIcon("Graphics/youtube.png", "https://www.youtube.com/@boracikgaming", new Point(60, 0));
            PictureBox kickIcon = CreateLinkIcon("Graphics/kick.png", "https://kick.com/boracik-gaming", new Point(120, 0));
            PictureBox twitchIcon = CreateLinkIcon("Graphics/twitch.png", "https://www.twitch.tv/boracik_gaming", new Point(180, 0));

            socialPanel.Controls.Add(discordIcon);
            socialPanel.Controls.Add(youtubeIcon);
            socialPanel.Controls.Add(kickIcon);
            socialPanel.Controls.Add(twitchIcon);

            overlayPanel.Controls.Add(socialPanel);

            return overlayPanel;
        }

        private static PictureBox CreateLinkIcon(string imagePath, string url, Point location)
        {
            PictureBox icon = new PictureBox
            {
                Size = new Size(32, 32), // Dopasowanie rozmiaru do obrazu
                Location = location,
                Image = Image.FromFile(imagePath),
                SizeMode = PictureBoxSizeMode.StretchImage, // Ustawienia rozmiaru obrazu
                Cursor = Cursors.Hand
            };
            icon.Click += (s, ev) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return icon;
        }

        private static Label CreateBoldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Dock = DockStyle.Fill
            };
        }
    }
}
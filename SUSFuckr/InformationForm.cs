using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D; // Dodano odwo³anie do przestrzeni nazw dla LinearGradientBrush
using System.Windows.Forms;

namespace SUSFuckr
{
    public class InformationForm : Form
    {
        private string appVersion;

        public InformationForm(string appVersion)
        {
            this.appVersion = appVersion ?? "Unknown";
            InitializeForm();
        }

        private void InitializeForm()
        {
            // Stylizowanie okna
            this.Text = "Informacje o aplikacji";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Paint += (s, e) =>
            {
                LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle, Color.LightSteelBlue, Color.LightGray, 45F);
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            };

            // Dodanie przycisku zamkniêcia
            Button closeButton = new Button
            {
                Text = "×",
                Font = new Font("Arial", 16, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(this.Width - 50, 10),
                Size = new Size(40, 40),
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, ev) => this.Close();
            this.Controls.Add(closeButton);

            // Tabela Layout dla informacji
            TableLayoutPanel tableLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                Location = new Point(50, 50),
                BackColor = Color.Transparent
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.Controls.Add(CreateBoldLabel("Wersja:"), 0, 0);
            tableLayout.Controls.Add(CreateInfoLabel(appVersion), 1, 0);
            tableLayout.Controls.Add(CreateBoldLabel("G³ówny Programista:"), 0, 1);
            tableLayout.Controls.Add(CreateInfoLabel("ChatGPT"), 1, 1);
            tableLayout.Controls.Add(CreateBoldLabel("Pomocnik programisty:"), 0, 2);
            tableLayout.Controls.Add(CreateInfoLabel("boracik"), 1, 2);
            tableLayout.Controls.Add(CreateBoldLabel("Testerzy:"), 0, 3);
            tableLayout.Controls.Add(CreateInfoLabel("Spo³ecznoœæ Psychopatyczna"), 1, 3);
            this.Controls.Add(tableLayout);

            // Œrodkowa linia dla ikon spo³ecznoœciowych
            int iconWidth = 48;
            int gap = 20;
            int totalWidth = 4 * iconWidth + 3 * gap; // Szerokoœæ wszystkich ikon + odstêpy
            int startingPoint = (this.Width - totalWidth) / 2; // Punkt startowy na formularzu

            AddSocialIcon("Graphics/discord.png", "https://discord.gg/psychopaci", new Point(startingPoint, 250));
            AddSocialIcon("Graphics/youtube.png", "https://www.youtube.com/@boracikgaming", new Point(startingPoint + iconWidth + gap, 250));
            AddSocialIcon("Graphics/kick.png", "https://kick.com/boracik-gaming", new Point(startingPoint + 2 * (iconWidth + gap), 250));
            AddSocialIcon("Graphics/twitch.png", "https://www.twitch.tv/boracik_gaming", new Point(startingPoint + 3 * (iconWidth + gap), 250));
        }

        private void AddSocialIcon(string imagePath, string url, Point location)
        {
            PictureBox icon = new PictureBox
            {
                Size = new Size(48, 48),
                Location = location,
                Image = Image.FromFile(imagePath),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Cursor = Cursors.Hand,
               // BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Transparent,
            };
            icon.Click += (s, ev) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            this.Controls.Add(icon);
        }

        private static Label CreateBoldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 12, FontStyle.Bold), // Zmiana czcionki na Segoe UI
                ForeColor = Color.DarkSlateGray // Ustawienie ciemniejszego koloru czcionki
            };
        }

        private static Label CreateInfoLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12), // Zmiana czcionki na Segoe UI
                ForeColor = Color.DarkSlateGray // Ustawienie ciemniejszego koloru czcionki
            };
        }
    }
}
using System;
using System.IO;
using System.Windows.Forms;

namespace SUSFuckr
{
    public partial class PathSettingsForm : Form
    {
        public PathSettingsForm()
        {
            InitializeComponent();
            txtPath.Text = PathSettings.ModsInstallPath ?? string.Empty; // Poprawka linii 12
        }

        private void InitializeComponent()
        {
            this.Text = "Ustawienia œcie¿ki modów";
            this.Size = new System.Drawing.Size(500, 180);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblPath = new Label
            {
                Text = "Œcie¿ka instalacji modów:",
                Location = new System.Drawing.Point(12, 15),
                AutoSize = true
            };

            txtPath = new TextBox
            {
                Location = new System.Drawing.Point(12, 38),
                Width = 360
            };

            var btnBrowse = new Button
            {
                Text = "Przegl¹daj...",
                Location = new System.Drawing.Point(378, 36),
                Width = 90
            };
            btnBrowse.Click += btnBrowse_Click; // Poprawiona sygnatura

            var btnSave = new Button
            {
                Text = "Zapisz",
                Location = new System.Drawing.Point(12, 80),
                Width = 80
            };
            btnSave.Click += btnSave_Click; // Poprawiona sygnatura

            var btnCancel = new Button
            {
                Text = "Anuluj",
                Location = new System.Drawing.Point(98, 80),
                Width = 80,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += btnCancel_Click; // Poprawiona sygnatura

            var btnReset = new Button
            {
                Text = "Resetuj do domyœlnej",
                Location = new System.Drawing.Point(184, 80),
                Width = 140
            };
            btnReset.Click += btnReset_Click; // Poprawiona sygnatura

            this.Controls.AddRange(new Control[] { lblPath, txtPath, btnBrowse, btnSave, btnCancel, btnReset });
            this.CancelButton = btnCancel;
        }

        private TextBox txtPath = null!;

        private void btnBrowse_Click(object? sender, EventArgs e) // Poprawiona sygnatura
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Wybierz folder do instalacji modów";
                dialog.SelectedPath = txtPath.Text;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object? sender, EventArgs e) // Poprawiona sygnatura
        {
            if (Directory.Exists(txtPath.Text) || string.IsNullOrWhiteSpace(txtPath.Text))
            {
                PathSettings.ModsInstallPath = string.IsNullOrWhiteSpace(txtPath.Text) ? string.Empty : txtPath.Text;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Podana œcie¿ka nie istnieje.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e) // Poprawiona sygnatura
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnReset_Click(object? sender, EventArgs e) // Poprawiona sygnatura
        {
            txtPath.Text = PathSettings.DefaultModsPath;
        }

    }
}

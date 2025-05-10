using System;
using System.Windows.Forms;
using System.IO;

namespace SUSFuckr
{
    public partial class PathSettingsForm : Form
    {
        public PathSettingsForm()
        {
            InitializeComponent();
            txtModsPath.Text = PathSettings.ModsInstallPath;
        }

        private void InitializeComponent()
        {
            this.txtModsPath = new TextBox();
            this.btnBrowse = new Button();
            this.btnSave = new Button();
            this.btnCancel = new Button();
            this.btnReset = new Button();
            this.lblInfo = new Label();

            // txtModsPath
            this.txtModsPath.Location = new System.Drawing.Point(12, 40);
            this.txtModsPath.Name = "txtModsPath";
            this.txtModsPath.Size = new System.Drawing.Size(350, 23);
            this.txtModsPath.TabIndex = 0;

            // btnBrowse
            this.btnBrowse.Location = new System.Drawing.Point(368, 40);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Przegl�daj";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new EventHandler(btnBrowse_Click);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(287, 80);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Zapisz";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new EventHandler(btnSave_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(368, 80);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Anuluj";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new EventHandler(btnCancel_Click);

            // btnReset
            this.btnReset.Location = new System.Drawing.Point(12, 80);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(120, 23);
            this.btnReset.TabIndex = 4;
            this.btnReset.Text = "Przywr�� domy�ln�";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new EventHandler(btnReset_Click);

            // lblInfo
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(12, 15);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(250, 15);
            this.lblInfo.TabIndex = 5;
            this.lblInfo.Text = "Wybierz katalog instalacji mod�w:";

            // PathSettingsForm
            this.ClientSize = new System.Drawing.Size(455, 120);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtModsPath);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PathSettingsForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Ustawienia �cie�ki instalacji";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Wybierz katalog instalacji mod�w";
                folderDialog.UseDescriptionForTitle = true;

                if (!string.IsNullOrEmpty(txtModsPath.Text) && Directory.Exists(txtModsPath.Text))
                {
                    folderDialog.InitialDirectory = txtModsPath.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtModsPath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtModsPath.Text))
            {
                MessageBox.Show("�cie�ka nie mo�e by� pusta.", "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Sprawd� czy katalog istnieje, je�li nie - utw�rz
                if (!Directory.Exists(txtModsPath.Text))
                {
                    var result = MessageBox.Show(
                        "Wybrany katalog nie istnieje. Czy chcesz go utworzy�?",
                        "Katalog nie istnieje",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Directory.CreateDirectory(txtModsPath.Text);
                    }
                    else
                    {
                        return;
                    }
                }

                // Sprawd� uprawnienia do zapisu
                try
                {
                    string testFile = Path.Combine(txtModsPath.Text, "test_write_permission.tmp");
                    File.WriteAllText(testFile, "Test");
                    File.Delete(testFile);
                }
                catch
                {
                    MessageBox.Show(
                        "Brak uprawnie� do zapisu w wybranym katalogu. Wybierz inny katalog lub uruchom aplikacj� jako administrator.",
                        "Brak uprawnie�",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Zapisz �cie�k�
                PathSettings.ModsInstallPath = txtModsPath.Text;

                MessageBox.Show(
                    "�cie�ka instalacji mod�w zosta�a zmieniona. Nowe mody b�d� instalowane w wybranej lokalizacji.",
                    "Sukces",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Wyst�pi� b��d podczas zapisywania �cie�ki: {ex.Message}",
                    "B��d",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            txtModsPath.Text = PathSettings.DefaultModsPath;
        }

        private TextBox txtModsPath;
        private Button btnBrowse = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Button btnReset = null!;
        private Label lblInfo = null!;
    }
}
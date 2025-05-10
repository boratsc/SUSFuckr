namespace SUSFuckr
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox textBoxPath;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Button btnLaunch;
        private System.Windows.Forms.Button btnModify;
        private System.Windows.Forms.Button btnFixBlackScreen;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnUpdateMod;
        private System.Windows.Forms.Panel contentPanel;
        private System.Windows.Forms.Panel scrollablePanel;
        private System.Windows.Forms.RichTextBox txtLegendaryLog;
        private System.Windows.Forms.ProgressBar progressBar; // Dodaj kontrolkę ProgressBar

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {

            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.labelVersion = new System.Windows.Forms.Label();
            this.btnLaunch = new System.Windows.Forms.Button();
            this.btnModify = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUpdateMod = new System.Windows.Forms.Button();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.scrollablePanel = new System.Windows.Forms.Panel();
            this.btnFixBlackScreen = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar(); // Inicjalizacja ProgressBar

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 590); // Zwiększona wysokość dla paska
            this.BackColor = Color.LightGray;

            this.txtLegendaryLog = new System.Windows.Forms.RichTextBox();

            // txtLegendaryLog
            this.txtLegendaryLog.Location = new System.Drawing.Point(7, 461);
            this.txtLegendaryLog.Name = "txtLegendaryLog";
            this.txtLegendaryLog.Size = new System.Drawing.Size(600, 100);
            this.txtLegendaryLog.TabIndex = 10;
            this.txtLegendaryLog.Text = "";
            this.Controls.Add(this.txtLegendaryLog);

            // contentPanel
            this.contentPanel.Location = new System.Drawing.Point(7, 160); // Panel o 7px mniejszy niż okno
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(600, 300); // Przystosowane do formy prostokąta
            this.contentPanel.BackColor = SystemColors.Control;
            this.Controls.Add(this.contentPanel);

            // scrollablePanel
            this.scrollablePanel.Location = new System.Drawing.Point(7, 30); // Panel o 7px mniejszy niż okno
            this.scrollablePanel.Name = "scrollablePanel";
            this.scrollablePanel.Size = new System.Drawing.Size(600, 120); // Miejsce na nowe ikony lub interakcje
            this.scrollablePanel.BackColor = SystemColors.Control;
            this.scrollablePanel.AutoScroll = true; // Włączenie przewijania
            this.scrollablePanel.Scroll += (s, e) =>
            {
                this.scrollablePanel.VerticalScroll.Value = 0; // Przy każdej próbie zmiany resetuj do zera
            };
            this.Controls.Add(this.scrollablePanel);

            // Relokacja kontrolek w contentPanel
            this.contentPanel.Controls.Add(this.browseButton);
            this.contentPanel.Controls.Add(this.textBoxPath);
            this.contentPanel.Controls.Add(this.labelVersion);
            this.contentPanel.Controls.Add(this.btnLaunch);
            this.contentPanel.Controls.Add(this.btnModify);
            this.contentPanel.Controls.Add(this.btnDelete);
            this.contentPanel.Controls.Add(this.btnUpdateMod);
            this.contentPanel.Controls.Add(this.progressBar); // Dodanie ProgressBar

            // Btn Launch
            this.btnLaunch.Location = new System.Drawing.Point(20, 10);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(560, 30);
            this.btnLaunch.TabIndex = 4;
            this.btnLaunch.Text = "Uruchom";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Image = Image.FromFile("Graphics/UI/play.png");
            this.btnLaunch.ImageAlign = ContentAlignment.MiddleLeft;
            this.btnLaunch.TextAlign = ContentAlignment.MiddleCenter;
            this.btnLaunch.Click += new System.EventHandler(this.LaunchButton_Click);

            // Btn Modify
            this.btnModify.Location = new System.Drawing.Point(20, 50);
            this.btnModify.Name = "btnModify";
            this.btnModify.Size = new System.Drawing.Size(560, 30);
            this.btnModify.TabIndex = 5;
            this.btnModify.Text = "Instaluj moda";
            this.btnModify.UseVisualStyleBackColor = true;
            this.btnModify.Enabled = false;
            this.btnModify.Image = Image.FromFile("Graphics/UI/install.png");
            this.btnModify.ImageAlign = ContentAlignment.MiddleLeft;
            this.btnModify.TextAlign = ContentAlignment.MiddleCenter;
            this.btnModify.Click += new System.EventHandler(this.ModifyButton_Click);

            // Btn Delete
            this.btnDelete.Location = new System.Drawing.Point(20, 90);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(560, 30);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "Usuń";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Enabled = false;
            this.btnDelete.Image = Image.FromFile("Graphics/UI/uninstall.png");
            this.btnDelete.ImageAlign = ContentAlignment.MiddleLeft;
            this.btnDelete.TextAlign = ContentAlignment.MiddleCenter;
            this.btnDelete.Click += new System.EventHandler(this.DeleteButton_Click);

            // Btn Update Mod
            this.btnUpdateMod.Location = new System.Drawing.Point(20, 130);
            this.btnUpdateMod.Name = "btnUpdateMod";
            this.btnUpdateMod.Size = new System.Drawing.Size(560, 30);
            this.btnUpdateMod.TabIndex = 7;
            this.btnUpdateMod.Text = "Aktualizuj moda";
            this.btnUpdateMod.UseVisualStyleBackColor = true;
            this.btnUpdateMod.Enabled = false;
            this.btnUpdateMod.Image = Image.FromFile("Graphics/UI/update.png");
            this.btnUpdateMod.ImageAlign = ContentAlignment.MiddleLeft;
            this.btnUpdateMod.TextAlign = ContentAlignment.MiddleCenter;
            this.btnUpdateMod.Click += new System.EventHandler(this.UpdateModButton_Click);

            // TextBox Path
            this.textBoxPath.Location = new System.Drawing.Point(140, 170);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.ReadOnly = true;
            this.textBoxPath.Size = new System.Drawing.Size(440, 26);
            this.textBoxPath.TabIndex = 0;

            // Browse Button
            this.browseButton.Location = new System.Drawing.Point(20, 170);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(100, 30);
            this.browseButton.TabIndex = 1;
            this.browseButton.Text = "Wskaż ręcznie";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);

            // Label Version
            this.labelVersion.Location = new System.Drawing.Point(20, 205);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(400, 30);
            this.labelVersion.TabIndex = 2;
            this.labelVersion.Text = "Wersja gry: Nieznana";

            // ProgressBar - dodanie kontrolki
            this.progressBar.Location = new System.Drawing.Point(20, 235);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(560, 30);
            this.progressBar.TabIndex = 8;
            this.progressBar.Visible = false; // Ukryty na starcie

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
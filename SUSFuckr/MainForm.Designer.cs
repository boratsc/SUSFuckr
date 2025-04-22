namespace SUSFuckr
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox textBoxPath;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.PictureBox gameIcon;
        private System.Windows.Forms.Label labelGame;
        private System.Windows.Forms.Button btnLaunch;
        private System.Windows.Forms.Button btnModify;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnUpdateMod;

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
            this.gameIcon = new System.Windows.Forms.PictureBox();
            this.labelGame = new System.Windows.Forms.Label();
            this.btnLaunch = new System.Windows.Forms.Button();
            this.btnModify = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUpdateMod = new System.Windows.Forms.Button();

            // Configure components here
            //
            // gameIcon
            //
            this.gameIcon.Location = new System.Drawing.Point(20, 20); // Przesunięcie na bok
            this.gameIcon.Name = "gameIcon";
            this.gameIcon.Size = new System.Drawing.Size(64, 64);
            this.gameIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            this.gameIcon.Image = Image.FromFile("Graphics/Among1.png");
            this.gameIcon.Click += new System.EventHandler(this.GameIcon_Click);
            //
            // labelGame
            //
            this.labelGame.Location = new System.Drawing.Point(20, 84);
            this.labelGame.Name = "labelGame";
            this.labelGame.Size = new System.Drawing.Size(100, 30);
            this.labelGame.TabIndex = 3;
            this.labelGame.Text = "Among Us";
            //
            // btnLaunch
            //
            this.btnLaunch.Location = new System.Drawing.Point(20, 120);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(500, 30);
            this.btnLaunch.TabIndex = 4;
            this.btnLaunch.Text = "Uruchom";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.LaunchButton_Click);
            //
            // btnModify
            //
            this.btnModify.Location = new System.Drawing.Point(20, 160);
            this.btnModify.Name = "btnModify";
            this.btnModify.Size = new System.Drawing.Size(500, 30);
            this.btnModify.TabIndex = 5;
            this.btnModify.Text = "Modyfikuj";
            this.btnModify.UseVisualStyleBackColor = true;
            this.btnModify.Enabled = false;
            //
            // btnDelete
            //
            this.btnDelete.Location = new System.Drawing.Point(20, 200);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(500, 30);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "Usuń";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Enabled = false;
            //
            // btnUpdateMod
            //
            this.btnUpdateMod.Location = new System.Drawing.Point(20, 240);
            this.btnUpdateMod.Name = "btnUpdateMod";
            this.btnUpdateMod.Size = new System.Drawing.Size(500, 30);
            this.btnUpdateMod.TabIndex = 7;
            this.btnUpdateMod.Text = "Aktualizuj moda";
            this.btnUpdateMod.UseVisualStyleBackColor = true;
            this.btnUpdateMod.Enabled = false;
            //
            // textBoxPath
            //
            this.textBoxPath.Location = new System.Drawing.Point(140, 290); // Przemieszczenie na dół okna
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.ReadOnly = true;
            this.textBoxPath.Size = new System.Drawing.Size(400, 26);
            this.textBoxPath.TabIndex = 0;
            //
            // browseButton
            //
            this.browseButton.Location = new System.Drawing.Point(20, 290); // Przemieszczenie na dół okna, obok TextBox
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(100, 30);
            this.browseButton.TabIndex = 1;
            this.browseButton.Text = "Wskaż ręcznie";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            //
            // labelVersion
            //
            this.labelVersion.Location = new System.Drawing.Point(20, 325);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(400, 30);
            this.labelVersion.TabIndex = 2;
            this.labelVersion.Text = "Wersja gry: Nieznana";
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.textBoxPath);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.gameIcon);
            this.Controls.Add(this.labelGame);
            this.Controls.Add(this.btnLaunch);
            this.Controls.Add(this.btnModify);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnUpdateMod);
            this.Name = "MainForm";
            this.Text = "SUSFuckr";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
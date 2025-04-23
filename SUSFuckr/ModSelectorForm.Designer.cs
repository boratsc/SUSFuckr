namespace SUSFuckr
{
    partial class ModSelectorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox AvailableModsListBox;
        private System.Windows.Forms.Button btnOk;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AvailableModsListBox = new System.Windows.Forms.ListBox();
            this.btnOk = new System.Windows.Forms.Button();

            this.AvailableModsListBox.Location = new System.Drawing.Point(12, 12);
            this.AvailableModsListBox.Name = "AvailableModsListBox";
            this.AvailableModsListBox.Size = new System.Drawing.Size(260, 200);

            this.btnOk.Location = new System.Drawing.Point(12, 220);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(260, 30);
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);

            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.AvailableModsListBox);
            this.Controls.Add(this.btnOk);
            this.Name = "ModSelectorForm";
            this.Text = "Wybierz Mody";
            this.ResumeLayout(false);
        }

        #endregion
    }
}
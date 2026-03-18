namespace PdfMerger
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelDrop = new System.Windows.Forms.Panel();
            this.lblDropHint = new System.Windows.Forms.Label();
            this.pdfListPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFolderTitle = new System.Windows.Forms.Label();
            this.lblFolder = new System.Windows.Forms.Label();
            this.btnChangeFolder = new System.Windows.Forms.Button();
            this.btnMerge = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelDrop.SuspendLayout();
            this.SuspendLayout();

            // panelDrop
            this.panelDrop.AllowDrop = true;
            this.panelDrop.BackColor = System.Drawing.Color.FromArgb(245, 248, 255);
            this.panelDrop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelDrop.Controls.Add(this.lblDropHint);
            this.panelDrop.Controls.Add(this.pdfListPanel);
            this.panelDrop.Location = new System.Drawing.Point(20, 20);
            this.panelDrop.Name = "panelDrop";
            this.panelDrop.Size = new System.Drawing.Size(460, 220);
            this.panelDrop.TabIndex = 0;
            this.panelDrop.DragEnter += new System.Windows.Forms.DragEventHandler(this.panelDrop_DragEnter);
            this.panelDrop.DragDrop += new System.Windows.Forms.DragEventHandler(this.panelDrop_DragDrop);
            this.panelDrop.DragOver += new System.Windows.Forms.DragEventHandler(this.panelDrop_DragOver);
            this.panelDrop.Click += new System.EventHandler(this.panelDrop_Click);
            this.panelDrop.Cursor = System.Windows.Forms.Cursors.Hand;

            // lblDropHint
            this.lblDropHint.AutoSize = false;
            this.lblDropHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDropHint.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Regular);
            this.lblDropHint.ForeColor = System.Drawing.Color.FromArgb(160, 160, 180);
            this.lblDropHint.Name = "lblDropHint";
            this.lblDropHint.Size = new System.Drawing.Size(458, 218);
            this.lblDropHint.TabIndex = 0;
            this.lblDropHint.Text = "גרור לכאן עד 3 קבצי PDF\r\nאו לחץ לבחירה";
            this.lblDropHint.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblDropHint.Click += new System.EventHandler(this.panelDrop_Click);
            this.lblDropHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // pdfListPanel
            this.pdfListPanel.AutoSize = false;
            this.pdfListPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pdfListPanel.Location = new System.Drawing.Point(0, 0);
            this.pdfListPanel.Name = "pdfListPanel";
            this.pdfListPanel.Size = new System.Drawing.Size(458, 218);
            this.pdfListPanel.TabIndex = 1;
            this.pdfListPanel.WrapContents = false;
            this.pdfListPanel.Padding = new System.Windows.Forms.Padding(10, 15, 10, 10);
            this.pdfListPanel.Click += new System.EventHandler(this.panelDrop_Click);

            // lblFolderTitle
            this.lblFolderTitle.AutoSize = true;
            this.lblFolderTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFolderTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this.lblFolderTitle.Location = new System.Drawing.Point(20, 260);
            this.lblFolderTitle.Name = "lblFolderTitle";
            this.lblFolderTitle.Size = new System.Drawing.Size(120, 15);
            this.lblFolderTitle.TabIndex = 1;
            this.lblFolderTitle.Text = "תיקיית שמירה:";

            // lblFolder
            this.lblFolder.AutoSize = false;
            this.lblFolder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFolder.ForeColor = System.Drawing.Color.FromArgb(60, 100, 180);
            this.lblFolder.Location = new System.Drawing.Point(20, 280);
            this.lblFolder.Name = "lblFolder";
            this.lblFolder.Size = new System.Drawing.Size(340, 20);
            this.lblFolder.TabIndex = 2;
            this.lblFolder.Text = "לא נבחרה תיקייה";
            this.lblFolder.AutoEllipsis = true;

            // btnChangeFolder
            this.btnChangeFolder.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(100, 140, 220);
            this.btnChangeFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChangeFolder.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnChangeFolder.ForeColor = System.Drawing.Color.FromArgb(60, 100, 180);
            this.btnChangeFolder.Location = new System.Drawing.Point(370, 276);
            this.btnChangeFolder.Name = "btnChangeFolder";
            this.btnChangeFolder.Size = new System.Drawing.Size(110, 28);
            this.btnChangeFolder.TabIndex = 3;
            this.btnChangeFolder.Text = "שנה תיקייה";
            this.btnChangeFolder.UseVisualStyleBackColor = true;
            this.btnChangeFolder.Click += new System.EventHandler(this.btnChangeFolder_Click);

            // btnMerge
            this.btnMerge.BackColor = System.Drawing.Color.FromArgb(60, 120, 220);
            this.btnMerge.Enabled = false;
            this.btnMerge.FlatAppearance.BorderSize = 0;
            this.btnMerge.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMerge.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnMerge.ForeColor = System.Drawing.Color.White;
            this.btnMerge.Location = new System.Drawing.Point(130, 320);
            this.btnMerge.Name = "btnMerge";
            this.btnMerge.Size = new System.Drawing.Size(240, 44);
            this.btnMerge.TabIndex = 4;
            this.btnMerge.Text = "מזג PDF'ים";
            this.btnMerge.UseVisualStyleBackColor = false;
            this.btnMerge.Click += new System.EventHandler(this.btnMerge_Click);

            // lblStatus
            this.lblStatus.AutoSize = false;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(60, 160, 80);
            this.lblStatus.Location = new System.Drawing.Point(20, 375);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(460, 20);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(500, 410);
            this.Controls.Add(this.panelDrop);
            this.Controls.Add(this.lblFolderTitle);
            this.Controls.Add(this.lblFolder);
            this.Controls.Add(this.btnChangeFolder);
            this.Controls.Add(this.btnMerge);
            this.Controls.Add(this.lblStatus);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PDF Merger - מאחד קבצי PDF";
            this.panelDrop.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel panelDrop;
        private System.Windows.Forms.Label lblDropHint;
        private System.Windows.Forms.FlowLayoutPanel pdfListPanel;
        private System.Windows.Forms.Label lblFolderTitle;
        private System.Windows.Forms.Label lblFolder;
        private System.Windows.Forms.Button btnChangeFolder;
        private System.Windows.Forms.Button btnMerge;
        private System.Windows.Forms.Label lblStatus;
    }
}

namespace RECV_Editor
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DebugExtractButton = new System.Windows.Forms.Button();
            this.DebugDecompressButton = new System.Windows.Forms.Button();
            this.DebugGroup = new System.Windows.Forms.GroupBox();
            this.ExtractAllButton = new System.Windows.Forms.Button();
            this.StatusStrip = new System.Windows.Forms.StatusStrip();
            this.StatusProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.StatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.InsertAllButton = new System.Windows.Forms.Button();
            this.DebugGroup.SuspendLayout();
            this.StatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // DebugExtractButton
            // 
            this.DebugExtractButton.Location = new System.Drawing.Point(6, 19);
            this.DebugExtractButton.Name = "DebugExtractButton";
            this.DebugExtractButton.Size = new System.Drawing.Size(166, 37);
            this.DebugExtractButton.TabIndex = 0;
            this.DebugExtractButton.Text = "Extract";
            this.DebugExtractButton.UseVisualStyleBackColor = true;
            this.DebugExtractButton.Click += new System.EventHandler(this.DebugExtractButton_Click);
            // 
            // DebugDecompressButton
            // 
            this.DebugDecompressButton.Location = new System.Drawing.Point(177, 19);
            this.DebugDecompressButton.Name = "DebugDecompressButton";
            this.DebugDecompressButton.Size = new System.Drawing.Size(166, 37);
            this.DebugDecompressButton.TabIndex = 1;
            this.DebugDecompressButton.Text = "Decompress";
            this.DebugDecompressButton.UseVisualStyleBackColor = true;
            this.DebugDecompressButton.Click += new System.EventHandler(this.DebugDecompressButton_Click);
            // 
            // DebugGroup
            // 
            this.DebugGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DebugGroup.Controls.Add(this.DebugExtractButton);
            this.DebugGroup.Controls.Add(this.DebugDecompressButton);
            this.DebugGroup.Location = new System.Drawing.Point(12, 69);
            this.DebugGroup.Name = "DebugGroup";
            this.DebugGroup.Size = new System.Drawing.Size(349, 66);
            this.DebugGroup.TabIndex = 2;
            this.DebugGroup.TabStop = false;
            this.DebugGroup.Text = "Debug";
            // 
            // ExtractAllButton
            // 
            this.ExtractAllButton.Location = new System.Drawing.Point(12, 12);
            this.ExtractAllButton.Name = "ExtractAllButton";
            this.ExtractAllButton.Size = new System.Drawing.Size(172, 51);
            this.ExtractAllButton.TabIndex = 3;
            this.ExtractAllButton.Text = "Extract All...";
            this.ExtractAllButton.UseVisualStyleBackColor = true;
            this.ExtractAllButton.Click += new System.EventHandler(this.ExtractAllButton_Click);
            // 
            // StatusStrip
            // 
            this.StatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StatusProgressBar,
            this.StatusLabel});
            this.StatusStrip.Location = new System.Drawing.Point(0, 140);
            this.StatusStrip.Name = "StatusStrip";
            this.StatusStrip.Size = new System.Drawing.Size(373, 22);
            this.StatusStrip.SizingGrip = false;
            this.StatusStrip.TabIndex = 4;
            this.StatusStrip.Text = "statusStrip1";
            // 
            // StatusProgressBar
            // 
            this.StatusProgressBar.Margin = new System.Windows.Forms.Padding(10, 3, 1, 3);
            this.StatusProgressBar.Name = "StatusProgressBar";
            this.StatusProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // StatusLabel
            // 
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(118, 17);
            this.StatusLabel.Text = "toolStripStatusLabel1";
            // 
            // InsertAllButton
            // 
            this.InsertAllButton.Location = new System.Drawing.Point(191, 13);
            this.InsertAllButton.Name = "InsertAllButton";
            this.InsertAllButton.Size = new System.Drawing.Size(170, 50);
            this.InsertAllButton.TabIndex = 5;
            this.InsertAllButton.Text = "Insert All...";
            this.InsertAllButton.UseVisualStyleBackColor = true;
            this.InsertAllButton.Click += new System.EventHandler(this.InsertAllButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(373, 162);
            this.Controls.Add(this.InsertAllButton);
            this.Controls.Add(this.StatusStrip);
            this.Controls.Add(this.ExtractAllButton);
            this.Controls.Add(this.DebugGroup);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Resident Evil: Code Veronica - Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.DebugGroup.ResumeLayout(false);
            this.StatusStrip.ResumeLayout(false);
            this.StatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button DebugExtractButton;
        private System.Windows.Forms.Button DebugDecompressButton;
        private System.Windows.Forms.GroupBox DebugGroup;
        private System.Windows.Forms.Button ExtractAllButton;
        private System.Windows.Forms.StatusStrip StatusStrip;
        private System.Windows.Forms.ToolStripProgressBar StatusProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel StatusLabel;
        private System.Windows.Forms.Button InsertAllButton;
    }
}


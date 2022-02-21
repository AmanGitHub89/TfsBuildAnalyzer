namespace TfsBuildAnalyzerDatabaseInstaller
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
            this.btnInstall = new System.Windows.Forms.Button();
            this.scriptsRanTexBox = new System.Windows.Forms.TextBox();
            this.txtSqlServerName = new System.Windows.Forms.TextBox();
            this.lblSqlServerName = new System.Windows.Forms.Label();
            this.lblSuccessLabel = new System.Windows.Forms.Label();
            this.lblSqlServerConnectionStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnInstall
            // 
            this.btnInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInstall.Enabled = false;
            this.btnInstall.Location = new System.Drawing.Point(688, 51);
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new System.Drawing.Size(112, 37);
            this.btnInstall.TabIndex = 0;
            this.btnInstall.Text = "Install";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new System.EventHandler(this.btnInstall_Click);
            // 
            // scriptsRanTexBox
            // 
            this.scriptsRanTexBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptsRanTexBox.Location = new System.Drawing.Point(13, 94);
            this.scriptsRanTexBox.Multiline = true;
            this.scriptsRanTexBox.Name = "scriptsRanTexBox";
            this.scriptsRanTexBox.ReadOnly = true;
            this.scriptsRanTexBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.scriptsRanTexBox.Size = new System.Drawing.Size(787, 344);
            this.scriptsRanTexBox.TabIndex = 1;
            // 
            // txtSqlServerName
            // 
            this.txtSqlServerName.Location = new System.Drawing.Point(13, 39);
            this.txtSqlServerName.Name = "txtSqlServerName";
            this.txtSqlServerName.Size = new System.Drawing.Size(227, 20);
            this.txtSqlServerName.TabIndex = 2;
            this.txtSqlServerName.Leave += new System.EventHandler(this.txtSqlServerName_Leave);
            // 
            // lblSqlServerName
            // 
            this.lblSqlServerName.AutoSize = true;
            this.lblSqlServerName.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSqlServerName.Location = new System.Drawing.Point(13, 12);
            this.lblSqlServerName.Name = "lblSqlServerName";
            this.lblSqlServerName.Size = new System.Drawing.Size(113, 17);
            this.lblSqlServerName.TabIndex = 3;
            this.lblSqlServerName.Text = "Sql Server name";
            // 
            // lblSuccessLabel
            // 
            this.lblSuccessLabel.AutoSize = true;
            this.lblSuccessLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSuccessLabel.Location = new System.Drawing.Point(13, 66);
            this.lblSuccessLabel.Name = "lblSuccessLabel";
            this.lblSuccessLabel.Size = new System.Drawing.Size(127, 22);
            this.lblSuccessLabel.TabIndex = 4;
            this.lblSuccessLabel.Text = "Success Label";
            // 
            // lblSqlServerConnectionStatus
            // 
            this.lblSqlServerConnectionStatus.AutoSize = true;
            this.lblSqlServerConnectionStatus.Location = new System.Drawing.Point(246, 42);
            this.lblSqlServerConnectionStatus.Name = "lblSqlServerConnectionStatus";
            this.lblSqlServerConnectionStatus.Size = new System.Drawing.Size(0, 13);
            this.lblSqlServerConnectionStatus.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(812, 450);
            this.Controls.Add(this.lblSqlServerConnectionStatus);
            this.Controls.Add(this.lblSuccessLabel);
            this.Controls.Add(this.lblSqlServerName);
            this.Controls.Add(this.txtSqlServerName);
            this.Controls.Add(this.scriptsRanTexBox);
            this.Controls.Add(this.btnInstall);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Install Sql Database for Night Run tool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnInstall;
        private System.Windows.Forms.TextBox scriptsRanTexBox;
        private System.Windows.Forms.TextBox txtSqlServerName;
        private System.Windows.Forms.Label lblSqlServerName;
        private System.Windows.Forms.Label lblSuccessLabel;
        private System.Windows.Forms.Label lblSqlServerConnectionStatus;
    }
}


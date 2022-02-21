using System;
using System.Windows.Forms;

namespace TfsBuildAnalyzerDatabaseInstaller
{
    public partial class Form1 : Form
    {
        private bool myIsConnectingToServer;
        private bool myRecheckConnection;
        private bool myLastCanConnectStatus;

        public Form1()
        {
            InitializeComponent();
            txtSqlServerName.Text = LocalDatabaseInstaller.GetServer();
            txtSqlServerName.TextChanged += txtSqlServerName_TextChanged;
            lblSuccessLabel.Text = string.Empty;
            UpdateSqlServerConnectionStatus();
        }

        private void btnInstall_Click(object sender, EventArgs e)
        {
            if (myIsConnectingToServer) return;
            lblSuccessLabel.Text = string.Empty;
            scriptsRanTexBox.Text = string.Empty;

            var success = LocalDatabaseInstaller.SetupDatabase(status =>
            {
                scriptsRanTexBox.Text += $"{status}{Environment.NewLine}{Environment.NewLine}";
            });

            if (success != null)
            {
                lblSuccessLabel.Text = success.Value ? "SUCCESS : Database created/updated successfully." : "FAILURE : Database creation/updation failed.";
            }
        }

        private void txtSqlServerName_TextChanged(object sender, EventArgs e)
        {
            LocalDatabaseInstaller.SetServer(txtSqlServerName.Text.Trim());
            myRecheckConnection = true;
        }

        private async void UpdateSqlServerConnectionStatus()
        {
            lblSuccessLabel.Text = string.Empty;
            scriptsRanTexBox.Text = string.Empty;

            myIsConnectingToServer = true;
            btnInstall.Enabled = false;
            txtSqlServerName.Enabled = false;

            lblSqlServerConnectionStatus.Text = "Connecting...";
            var canConnect = await LocalDatabaseInstaller.CanConnectToServer();
            myLastCanConnectStatus = canConnect;
            lblSqlServerConnectionStatus.Text = canConnect ? "Connection successful." : "Could not connect to server. Make sure Sql Server service is running.";

            txtSqlServerName.Enabled = true;
            btnInstall.Enabled = canConnect;
            myIsConnectingToServer = false;
        }

        private void txtSqlServerName_Leave(object sender, EventArgs e)
        {
            if (myRecheckConnection || !myLastCanConnectStatus)
            {
                myRecheckConnection = false;
                UpdateSqlServerConnectionStatus();
            }
        }
    }
}

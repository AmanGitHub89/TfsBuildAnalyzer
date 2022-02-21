using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private static readonly DatabaseSettings myDatabaseSettings;
        private readonly Action<DatabaseSettings> myOnSettingsClosed;
        public static EventHandler ProjectStructureCleared;

        static SettingsWindow()
        {
            myDatabaseSettings = new DatabaseSettings
            {
                ConnectToDatabase = TfsBuildAnalyzerConfiguration.ConnectToDatabase,
                ConnectionTimeout = TfsBuildAnalyzerConfiguration.ConnectionTimeout,
                CommandTimeout = TfsBuildAnalyzerConfiguration.CommandTimeout,
                ConsistentFailureMinFailCount = TfsBuildAnalyzerConfiguration.ConsistentFailureMinimumFailCount,
                SqlServerName = TfsBuildAnalyzerConfiguration.SqlServer
            };
        }

        public SettingsWindow(Action<DatabaseSettings> onSettingsClosed)
        {
            myOnSettingsClosed = onSettingsClosed;
            InitializeComponent();

            myDatabaseSettings.ConnectToDatabase = TfsBuildAnalyzerConfiguration.ConnectToDatabase;
            myDatabaseSettings.ConsistentFailureMinFailCount = TfsBuildAnalyzerConfiguration.ConsistentFailureMinimumFailCount;

            DatabaseOptionsPanel.IsEnabled = myDatabaseSettings.ConnectToDatabase;
            ConnectionTimeoutTextBox.Text = myDatabaseSettings.ConnectionTimeout.ToString();
            CommandTimeoutTextBox.Text = myDatabaseSettings.CommandTimeout.ToString();
            ConsistentFailureMinimumCountTextBox.Text = myDatabaseSettings.ConsistentFailureMinFailCount.ToString();
            SqlServerNameTextBox.Text = myDatabaseSettings.SqlServerName;

            ConnectToDatabaseCheckbox.IsChecked = myDatabaseSettings.ConnectToDatabase;
        }

        private void ConnectToDatabase_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateDatabaseSettingsWithInputValues();
        }

        private void ConnectToDatabase_OnUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateDatabaseSettingsWithInputValues();
        }

        private bool UpdateDatabaseSettingsWithInputValues()
        {
            if (!int.TryParse(ConnectionTimeoutTextBox.Text.Trim(), out var connectionTimeout) || connectionTimeout < 5 || connectionTimeout > 60)
            {
                MessageBoxHelper.ShowErrorMessage("Invalid input.", "Connection Timeout must be a valid integer between 5 to 60.");
                DatabaseOptionsPanel.IsEnabled = true;
                return false;
            }
            if (!int.TryParse(CommandTimeoutTextBox.Text.Trim(), out var commandTimeout) || commandTimeout < 5 || commandTimeout > 60)
            {
                MessageBoxHelper.ShowErrorMessage("Invalid input.", "Command Timeout must be a valid integer between 5 to 60.");
                DatabaseOptionsPanel.IsEnabled = true;
                return false;
            }

            if (!int.TryParse(ConsistentFailureMinimumCountTextBox.Text.Trim(), out var consistentBuildMinimumCount) || consistentBuildMinimumCount < 3 || consistentBuildMinimumCount > 20)
            {
                MessageBoxHelper.ShowErrorMessage("Invalid input.", "Consistent Failure Minimum Count must be a valid integer between 3 to 20.");
                DatabaseOptionsPanel.IsEnabled = true;
                return false;
            }

            // ReSharper disable once PossibleInvalidOperationException
            var isChecked = (bool)ConnectToDatabaseCheckbox.IsChecked;
            DatabaseOptionsPanel.IsEnabled = isChecked;

            myDatabaseSettings.ConnectToDatabase = isChecked;
            myDatabaseSettings.ConnectionTimeout = connectionTimeout;
            myDatabaseSettings.CommandTimeout = commandTimeout;

            myDatabaseSettings.SqlServerName = SqlServerNameTextBox.Text.Trim();

            myDatabaseSettings.ConsistentFailureMinFailCount = consistentBuildMinimumCount;

            return true;
        }

        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (UpdateDatabaseSettingsWithInputValues())
            {
                myOnSettingsClosed(myDatabaseSettings);
            }
            else
            {
                e.Cancel = true;
            }
        }


        #region InputNumbersValidation
        private void ConnectionTimeoutTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            NumberTextValidation(e);
        }

        private void CommandTimeoutTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            NumberTextValidation(e);
        }

        private void ConsistentBuildMinimumCountTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            NumberTextValidation(e);
        }

        private void ConnectionTimeoutTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ConnectionTimeoutTextBox.Text, out var commandTimeout) || commandTimeout < 5 || commandTimeout > 60)
            {
                ConnectionTimeoutTextBox.Text = myDatabaseSettings.ConnectionTimeout.ToString();
            }
        }

        private void CommandTimeoutTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(CommandTimeoutTextBox.Text, out var commandTimeout) || commandTimeout < 5 || commandTimeout > 60)
            {
                CommandTimeoutTextBox.Text = myDatabaseSettings.CommandTimeout.ToString();
            }
        }

        private void ConsistentBuildMinimumCountTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ConsistentFailureMinimumCountTextBox.Text, out var consistentBuildMinimumCount) || consistentBuildMinimumCount < 3 || consistentBuildMinimumCount > 20)
            {
                ConsistentFailureMinimumCountTextBox.Text = myDatabaseSettings.ConsistentFailureMinFailCount.ToString();
            }
        }

        private static void NumberTextValidation(TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        #endregion

        private void ClearBuildsCacheButton_OnClick(object sender, RoutedEventArgs e)
        {
            TfsBuildAnalyzerConfiguration.ProjectStructure.ProjectCatalogNodeList.Clear();
            TfsBuildAnalyzerConfiguration.ProjectStructure.Save();
            MessageBoxHelper.ShowInfoMessage("Cache Cleared!", "Project structure cache cleared successfully.");
            ProjectStructureCleared?.Invoke(this, null);
        }
    }

    public class DatabaseSettings
    {
        public bool ConnectToDatabase { get; set; }
        public int ConnectionTimeout { get; set; }
        public int CommandTimeout { get; set; }
        public int ConsistentFailureMinFailCount { get; set; }
        public string SqlServerName { get; set; }
    }
}

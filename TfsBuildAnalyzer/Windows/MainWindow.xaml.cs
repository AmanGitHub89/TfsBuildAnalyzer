using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Repositories;
using TfsBuildAnalyzer.Utilities;

using Process = System.Diagnostics.Process;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));
        }

        private bool myIsUiDisabled;

        private Dispatcher myDispatcherObject;

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;
        }

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                BuildsTabControl.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                var message = $"{ex.Message}{Environment.NewLine}{ex.InnerException?.Message}";
                EventLog.WriteEntry("TfsBuildAnalyzer", message, EventLogEntryType.Error);
                MessageBoxHelper.ShowErrorMessage("An error occurred!", message);
                LogHelper.LogException(ex);
                throw;
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            myDispatcherObject = Dispatcher.CurrentDispatcher;

            ExistingBuildsRepository.BuildInfoListUpdated += ExistingBuildsRepository_OnBuildInfoListUpdated;
            ExistingBuildsRepository.BuildTypesListUpdated += ExistingBuildsRepository_OnBuildTypesListUpdated;

            BuildTypeCombobox.ItemsSource = new List<string>();
            RecentBuildDataGrid.ItemsSource = new List<BuildInfo>();

            OnWindowLoaded();

            LatestVersionChecker.CheckLatestVersion(isLatestVersion =>
            {
                if (!isLatestVersion)
                {
                    myDispatcherObject.Invoke(() =>
                    {
                        NewVersionAvailableInfoPanel.Visibility = Visibility.Visible;
                    });
                }
            });
        }

        private void OnWindowLoaded()
        {
            if (TfsBuildAnalyzerConfiguration.ConnectToDatabase && !ExistingBuildsRepository.GotBuilds)
            {
                DisableControls();
                NotConnectedToDatabasePanel.Visibility = Visibility.Hidden;
                ExistingBuildsRepository.GetRecentBuilds();
            }
            else
            {
                NotConnectedToDatabasePanel.Visibility = TfsBuildAnalyzerConfiguration.ConnectToDatabase ? Visibility.Hidden : Visibility.Visible;
            }
        }

        public void Reload(string buildType)
        {
            OnWindowLoaded();
            OnBuildTypesListUpdated(buildType);
        }

        private void ExistingBuildsRepository_OnBuildTypesListUpdated(object sender, string buildType)
        {
            OnBuildTypesListUpdated(buildType);
        }

        private void OnBuildTypesListUpdated(string buildType)
        {
            var buildTypes = ExistingBuildsRepository.GetBuildTypesList();

            var lastSelectedBuildType = TfsBuildAnalyzerConfiguration.LastSelectedBuildType;

            BuildTypeCombobox.ItemsSource = buildTypes;
            if (buildTypes.Count > 0)
            {
                BuildTypeCombobox.SelectedItem = buildType ?? buildTypes[0];
            }

            if (string.IsNullOrEmpty(buildType) && !string.IsNullOrEmpty(lastSelectedBuildType) && buildTypes.Contains(lastSelectedBuildType))
            {
                BuildTypeCombobox.SelectedItem = lastSelectedBuildType;
            }
        }

        private void ExistingBuildsRepository_OnBuildInfoListUpdated(object sender, EventArgs e)
        {
            if (myIsUiDisabled)
            {
                EnableControls();
                if (!ExistingBuildsRepository.GotBuilds)
                {
                    NotConnectedToDatabasePanel.Visibility = Visibility.Visible;
                }
            }
            UpdateBuildsListForSelectedType();
        }

        private void RecentBuildDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentBuildDataGrid.SelectedItem is BuildInfo buildInfo)
            {
                BuildDropFolderValidityChecker.LoadExistingBuild(buildInfo.BuildType, buildInfo.BuildNumber);
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            ExistingBuildsRepository.BuildInfoListUpdated -= ExistingBuildsRepository_OnBuildInfoListUpdated;
            ExistingBuildsRepository.BuildTypesListUpdated -= ExistingBuildsRepository_OnBuildTypesListUpdated;
        }

        private void BuildTypeCombobox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBuildsListForSelectedType();
        }

        private void UpdateBuildsListForSelectedType()
        {
            if (!(BuildTypeCombobox.SelectedItem is string selectedItem))
            {
                ShowConsistentFailures.Visibility = Visibility.Collapsed;
                RecentBuildDataGrid.ItemsSource = new List<BuildInfo>();
                return;
            }

            TfsBuildAnalyzerConfiguration.LastSelectedBuildType = selectedItem;
            ShowConsistentFailures.Visibility = Visibility.Visible;
            RecentBuildDataGrid.ItemsSource = ExistingBuildsRepository.GetBuildInfoList().Where(x => x.BuildType.Contains(selectedItem)).ToList();
        }

        private void RecentBuildDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CompareBuildsButton.Visibility = RecentBuildDataGrid.SelectedItems.Count == 2 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void CompareBuildsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (RecentBuildDataGrid.SelectedItems.Count != 2)
            {
                MessageBoxHelper.ShowInfoMessage("Invalid Selection.", "Select 2 builds to compare.");
                return;
            }
            var currentBuild = RecentBuildDataGrid.SelectedItems[0] as BuildInfo;
            var previousBuild = RecentBuildDataGrid.SelectedItems[1] as BuildInfo;

            RecentBuildDataGrid.UnselectAll();

            if (currentBuild == null || previousBuild == null)
            {
                MessageBoxHelper.ShowErrorMessage("Error while comparing.", "Could not read build data. Please try again.");
                return;
            }

            if (currentBuild.BuildDate < previousBuild.BuildDate)
            {
                var temp = currentBuild;
                currentBuild = previousBuild;
                previousBuild = temp;
            }

            try
            {
                DisableControls();
                var previousBuildResults = await BuildDataRepository.GetData(previousBuild.BuildType, previousBuild.BuildNumber);
                var currentBuildResults = await BuildDataRepository.GetData(currentBuild.BuildType, currentBuild.BuildNumber);

                if (previousBuildResults == null || currentBuildResults == null)
                {
                    MessageBoxHelper.ShowErrorMessage("Error while fetching results.", "Could not fetch results for selected builds from Database.");
                    return;
                }
                EnableControls();

                var compareBuildsWindow = new CompareBuildsWindow(previousBuildResults, currentBuildResults);
                compareBuildsWindow.Show();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                MessageBoxHelper.ShowErrorMessage("Database error!", "Could not get results from DataBase!!!");
            }
            finally
            {
                EnableControls();
            }
        }

        private void AnalyzeNewBuildButton_OnClick(object sender, RoutedEventArgs e)
        {
            RecentBuildDataGrid.UnselectAll();
            new AnalyzeNewBuildWindow().ShowDialog();
        }

        private void EnableControls()
        {
            myIsUiDisabled = false;
            MainGrid.IsEnabled = true;
            LoadingLabel.Visibility = Visibility.Hidden;
        }

        private void DisableControls()
        {
            myIsUiDisabled = true;
            MainGrid.IsEnabled = false;
            LoadingLabel.Visibility = Visibility.Visible;
        }

        private void OnSettingsClosed(DatabaseSettings databaseSettings)
        {
            TfsBuildAnalyzerConfiguration.ConnectionTimeout = databaseSettings.ConnectionTimeout;
            TfsBuildAnalyzerConfiguration.CommandTimeout = databaseSettings.CommandTimeout;

            TfsBuildAnalyzerConfiguration.SqlServer = databaseSettings.SqlServerName;

            TfsBuildAnalyzerConfiguration.ConsistentFailureMinimumFailCount = databaseSettings.ConsistentFailureMinFailCount;

            var isConnectToDbChanged = TfsBuildAnalyzerConfiguration.ConnectToDatabase != databaseSettings.ConnectToDatabase;
            TfsBuildAnalyzerConfiguration.ConnectToDatabase = databaseSettings.ConnectToDatabase;

            if (isConnectToDbChanged)
            {
                if (TfsBuildAnalyzerConfiguration.ConnectToDatabase)
                {
                    UpdateDbConnectionParameters.Update();
                    BuildTypeCombobox.ItemsSource = new List<string>();
                    RecentBuildDataGrid.ItemsSource = new List<BuildInfo>();
                    ExistingBuildsRepository.ClearData();
                }
                OnWindowLoaded();
            }
        }

        private async void ShowConsistentFailures_OnClick(object sender, RoutedEventArgs e)
        {
            if (!TfsBuildAnalyzerConfiguration.ConnectToDatabase)
            {
                MessageBoxHelper.ShowErrorMessage("Not connected to Database.", "Application is not connected to Database. Change in Settings page on main window.");
                return;
            }

            var buildType = (string) BuildTypeCombobox.SelectedItem;
            var currentBuild = ExistingBuildsRepository.GetBuildInfoList()
                .Where(x => x.BuildType.Equals(buildType)).OrderByDescending(x => x.BuildDate)
                .FirstOrDefault();

            if (currentBuild == null)
            {
                MessageBoxHelper.ShowErrorMessage("No builds found.", "No builds found for the selected build type.");
                return;
            }

            var results = await TfsBuildAnalyzerDbConnect.GetConsistentFailures(buildType, TfsBuildAnalyzerConfiguration.ConsistentFailureMinimumFailCount);
            var consistentFailuresWindow = new ConsistentFailuresWindow(results.OrderByDescending(x => x.FailCount).ToList(), currentBuild.BuildNumber);
            consistentFailuresWindow.Show();
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(OnSettingsClosed);
            settingsWindow.ShowDialog();
        }

        private void InstallDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Process.Start($"{directory}\\TfsBuildAnalyzerDatabaseInstaller.exe");
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage("Could not open 'TfsBuildAnalyzerDatabaseInstaller.exe'", ex.Message);
            }
        }

        private void RetryConnectToDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            RetryConnectToDatabaseButton.IsEnabled = false;
            OnWindowLoaded();
            RetryConnectToDatabaseButton.IsEnabled = true;
        }

        private void OnBuildClickHandler(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            var url = textBlock.Tag.ToString();
            try
            {
                Process.Start(url);
            }
            catch
            {
                MessageBoxHelper.ShowErrorMessage("Could not open URL", $"Error opening url: '{url}'");
            }
        }

        private void OnDeleteBuildClick(object sender, RoutedEventArgs e)
        {
            var deleteButton = (Button) sender;
            var confirmDelete = ((WrapPanel)deleteButton.Parent).Children.OfType<WrapPanel>().First(x => x.Name.Equals("ConfirmDeleteButton"));
            deleteButton.Visibility = Visibility.Collapsed;
            confirmDelete.Visibility = Visibility.Visible;
        }

        private async void OnDeleteSureBuildClick(object sender, RoutedEventArgs e)
        {
            if (!(RecentBuildDataGrid.SelectedItem is BuildInfo buildInfo)) return;
            var deleted = await TfsBuildAnalyzerDbConnect.DeleteResult(buildInfo);
            if (deleted)
            {
                ExistingBuildsRepository.DeleteBuild(buildInfo);
            }
            else
            {
                MessageBoxHelper.ShowErrorMessage("Could not delete build.", "Could not delete build. Open log for more details.");
                var confirmDelete = (Button)sender;
                var deleteButton = ((StackPanel)((StackPanel)confirmDelete.Parent).Parent).Children.OfType<Button>().First(x => x.Name.Equals("DeleteBuildButton"));
                deleteButton.Visibility = Visibility.Visible;
                confirmDelete.Visibility = Visibility.Collapsed;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for ShowAllResults.xaml
    /// </summary>
    public partial class ShowAllResults
    {
        private BuildInfo myBuildInfo;

        private string myResultSummaryLabelContent = string.Empty;

        internal void SetResults(BuildInfo buildInfo)
        {
            myBuildInfo = buildInfo;

            var passedResults = myBuildInfo.TestResults.Where(x => x.TestStatus == TestStatus.Passed).ToList();
            var failedResults = myBuildInfo.TestResults.Where(x => x.TestStatus == TestStatus.Failed).ToList();
            var ignoredResults = myBuildInfo.TestResults.Where(x => x.TestStatus == TestStatus.Ignored).ToList();
            myResultSummaryLabelContent = $"Build : {myBuildInfo.BuildType} {myBuildInfo.BuildNumber} - Passed : {passedResults.Count}, Failed : {failedResults.Count}, Ignored : {ignoredResults.Count}";

            if (IsInitialized)
            {
                UpdateResultGrid();
            }
        }

        public ShowAllResults()
        {
            InitializeComponent();
            AddBacklogItemWindow.BacklogItemAdded += AddBacklogItemWindow_OnBacklogItemAdded;
            AllResultsGrid.ResultGridSelectionChanged += ResultGridSelectionChanged;
        }

        private void ResultGridSelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectedCount(AllResultsGrid.GetSelectedCount());
        }

        private void ShowAllResults_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void PassedCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void FailedCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void IgnoredCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void PassedCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void FailedCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void IgnoredCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateResultGrid();
        }

        private void UpdateResultGrid()
        {
            if (!IsInitialized)
            {
                return;
            }

            var isPassedChecked = PassedCheckBox.IsChecked == true;
            var isFailedChecked = FailedCheckBox.IsChecked == true;
            var isIgnoredChecked = IgnoredCheckBox.IsChecked == true;

            var passedResults = isPassedChecked ? myBuildInfo.TestResults.Where(x => x.TestStatus == TestStatus.Passed).ToList() : new List<TestResult>();
            var failedResults = isFailedChecked ? myBuildInfo.TestResults.Where(x => x.TestStatus == TestStatus.Failed).ToList() : new List<TestResult>();
            var ignoredResults = isIgnoredChecked ? myBuildInfo.TestResults.Where(x => x.TestStatus == TestStatus.Ignored).ToList() : new List<TestResult>();

            ResultSummaryLabel.Content = myResultSummaryLabelContent;

            var results = passedResults.Union(failedResults).Union(ignoredResults).ToList();

            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                var searchText = SearchTextBox.Text.Trim();
                var parts = searchText.Split('*').Select(Regex.Escape).ToArray();
                var regex = string.Join(".*?", parts);

                if (FilterTypeCombobox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var searchCriteria = selectedItem.Content;
                    switch (searchCriteria)
                    {
                        case "Name":
                            results = results.Where(x => Regex.IsMatch(x.TestName, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        case "Error":
                            results = results.Where(x => Regex.IsMatch(x.ExceptionMessage + x.ExceptionTrace, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        case "Agent":
                            results = results.Where(x => Regex.IsMatch(x.TestAgent, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        case "File":
                            results = results.Where(x => Regex.IsMatch(x.FilePath, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        default:
                            throw new Exception("Invalid Search Criteria.");
                    }
                }
            }

            
            AllResultsGrid.SetResults(results.Select(GridResult.ToGridResult).ToList());

            DisplayedResultsCountLabel.Content = $"Showing {results.Count} Results";
            UpdateSelectedCount(0);
        }

        private void UpdateSelectedCount(int selectedCount)
        {
            SelectedResultsCountLabel.Content = $"{selectedCount} rows selected.";
        }

        private void ImportCsvFilterFile_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxHelper.ShowInfoMessage("File format.", "Select a file with test names separated by '#'.");
            var openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            var fileName = openFileDialog.FileName;
            if (!File.Exists(fileName))
            {
                MessageBoxHelper.ShowInfoMessage("File not found.", "Selected file could not be found.");
                return;
            }

            var testReportGenerator = new TestReportGenerator(myBuildInfo);
            testReportGenerator.GenerateReport(fileName);
        }

        private void ExportToExcel_OnClick(object sender, RoutedEventArgs e)
        {
            var exportWindow = new ExportWindow();
            exportWindow.Export(AllResultsGrid.GetDisplayedResults());
            exportWindow.ShowDialog();
        }

        private void FilterTypeCombobox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = string.Empty;
            }
        }

        private void AddBacklogItemWindow_OnBacklogItemAdded(object sender, BacklogItemAddedEventArgs e)
        {
            var test = myBuildInfo.TestResults.FirstOrDefault(x => x.TestId.Equals(e.Item.TestId));
            if (test != null)
            {
                test.HasItems = true;
                UpdateResultGrid();
            }
        }

        private void ShowAllResults_OnClosed(object sender, EventArgs e)
        {
            AddBacklogItemWindow.BacklogItemAdded -= AddBacklogItemWindow_OnBacklogItemAdded;
        }
    }
}

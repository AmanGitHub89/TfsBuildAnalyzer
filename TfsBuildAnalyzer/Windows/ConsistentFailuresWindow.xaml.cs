using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for ConsistentFailuresWindow.xaml
    /// </summary>
    public partial class ConsistentFailuresWindow : Window
    {
        internal List<ConsistentGridResult> TestResults { get; }
        internal List<ConsistentGridResult> DisplayedTestResults { get; private set; }

        public ConsistentFailuresWindow(List<TestResult> testResults, string buildNumber)
        {
            TestResults = testResults.Select(x => ConsistentGridResult.ToGridResult(x, buildNumber)).ToList();
            DisplayedTestResults = TestResults.ToList();
            InitializeComponent();
            AddBacklogItemWindow.BacklogItemAdded += AddBacklogItemWindow_OnBacklogItemAdded;
        }

        private void ConsistentFailuresWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataSource();
        }

        private void UpdateDataSource()
        {
            if (ResultDataGrid == null) return;
            ResultDataGrid.ItemsSource = DisplayedTestResults;
            ResultDataGrid.DataContext = DisplayedTestResults;
            DisplayedResultsCountLabel.Content = $"Showing {DisplayedTestResults.Count} Results";
        }

        private void FilterTypeCombobox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = string.Empty;
            }
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGridResult();
        }

        private void UpdateGridResult()
        {
            if (!IsInitialized)
            {
                return;
            }

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
                            DisplayedTestResults = TestResults.Where(x => Regex.IsMatch(x.TestName, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        case "Error":
                            DisplayedTestResults = TestResults.Where(x => Regex.IsMatch(x.Error, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        case "File":
                            DisplayedTestResults = TestResults.Where(x => Regex.IsMatch(x.FilePath, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        default:
                            throw new Exception("Invalid Search Criteria.");
                    }
                }
            }
            else
            {
                DisplayedTestResults = TestResults.ToList();
            }

            UpdateDataSource();
        }

        private void ExportToExcel_OnClick(object sender, RoutedEventArgs e)
        {
            var exportWindow = new ExportWindow();
            exportWindow.Export(ResultDataGrid.Items.OfType<ConsistentGridResult>().ToList());
            exportWindow.ShowDialog();
        }

        private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var builder = new StringBuilder();
            foreach (var selectedItem in ResultDataGrid.SelectedItems)
            {
                var gridResult = (ConsistentGridResult)selectedItem;
                builder.Append(gridResult.TestName + Environment.NewLine);
            }
            Clipboard.SetDataObject(builder.ToString());
        }

        private void ResultDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(ResultDataGrid.SelectedItem is ConsistentGridResult gridResult))
            {
                return;
            }

            TextDetailWindow textDetailWindow = null;
            switch (ResultDataGrid.CurrentCell.Column.Header.ToString())
            {
                case "Name":
                    textDetailWindow = new TextDetailWindow(gridResult.TestName);
                    break;
                case "Error":
                    textDetailWindow = new TextDetailWindow(gridResult.Error);
                    break;
                case "FileName":
                    textDetailWindow = new TextDetailWindow(gridResult.FilePath);
                    break;
            }

            textDetailWindow?.SetTitle(ResultDataGrid.CurrentCell.Column.Header.ToString());
            textDetailWindow?.ShowDialog();
        }

        private void AddBacklogItemWindow_OnBacklogItemAdded(object sender, BacklogItemAddedEventArgs e)
        {
            var test = TestResults.FirstOrDefault(x => x.TestId.Equals(e.Item.TestId));
            if (test != null)
            {
                test.HasItems = true;
            }
            UpdateGridResult();
        }

        private void ConsistentFailuresWindow_OnClosed(object sender, EventArgs e)
        {
            AddBacklogItemWindow.BacklogItemAdded -= AddBacklogItemWindow_OnBacklogItemAdded;
        }
    }

    public class ConsistentGridResult : IGridResult
    {
        public Guid TestId { get; set; }

        public string BuildType { get; set; }


        public string PreviousBuildNumber { get; set; }

        public bool HasItems { get; set; }

        public string TestAgent { get; set; }

        [ExportToExcel]
        public string TestName { get; set; }

        [ExportToExcel]
        public string BuildNumber { get; set; }

        [ExportToExcel]
        public int FailCount { get; set; }

        [ExportToExcel]
        public string Error { get; set; }

        [ExportToExcel]
        public string FilePath { get; set; }

        [ExcelCellColor]
        public string ExcelCellColor => ResultColors.Failed;

        internal static ConsistentGridResult ToGridResult(TestResult testResult, string buildNumber)
        {
            var gridResult = new ConsistentGridResult
            {
                TestId = testResult.TestId,
                TestName = testResult.TestName,
                BuildType = testResult.BuildType,
                BuildNumber = buildNumber,
                PreviousBuildNumber = testResult.BuildNumber,
                TestAgent = testResult.TestAgent,
                Error = testResult.ExceptionMessage + Environment.NewLine + Environment.NewLine + testResult.ExceptionTrace,
                FilePath = testResult.FilePath,
                HasItems = testResult.HasItems,
                FailCount = testResult.FailCount
            };
            return gridResult;
        }
    }
}

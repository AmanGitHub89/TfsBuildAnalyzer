using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for CompareBuildsWindow.xaml
    /// </summary>
    public partial class CompareBuildsWindow
    {
        private readonly BuildInfo myPreviousBuildResults;
        private readonly BuildInfo myCurrentBuildResults;

        internal List<CompareResultGridPassed> myPassedDelta;
        internal List<CompareResultGridFailed> myFailedDelta;

        internal List<CompareResultGridPassed> DisplayedPassedDelta { get; private set; }
        internal List<CompareResultGridFailed> DisplayedFailedDelta { get; private set; }

        public CompareBuildsWindow(BuildInfo previousBuildResults, BuildInfo currentBuildResults)
        {
            InitializeComponent();
            myPreviousBuildResults = previousBuildResults;
            myCurrentBuildResults = currentBuildResults;
        }

        private void CompareBuildsWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CompareBuilds();
            AddBacklogItemWindow.BacklogItemAdded += AddBacklogItemWindow_OnBacklogItemAdded;
        }

        private void CompareBuilds()
        {
            myPassedDelta = GetPassedDelta();
            myFailedDelta = GetFailedDelta();

            DisplayedPassedDelta = myPassedDelta.ToList();
            DisplayedFailedDelta = myFailedDelta.ToList();

            PassedDataGrid.ItemsSource = DisplayedPassedDelta;
            FailedDataGrid.ItemsSource = DisplayedFailedDelta;

            PassedDataGrid.DataContext = DisplayedPassedDelta;
            FailedDataGrid.DataContext = DisplayedFailedDelta;

            FailedIgnoredTabItem.Header = $"Failed/Ignored : {myFailedDelta.Count}";
            PassedTabItem.Header = $"Passed : {myPassedDelta.Count}";

            if (BuildCompareInfoLabel != null)
            {
                BuildCompareInfoLabel.Content = $"{myPreviousBuildResults.BuildNumber} Vs. {myCurrentBuildResults.BuildNumber}";
            }

            UpdateResultGrid();
        }

        private List<CompareResultGridPassed> GetPassedDelta()
        {
            var passedDelta = new List<CompareResultGridPassed>();

            var currentPassed = myCurrentBuildResults.TestResults.Where(x => x.TestStatus == TestStatus.Passed).ToList();

            //Tests that did not pass in last run
            var previousDeltaForCurrentPassed = myPreviousBuildResults.TestResults.
                Where(x => currentPassed.Any(y => y.TestName.Equals(x.TestName) && y.TestStatus != x.TestStatus)).ToList();

            //Get current tests list, for previous calculated delta. Not all passed (i.e. currentPassed) have to be shown.
            var currentTestsForPreviousDelta = myCurrentBuildResults.TestResults.
                Where(x => previousDeltaForCurrentPassed.Any(y => y.TestName.Equals(x.TestName)))
                //Remove duplicates
                .GroupBy(z => z.TestName).Select(x => x.First()).ToList();

            var currentPassedForPreviousUnavailable = currentPassed.Where(x => !myPreviousBuildResults.TestResults.Any(y => y.TestName.Equals(x.TestName))).ToList();

            var currentDeltaToShow = currentTestsForPreviousDelta.Union(currentPassedForPreviousUnavailable).ToList();

            foreach (var testResult in currentDeltaToShow)
            {
                var previousTestResults = previousDeltaForCurrentPassed.Where(x => x.TestName.Equals(testResult.TestName)).ToList();
                if (previousTestResults.Count > 0)
                {
                    foreach (var previousTestResult in previousTestResults)
                    {
                        passedDelta.Add(CompareResultGridPassed.ToGridResult(previousTestResult, testResult));
                    }
                }
                else
                {
                    passedDelta.Add(CompareResultGridPassed.ToGridResult(null, testResult));
                }
            }

            return passedDelta;
        }

        private List<CompareResultGridFailed> GetFailedDelta()
        {
            var failedDelta = new List<CompareResultGridFailed>();

            var currentNotPassed = myCurrentBuildResults.TestResults.Where(x => x.TestStatus != TestStatus.Passed).ToList();
            var previousResultsForCurrentNotPassed = myPreviousBuildResults.TestResults.Where(x => currentNotPassed.Any(y => y.TestName.Equals(x.TestName))).ToList();

            foreach (var testResult in currentNotPassed)
            {
                var previousTestResults = previousResultsForCurrentNotPassed.Where(x => x.TestName.Equals(testResult.TestName)).ToList();

                if (previousTestResults.Count > 0)
                {
                    //One test might have run multiple times in different agents in previous build
                    foreach (var previousTestResult in previousTestResults)
                    {
                        if (IsPreviousFailedSame(previousTestResult, testResult))
                        {
                            continue;
                        }

                        failedDelta.Add(CompareResultGridFailed.ToGridResult(previousTestResult, testResult));
                    }
                }
                else
                {
                    failedDelta.Add(CompareResultGridFailed.ToGridResult(null, testResult));
                }

            }
            return failedDelta;
        }

        private static bool IsPreviousFailedSame(TestResult previousTestResult, TestResult currentTestResult)
        {
            return previousTestResult != null && currentTestResult.TestStatus.Equals(previousTestResult.TestStatus)
                                              && currentTestResult.ExceptionMessage.Equals(previousTestResult.ExceptionMessage)
                                              && currentTestResult.ExceptionTrace.Equals(previousTestResult.ExceptionTrace);
        }

        private void PassedDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!(PassedDataGrid.SelectedItem is CompareResultGridPassed gridResult))
            {
                return;
            }

            TextDetailWindow textDetailWindow = null;
            switch (PassedDataGrid.CurrentCell.Column.Header.ToString())
            {
                case "Previous Error":
                    textDetailWindow = new TextDetailWindow(gridResult.PreviousError);
                    break;
                case "Previous FilePath":
                    textDetailWindow = new TextDetailWindow(gridResult.PreviousFilePath);
                    break;
                case "Current FilePath":
                    textDetailWindow = new TextDetailWindow(gridResult.CurrentFilePath);
                    break;
                case "Test Name":
                    textDetailWindow = new TextDetailWindow(gridResult.TestName);
                    break;
            }

            textDetailWindow?.SetTitle(PassedDataGrid.CurrentCell.Column.Header.ToString());
            textDetailWindow?.ShowDialog();
        }

        private void FailedDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!(FailedDataGrid.SelectedItem is CompareResultGridFailed gridResult))
            {
                return;
            }

            TextDetailWindow textDetailWindow = null;
            switch (FailedDataGrid.CurrentCell.Column.Header.ToString())
            {
                case "Previous Error":
                    textDetailWindow = new TextDetailWindow(gridResult.PreviousError);
                    break;
                case "Current Error":
                    textDetailWindow = new TextDetailWindow(gridResult.CurrentError);
                    break;
                case "Previous FilePath":
                    textDetailWindow = new TextDetailWindow(gridResult.PreviousFilePath);
                    break;
                case "Current FilePath":
                    textDetailWindow = new TextDetailWindow(gridResult.CurrentFilePath);
                    break;
                case "Test Name":
                    textDetailWindow = new TextDetailWindow(gridResult.TestName);
                    break;
            }

            textDetailWindow?.SetTitle(FailedDataGrid.CurrentCell.Column.Header.ToString());
            textDetailWindow?.ShowDialog();
        }

        private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResultGrid();
            e.Handled = true;
        }

        private void UpdateResultGrid()
        {
            if (!IsInitialized || DisplayedFailedDelta == null || DisplayedPassedDelta == null)
            {
                return;
            }

            var searchText = SearchTextBox.Text.Trim();

            var isFailedPage = TabControl1.SelectedIndex == 0;

            if (string.IsNullOrEmpty(searchText))
            {
                DisplayedFailedDelta = myFailedDelta.ToList();
                DisplayedPassedDelta = myPassedDelta.ToList();
            }
            else
            {
                var selectedComboBox = isFailedPage ? FailedFilterTypeCombobox : PassedFilterTypeCombobox;
                if (selectedComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var searchCriteria = selectedItem.Content;
                    var parts = searchText.Split('*').Select(Regex.Escape).ToArray();
                    var regex = string.Join(".*?", parts);
                    switch (searchCriteria)
                    {
                        case "Test Name":
                            if (isFailedPage)
                            {
                                DisplayedFailedDelta = myFailedDelta.Where(x => Regex.IsMatch(x.TestName, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            else
                            {
                                DisplayedPassedDelta = myPassedDelta.Where(x => Regex.IsMatch(x.TestName, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            break;
                        case "Change":
                            DisplayedFailedDelta = myFailedDelta.Where(x => !string.IsNullOrEmpty(x.Change) && x.Change.CaseInsensitiveContains(searchText)).ToList();
                            break;
                        case "Previous Error":
                            if (isFailedPage)
                            {
                                DisplayedFailedDelta = myFailedDelta.Where(x => !string.IsNullOrEmpty(x.PreviousError) && Regex.IsMatch(x.PreviousError, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            else
                            {
                                DisplayedPassedDelta = myPassedDelta.Where(x => !string.IsNullOrEmpty(x.PreviousError) && Regex.IsMatch(x.PreviousError, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            break;
                        case "Current Error":
                            DisplayedFailedDelta = myFailedDelta.Where(x => !string.IsNullOrEmpty(x.CurrentError) && Regex.IsMatch(x.CurrentError, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            break;
                        case "Previous File":
                            if (isFailedPage)
                            {
                                DisplayedFailedDelta = myFailedDelta.Where(x => Regex.IsMatch(x.PreviousFilePath, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            else
                            {
                                DisplayedPassedDelta = myPassedDelta.Where(x => Regex.IsMatch(x.PreviousFilePath, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            break;
                        case "Current File":
                            if (isFailedPage)
                            {
                                DisplayedFailedDelta = myFailedDelta.Where(x => Regex.IsMatch(x.CurrentFilePath, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            else
                            {
                                DisplayedPassedDelta = myPassedDelta.Where(x => Regex.IsMatch(x.CurrentFilePath, regex, RegexOptions.IgnoreCase | RegexOptions.Singleline)).ToList();
                            }
                            break;
                        default:
                            throw new Exception("Invalid Search Criteria.");
                    }
                }
            }

            FailedDataGrid.ItemsSource = DisplayedFailedDelta.ToList();
            PassedDataGrid.ItemsSource = DisplayedPassedDelta.ToList();

            FailedDataGrid.DataContext = DisplayedFailedDelta.ToList();
            PassedDataGrid.DataContext = DisplayedPassedDelta.ToList();

            DisplayedResultsCountLabel.Content = isFailedPage ? $"Showing {((List<CompareResultGridFailed>) FailedDataGrid.DataContext).Count} Results" 
                                                     : $"Showing {((List<CompareResultGridPassed>) PassedDataGrid.DataContext).Count} Results";
            ShowSelectedCount();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ShowSelectedCount();

            var isFailedPage = TabControl1.SelectedIndex == 0;
            if (isFailedPage)
            {
                FailedFilterTypeCombobox.Visibility = Visibility.Visible;
                PassedFilterTypeCombobox.Visibility = Visibility.Collapsed;
            }
            else
            {
                FailedFilterTypeCombobox.Visibility = Visibility.Collapsed;
                PassedFilterTypeCombobox.Visibility = Visibility.Visible;
            }
        }

        private void FailedDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowSelectedCount();
            e.Handled = true;
        }

        private void PassedDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowSelectedCount();
            e.Handled = true;
        }

        private void ShowSelectedCount()
        {
            if (SelectedRowsCountLabel == null)
            {
                return;
            }
            var isFailedPage = TabControl1.SelectedIndex == 0;
            if (isFailedPage)
            {
                SelectedRowsCountLabel.Content = $"{FailedDataGrid.SelectedItems.Count} rows selected.";
            }
            else
            {
                SelectedRowsCountLabel.Content = $"{PassedDataGrid.SelectedItems.Count} rows selected.";
            }
        }

        private void ExportToExcel_OnClick(object sender, RoutedEventArgs e)
        {
            var exportWindow = new ExportWindow();
            var isFailedPage = TabControl1.SelectedIndex == 0;
            if (isFailedPage)
            {
                exportWindow.Export(FailedDataGrid.Items.OfType<CompareResultGridFailed>().ToList());
            }
            else
            {
                exportWindow.Export(PassedDataGrid.Items.OfType<CompareResultGridPassed>().ToList());
            }
            exportWindow.ShowDialog();
            e.Handled = true;
        }

        private void FailedFilterTypeCombobox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = string.Empty;
            }
        }

        private void PassedFilterTypeCombobox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = string.Empty;
            }
        }

        private void AddBacklogItemWindow_OnBacklogItemAdded(object sender, BacklogItemAddedEventArgs e)
        {
            var passedTest = DisplayedPassedDelta.FirstOrDefault(x => x.TestId.Equals(e.Item.TestId));
            if (passedTest != null)
            {
                passedTest.HasItems = true;
                UpdateResultGrid();
            }
            var failedTest = DisplayedFailedDelta.FirstOrDefault(x => x.TestId.Equals(e.Item.TestId));
            if (failedTest != null)
            {
                failedTest.HasItems = true;
                UpdateResultGrid();
            }

            var test = myCurrentBuildResults.TestResults.FirstOrDefault(x => x.TestId.Equals(e.Item.TestId));
            if (test != null)
            {
                test.HasItems = true;
            }
        }

        private void CompareBuildsWindow_OnClosed(object sender, EventArgs e)
        {
            AddBacklogItemWindow.BacklogItemAdded -= AddBacklogItemWindow_OnBacklogItemAdded;
        }
    }

    internal class CompareResultGridPassed : IGridResult
    {
        public Guid TestId { get; set; }
        public string BuildType { get; set; }
        public string BuildNumber { get; set; }

        [ExportToExcel]
        public string TestName { get; set; }

        [ExportToExcel]
        public TestStatus? PreviousStatus { get; set; }

        [ExportToExcel]
        public string PreviousError { get; set; }

        [ExportToExcel]
        public string PreviousAgent { get; set; }

        [ExportToExcel]
        public string CurrentAgent { get; set; }

        [ExportToExcel]
        public string PreviousFilePath { get; set; }

        [ExportToExcel]
        public string CurrentFilePath { get; set; }

        public bool HasItems { get; set; }

        [ExcelCellColor]
        public string ExcelCellColor => ResultColors.Passed;

        internal static CompareResultGridPassed ToGridResult(TestResult previousResult, TestResult currentResult)
        {
            var compareResultGridPassed = new CompareResultGridPassed
            {
                TestId = currentResult.TestId,
                TestName = currentResult.TestName,
                PreviousStatus = previousResult?.TestStatus,
                PreviousError = previousResult == null ? null : previousResult.ExceptionMessage + Environment.NewLine + Environment.NewLine + previousResult.ExceptionTrace,
                PreviousAgent = previousResult?.TestAgent,
                CurrentAgent = currentResult.TestAgent,
                PreviousFilePath = previousResult?.FilePath,
                CurrentFilePath = currentResult.FilePath,
                HasItems = currentResult.HasItems,
                BuildType = currentResult.BuildType,
                BuildNumber = currentResult.BuildNumber
            };
            return compareResultGridPassed;
        }
    }

    internal class CompareResultGridFailed : IGridResult
    {
        public Guid TestId { get; set; }
        public string BuildType { get; set; }
        public string BuildNumber { get; set; }

        [ExportToExcel]
        public string TestName { get; set; }

        [ExportToExcel]
        public string Change { get; set; }

        [ExportToExcel]
        public TestStatus? PreviousStatus { get; set; }

        [ExportToExcel]
        public TestStatus CurrentStatus { get; set; }

        [ExportToExcel]
        public string PreviousError { get; set; }

        [ExportToExcel]
        public string CurrentError { get; set; }

        [ExportToExcel]
        public string PreviousAgent { get; set; }

        [ExportToExcel]
        public string CurrentAgent { get; set; }

        [ExportToExcel]
        public string PreviousFilePath { get; set; }

        [ExportToExcel]
        public string CurrentFilePath { get; set; }

        public bool HasItems { get; set; }

        [ExcelCellColor]
        public string ExcelCellColor => PreviousStatus == TestStatus.Passed && CurrentStatus != TestStatus.Passed ? ResultColors.NewFailure : ResultColors.Failed;

        internal static CompareResultGridFailed ToGridResult(TestResult previousResult, TestResult currentResult)
        {
            var compareResultGridFailed = new CompareResultGridFailed
            {
                TestId = currentResult.TestId,
                TestName = currentResult.TestName,
                PreviousStatus = previousResult?.TestStatus,
                CurrentStatus = currentResult.TestStatus,
                PreviousError = previousResult == null ? null : previousResult.ExceptionMessage + Environment.NewLine + Environment.NewLine + previousResult.ExceptionTrace,
                CurrentError = currentResult.ExceptionMessage + Environment.NewLine + Environment.NewLine + currentResult.ExceptionTrace,
                PreviousAgent = previousResult?.TestAgent,
                CurrentAgent = currentResult.TestAgent,
                PreviousFilePath = previousResult?.FilePath,
                CurrentFilePath = currentResult.FilePath,
                HasItems = currentResult.HasItems,
                BuildType = currentResult.BuildType,
                BuildNumber = currentResult.BuildNumber
            };
            if (previousResult != null)
            {
                var isSameStatus = previousResult.TestStatus == currentResult.TestStatus;
                if (isSameStatus)
                {
                    var isMessageDifferent = !string.IsNullOrEmpty(previousResult.ExceptionMessage) && !string.IsNullOrEmpty(currentResult.ExceptionMessage) 
                                                                                                    && !previousResult.ExceptionMessage.Equals(currentResult.ExceptionMessage);
                    var isStackTraceDifferent = !string.IsNullOrEmpty(previousResult.ExceptionTrace) && !string.IsNullOrEmpty(currentResult.ExceptionTrace)
                                                                                                       && !previousResult.ExceptionMessage.Equals(currentResult.ExceptionTrace);
                    if (isMessageDifferent && isStackTraceDifferent)
                    {
                        compareResultGridFailed.Change = "Exception message and stack trace are different.";
                    }
                    else if (isMessageDifferent)
                    {
                        compareResultGridFailed.Change = "Exception message is different.";
                    }
                    else
                    {
                        compareResultGridFailed.Change = "Stack trace is different.";
                    }
                }
                else
                {
                    compareResultGridFailed.Change = $"{previousResult.TestStatus.ToString()} to {currentResult.TestStatus.ToString()}";
                }
            }
            return compareResultGridFailed;
        }
    }
}

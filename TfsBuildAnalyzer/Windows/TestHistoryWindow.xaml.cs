using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for TestHistoryWindow.xaml
    /// </summary>
    public partial class TestHistoryWindow
    {
        private readonly IGridResult myGridResult;
        internal List<TestHistory> TestHistoryList { get; private set; }

        public TestHistoryWindow(IGridResult gridResult)
        {
            InitializeComponent();

            myGridResult = gridResult;
            TestNameLabel.Text = myGridResult.TestName;

            TestHistoryList= new List<TestHistory>();
        }

        private async void TestHistoryWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TfsBuildAnalyzerConfiguration.ConnectToDatabase)
                {
                    MessageBoxHelper.ShowWarningMessage("Not connected to Database.",
                        "Application is not connected to Database. Cannot show history. Change in Settings page on main window.");
                    Close();
                    return;
                }
                TestHistoryList = await TfsBuildAnalyzerDbConnect.GetTestHistory(myGridResult.TestId, myGridResult.BuildType, myGridResult.BuildNumber);

                UpdateGrid();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                MessageBoxHelper.ShowErrorMessage("Database error!", "Could not get results from DataBase!!!");
            }
        }

        private void UpdateGrid()
        {
            TestHistoryDataGrid.ItemsSource = TestHistoryList;
            TestHistoryDataGrid.DataContext = TestHistoryList;
        }

        private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var builder = new StringBuilder();
            foreach (var selectedItem in TestHistoryDataGrid.SelectedItems)
            {
                var gridResult = (TestHistory)selectedItem;
                builder.Append($"{myGridResult.TestName}{Environment.NewLine}");
            }
            Clipboard.SetDataObject(builder.ToString());
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(myGridResult.TestName);
        }

        private void TestHistoryDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(TestHistoryDataGrid.SelectedItem is TestHistory testHistory))
            {
                return;
            }

            TextDetailWindow textDetailWindow = null;
            switch (TestHistoryDataGrid.CurrentCell.Column.Header.ToString())
            {
                case "Build Type":
                    textDetailWindow = new TextDetailWindow(testHistory.BuildType);
                    break;
                case "Build Number":
                    textDetailWindow = new TextDetailWindow(testHistory.BuildNumber);
                    break;
                case "Agent":
                    textDetailWindow = new TextDetailWindow(testHistory.TestAgent);
                    break;
                case "Error":
                    textDetailWindow = new TextDetailWindow(testHistory.Error);
                    break;
                case "FileName":
                    textDetailWindow = new TextDetailWindow(testHistory.FilePath);
                    break;
            }

            textDetailWindow?.SetTitle(TestHistoryDataGrid.CurrentCell.Column.Header.ToString());
            textDetailWindow?.ShowDialog();
        }
    }
}

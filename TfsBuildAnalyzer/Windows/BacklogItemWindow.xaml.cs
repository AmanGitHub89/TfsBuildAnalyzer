using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for BacklogItemWindow.xaml
    /// </summary>
    public partial class BacklogItemWindow
    {
        private readonly IGridResult myGridResult;

        internal List<TestBacklogItem> BacklogItems { get; private set; }

        public BacklogItemWindow(IGridResult gridResult)
        {
            InitializeComponent();

            myGridResult = gridResult;
            TestNameLabel.Text = myGridResult.TestName;

            BacklogItems = new List<TestBacklogItem>();

            AddBacklogItemWindow.BacklogItemAdded += AddBacklogItemWindow_OnBacklogItemAdded;
        }

        private async void BacklogItemWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!TfsBuildAnalyzerConfiguration.ConnectToDatabase)
            {
                MessageBoxHelper.ShowWarningMessage("Not connected to Database.",
                    "Application is not connected to Database. Cannot show linked backlog items. Change in Settings page on main window.");
                Close();
                return;
            }
            try
            {
                BacklogItems = await TfsBuildAnalyzerDbConnect.GetBacklogItemsForTestList(new List<TestIdType> {new TestIdType {TestId = myGridResult.TestId}});

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
            //ResultDataGrid.ItemsSource = null;
            ResultDataGrid.ItemsSource = BacklogItems;
            ResultDataGrid.DataContext = BacklogItems;
        }

        private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var builder = new StringBuilder();
            foreach (var selectedItem in ResultDataGrid.SelectedItems)
            {
                var gridResult = (TestBacklogItem)selectedItem;
                builder.Append($"{gridResult.BacklogId} {gridResult.BacklogTitle}{Environment.NewLine}");
            }
            Clipboard.SetDataObject(builder.ToString());
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(myGridResult.TestName);
        }

        private void AddBacklogButton_OnClick(object sender, RoutedEventArgs e)
        {
            var addBacklogItemWindow = new AddBacklogItemWindow(myGridResult);
            addBacklogItemWindow.ShowDialog();
        }

        private void AddBacklogItemWindow_OnBacklogItemAdded(object sender, BacklogItemAddedEventArgs e)
        {
            BacklogItems.Add(e.Item);
            BacklogItems = BacklogItems.ToList();
            UpdateGrid();
        }

        private void BacklogItemWindow_OnClosed(object sender, EventArgs e)
        {
            AddBacklogItemWindow.BacklogItemAdded -= AddBacklogItemWindow_OnBacklogItemAdded;
        }
    }
}

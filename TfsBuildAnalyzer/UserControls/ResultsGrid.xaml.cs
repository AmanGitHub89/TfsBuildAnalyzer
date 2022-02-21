using TfsBuildAnalyzer.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsGrid.xaml
    /// </summary>
    public partial class ResultsGrid : UserControl
    {
        internal List<GridResult> TestResults { get; private set; } = new List<GridResult>();
        public EventHandler ResultGridSelectionChanged;

        internal void SetResults(List<GridResult> testResults)
        {
            TestResults = testResults;
            if (IsInitialized)
            {
                UpdateDataSource();
            }

            ResultDataGrid.DataContext = TestResults;
        }

        internal List<GridResult> GetDisplayedResults()
        {
            return ResultDataGrid.Items.OfType<GridResult>().ToList();
        }

        internal int GetSelectedCount()
        {
            return ResultDataGrid.SelectedItems.Count;
        }

        public ResultsGrid()
        {
            InitializeComponent();
        }

        private void ResultsGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDataSource();
        }

        private void UpdateDataSource()
        {
            if (ResultDataGrid != null)
            {
                ResultDataGrid.ItemsSource = TestResults;
            }
        }

        private void ResultDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(ResultDataGrid.SelectedItem is GridResult gridResult))
                {
                    return;
                }

                TextDetailWindow textDetailWindow = null;
                var currentCellColumn = ResultDataGrid.CurrentCell.Column;
                if (currentCellColumn == null) return;
                switch (currentCellColumn.Header.ToString())
                {
                    case "Error":
                        textDetailWindow = new TextDetailWindow(gridResult.Error);
                        break;
                    case "FileName":
                        textDetailWindow = new TextDetailWindow(gridResult.FilePath);
                        break;
                    case "Name":
                        textDetailWindow = new TextDetailWindow(gridResult.TestName);
                        break;
                }

                e.Handled = true;

                textDetailWindow?.SetTitle(ResultDataGrid.CurrentCell.Column.Header.ToString());
                textDetailWindow?.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage(ex.Message, ex.StackTrace);
            }
        }

        private void ResultDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResultGridSelectionChanged?.Invoke(null, null);
            e.Handled = true;
        }

        private void CommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var builder = new StringBuilder();
            foreach (var selectedItem in ResultDataGrid.SelectedItems)
            {
                var gridResult = (GridResult)selectedItem;
                builder.Append(gridResult.TestName + Environment.NewLine);
            }
            Clipboard.SetDataObject(builder.ToString());
        }

        private void OnFilePathClickHandler(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBlock = (TextBlock)sender;
                var url = textBlock.Tag.ToString();
                e.Handled = true;
                Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBoxHelper.ShowErrorMessage("Error", ex.StackTrace);
            }
        }
    }
}

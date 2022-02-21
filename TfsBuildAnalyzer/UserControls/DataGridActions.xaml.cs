using System.Windows;
using System.Windows.Controls;

using TfsBuildAnalyzer.Windows;


namespace TfsBuildAnalyzer.UserControls
{
    /// <summary>
    /// Interaction logic for DataGridActions.xaml
    /// </summary>
    public partial class DataGridActions : UserControl
    {
        public DataGridActions()
        {
            InitializeComponent();
        }

        private void HistoryButton_OnClick(object sender, RoutedEventArgs e)
        {
            var gridResult = GetResult(sender);
            var testHistoryWindow = new TestHistoryWindow(gridResult);
            testHistoryWindow.ShowDialog();
        }

        private void TfsBacklogButton_OnClick(object sender, RoutedEventArgs e)
        {
            var gridResult = GetResult(sender);
            var backlogItemWindow = new BacklogItemWindow(gridResult);
            backlogItemWindow.ShowDialog();
        }

        private static IGridResult GetResult(object sender)
        {
            return ((FrameworkElement)sender).DataContext as IGridResult;
        }
    }
}

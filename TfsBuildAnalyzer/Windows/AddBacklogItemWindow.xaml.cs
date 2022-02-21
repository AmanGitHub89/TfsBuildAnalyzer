using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for AddBacklogItemWindow.xaml
    /// </summary>
    public partial class AddBacklogItemWindow
    {
        public static event EventHandler<BacklogItemAddedEventArgs> BacklogItemAdded;

        private IGridResult myGridResult;
        private bool myIsUiDisabled;

        public AddBacklogItemWindow(IGridResult gridResult)
        {
            myGridResult = gridResult;
            InitializeComponent();
        }

        private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var backlogId = BacklogIdTextBox.Text.Trim();
            var backlogTitle = BacklogTitleTextBox.Text.Trim();
            if (string.IsNullOrEmpty(backlogId) || !int.TryParse(backlogId, out var backlogIdInt))
            {
                MessageBoxHelper.ShowErrorMessage("Invalid ID.", "Please enter valid backlog ID.");
                return;
            }
            if (string.IsNullOrEmpty(backlogTitle))
            {
                MessageBoxHelper.ShowErrorMessage("Invalid title.", "Please enter backlog title.");
                return;
            }

            var isSaved = false;
            var isAlreadyExists = false;

            try
            {
                DisableControls();

                var saveResult = await TfsBuildAnalyzerDbConnect.InsertBacklogItem(myGridResult.TestId, backlogIdInt, backlogTitle);
                if (saveResult.Equals("1"))
                {
                    isSaved = true;
                }
                else
                {
                    isAlreadyExists = saveResult.Equals("0");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                EnableControls();
            }

            if (isSaved)
            {
                myGridResult.HasItems = true;
                BacklogItemAdded?.Invoke(this,
                    new BacklogItemAddedEventArgs
                        {Item = new TestBacklogItem {TestId = myGridResult.TestId, BacklogId = backlogIdInt, BacklogTitle = backlogTitle}});
            }
            else
            {
                if (isAlreadyExists)
                {
                    MessageBoxHelper.ShowErrorMessage("Backlog already exists.", "The entered Backlog is already associated with the given test.");
                }
                else
                {
                    MessageBoxHelper.ShowErrorMessage("Database Error.", "Error while associating backlog to test.");
                }
            }
            Close();
        }

        private void DisableControls()
        {
            myIsUiDisabled = true;
            IsEnabled = false;
        }

        private void EnableControls()
        {
            myIsUiDisabled = false;
            IsEnabled = true;
        }

        private void AddBacklogItemWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = myIsUiDisabled;
        }

        private void BacklogIdTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void BacklogIdTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = BacklogIdTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(text) && !int.TryParse(text, out _))
            {
                BacklogIdTextBox.Text = string.Empty;
            }
        }
    }
}

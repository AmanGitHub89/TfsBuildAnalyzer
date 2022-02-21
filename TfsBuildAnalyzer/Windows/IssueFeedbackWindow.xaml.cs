using System;
using System.Windows;

using DbConnect;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for IssueFeedbackWindow.xaml
    /// </summary>
    public partial class IssueFeedbackWindow
    {
        private readonly ViewType myViewType;

        public IssueFeedbackWindow(ViewType viewType)
        {
            myViewType = viewType;
            InitializeComponent();
        }

        private void IssueFeedbackWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            switch (myViewType)
            {
                case ViewType.Issue:
                    IssueLabel.Visibility = Visibility.Visible;
                    Title = "Submit Issue";
                    break;
                case ViewType.Feedback:
                    Title = "Submit Feedback/Suggestions";
                    FeedbackLabel.Visibility = Visibility.Visible;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void SubmitData()
        {
            var textData = IssueFeedbackTextBox.Text.Trim();
            switch (myViewType)
            {
                case ViewType.Feedback:
                    try
                    {
                        var isSaved = await TfsBuildAnalyzerDbConnect.InsertAppFeedback(textData);
                        if (isSaved)
                        {
                            MessageBoxHelper.ShowInfoMessage("Inputs saved.", "Your inputs have been saved successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowErrorMessage("Error while saving.", "An error occurred while saving to database.");
                        LogHelper.LogException(ex);
                    }
                    break;
                case ViewType.Issue:
                    try
                    {
                        var isSaved = await TfsBuildAnalyzerDbConnect.InsertAppIssue(textData);
                        if (isSaved)
                        {
                            MessageBoxHelper.ShowInfoMessage("Inputs saved.", "Your inputs have been saved successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBoxHelper.ShowErrorMessage("Error while saving.", "An error occurred while saving to database.");
                        LogHelper.LogException(ex);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Close();
        }


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (IssueFeedbackTextBox.Text.Trim().Equals(string.Empty))
            {
                MessageBoxHelper.ShowWarningMessage("Invalid input.","No data to save. Please enter your inputs in the text-box.");
                return;
            }
            SubmitData();
        }

        public enum ViewType
        {
            Feedback,
            Issue
        }
    }
}

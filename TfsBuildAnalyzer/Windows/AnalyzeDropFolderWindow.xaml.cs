using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.DropFolderReaderLogic;
using TfsBuildAnalyzer.Repositories;
using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Windows
{
    /// <summary>
    /// Interaction logic for AnalyzeDropFolderWindow.xaml
    /// </summary>
    public partial class AnalyzeDropFolderWindow
    {
        private readonly BuildInfo myBuild;
        private bool myIsAnalyzing;

        public AnalyzeDropFolderWindow(BuildInfo build)
        {
            myBuild = build;
            InitializeComponent();
        }

        private void StartAnalyzing()
        {
            BuildNumberLabel.Content = BuildNumberLabel.Content.ToString().Replace("***", myBuild.BuildNumber);

            myIsAnalyzing = true;
            var tfsBuildDropFolderReader = new DropFolderReader
            {
                ProgressChanged = ProgressChanged,
                ActionChanged = (sender, s) => { ProgressControl1.StepLabel.Content = s; },
                Completed = delegate (object o, IList<TestResult> list)
                {
                    myIsAnalyzing = false;
                    ProgressControl1.SetProgress(100);
                    ProcessResults(list.ToList());
                }
            };

            tfsBuildDropFolderReader.Read(myBuild.BuildDropLocation);
            myBuild.BuildDropLocation = string.Empty;
        }

        private void ProcessResults(List<TestResult> testResults)
        {
            testResults.ForEach(x =>
            {
                x.BuildType = myBuild.BuildType;
                x.BuildNumber = myBuild.BuildNumber;
            });
            foreach (var testResult in testResults)
            {
                var duplicateTestWithId = testResults.FirstOrDefault(x => x.TestName.Equals(testResult.TestName) && !x.Equals(testResult) && !x.TestId.Equals(Guid.Empty));
                testResult.TestId = duplicateTestWithId?.TestId ?? Guid.NewGuid();
            }

            try
            {
                myBuild.PassedCount = testResults.Count(x => x.TestStatus == TestStatus.Passed);
                myBuild.FailedCount = testResults.Count(x => x.TestStatus == TestStatus.Failed);
                myBuild.IgnoredCount = testResults.Count(x => x.TestStatus == TestStatus.Ignored);
                myBuild.TestResults = testResults;
                if (myBuild.TestResults.Count == 0)
                {
                    MessageBoxHelper.ShowErrorMessage("No data.", "The build contains no test result data.");
                    Close();
                    return;
                }

                BuildDataRepository.AddData(myBuild);
                if (TfsBuildAnalyzerConfiguration.ConnectToDatabase)
                {
                    SaveResultsToDataBase();
                }
                else
                {
                    AddBuildInfoToRepository();
                }
                ShowResults(myBuild);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                throw;
            }
        }

        private async void SaveResultsToDataBase()
        {
            ProgressControl1.StepLabel.Content = "Saving Results to Database.";

            SaveResultsResponse saveResultsResponse = null;
            try
            {
                saveResultsResponse = await TfsBuildAnalyzerDbConnect.SaveResults(myBuild);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            if (saveResultsResponse != null)
            {
                if (saveResultsResponse.IsSaved)
                {
                    //Update TestIds to tests
                    myBuild.TestResults.ForEach(testResult =>
                    {
                        var oldVsNewTestId = saveResultsResponse.OldVsNewTestIdList.FirstOrDefault(testId => testResult.TestId.Equals(testId.TestId));
                        if (oldVsNewTestId == null)
                        {
                            LogHelper.LogMessage("SaveResultsToDataBase - Old vs new count for test ids did not match.");
                            MessageBoxHelper.ShowErrorMessage("Database error!", "Could not save results to DataBase!!!");
                            return;
                        }

                        testResult.TestId = oldVsNewTestId.NewTestId;
                        testResult.HasItems = oldVsNewTestId.HasItems;
                    });

                    AddBuildInfoToRepository();
                }
                else
                {
                    MessageBoxHelper.ShowErrorMessage("Database error!", "Could not save results to DataBase!!!");
                }
            }
            else
            {
                MessageBoxHelper.ShowErrorMessage("Database error!", "Could not save results to DataBase!!!");
            }
        }

        private void AddBuildInfoToRepository()
        {
            ExistingBuildsRepository.AddBuildType(myBuild.BuildType);

            ExistingBuildsRepository.AddBuildInfo(myBuild);
        }

        private void ShowResults(BuildInfo buildInfo)
        {
            var showAllResults = new ShowAllResults();
            showAllResults.SetResults(buildInfo);
            showAllResults.Show();
            Close();
        }

        private void ProgressChanged(object sender, int progress)
        {
            ProgressControl1.SetProgress(progress);
        }

        private void AnalyzeDropFolderWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            StartAnalyzing();
        }

        private void AnalyzeDropFolderWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = myIsAnalyzing;
        }
    }
}

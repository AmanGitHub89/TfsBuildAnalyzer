using System;
using System.Globalization;
using System.IO;
using System.Linq;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.DropFolderReaderLogic;
using TfsBuildAnalyzer.Repositories;
using TfsBuildAnalyzer.Utilities;
using TfsBuildAnalyzer.Windows;


namespace TfsBuildAnalyzer
{
    internal static class BuildDropFolderValidityChecker
    {
        internal static CanAnalyzeState CanAnalyze(BuildInfo buildInfo)
        {
            var connectToDatabase = TfsBuildAnalyzerConfiguration.ConnectToDatabase;

            if (connectToDatabase && string.IsNullOrEmpty(buildInfo.BuildType))
            {
                return CanAnalyzeState.InvalidBuildType;
            }

            if (!IsValidDropLocationDirectory(buildInfo.BuildDropLocation))
            {
                return CanAnalyzeState.InvalidDropFolder;
            }

            if (string.IsNullOrEmpty(buildInfo.BuildNumber))
            {
                buildInfo.BuildNumber = new DirectoryInfo(buildInfo.BuildDropLocation).Name;
            }

            if (connectToDatabase && ExistingBuildsRepository.GetBuildInfoList().Any(x => x.BuildType.Equals(buildInfo.BuildType) && x.BuildNumber.Equals(buildInfo.BuildNumber)))
            {
                return CanAnalyzeState.BuildExists;
            }

            if (buildInfo.BuildDate == DateTime.MinValue)
            {
                buildInfo.BuildDate = new DirectoryInfo(buildInfo.BuildDropLocation).LastWriteTime;
            }

            return CanAnalyzeState.CanAnalyze;
        }

        public static void ShowMessageForAnalyzeState(CanAnalyzeState canAnalyzeState, BuildInfo buildInfo)
        {
            switch (canAnalyzeState)
            {
                case CanAnalyzeState.CanAnalyze:
                    break;
                case CanAnalyzeState.InvalidBuildType:
                    MessageBoxHelper.ShowWarningMessage("Invalid build type.", "Please select a build type from drop down.");
                    break;
                case CanAnalyzeState.InvalidDropFolder:
                    MessageBoxHelper.ShowWarningMessage("Invalid directory.", $"Drop folder directory is invalid.{Environment.NewLine}{buildInfo.BuildDropLocation ?? string.Empty}");
                    break;
            }
        }

        private static bool IsValidDropLocationDirectory(string buildDropLocation)
        {
            return !string.IsNullOrEmpty(buildDropLocation) && Directory.Exists(buildDropLocation) && DropFolderReader.IsValidParentDropFolder(buildDropLocation);
        }

        public static async void LoadExistingBuild(string buildType, string buildNumber)
        {
            try
            {
                var analyzedBuildData = await BuildDataRepository.GetData(buildType, buildNumber);
                if (analyzedBuildData != null)
                {
                    var showAllResults = new ShowAllResults();
                    showAllResults.SetResults(analyzedBuildData);
                    showAllResults.Show();
                }
                else
                {
                    MessageBoxHelper.ShowErrorMessage("Error while opening.", "Could not read build data. Please try again.");
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                MessageBoxHelper.ShowErrorMessage("Database error!", "Could not get results from DataBase!!!");
            }
        }
    }
    internal enum CanAnalyzeState
    {
        CanAnalyze,
        InvalidBuildType,
        InvalidDropFolder,
        BuildExists
    }
}

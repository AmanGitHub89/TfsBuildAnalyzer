using System;
using System.Collections.Generic;
using System.Linq;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Repositories
{
    internal static class ExistingBuildsRepository
    {
        private static List<BuildInfo> myBuildInfoList = new List<BuildInfo>();
        private static List<string> myBuildTypesList = new List<string>();

        public static event EventHandler BuildInfoListUpdated;

        public static event EventHandler<string> BuildTypesListUpdated;

        public static bool GotBuilds { get; private set; }
        private static bool myGettingBuilds;

        static ExistingBuildsRepository()
        {
            if (!TfsBuildAnalyzerConfiguration.HasCheckedSqlServerName)
            {
                var machineName = Environment.MachineName;
                if (!TfsBuildAnalyzerConfiguration.SqlServer.CaseInsensitiveContains(machineName))
                {
                    TfsBuildAnalyzerConfiguration.SqlServer = $"{Environment.MachineName}\\MSSQLSERVER_SYDS";
                }
                TfsBuildAnalyzerConfiguration.HasCheckedSqlServerName = true;
            }
            if (TfsBuildAnalyzerConfiguration.ConnectToDatabase)
            {
                GetRecentBuilds();
            }
        }

        public static async void GetRecentBuilds()
        {
            if (GotBuilds || myGettingBuilds)
            {
                return;
            }

            myGettingBuilds = true;
            try
            {
                myBuildTypesList = await TfsBuildAnalyzerDbConnect.GetBuildTypes();
                GotBuilds = true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                MessageBoxHelper.ShowErrorMessage("Database error!", "Could not connect to DataBase!!!");
            }
            finally
            {
                BuildTypesListUpdated?.Invoke(null, null);
            }

            try
            {
                myBuildInfoList = await TfsBuildAnalyzerDbConnect.GetRecentBuilds();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                BuildInfoListUpdated?.Invoke(null, null);
            }

            myGettingBuilds = false;
        }

        public static void AddBuildInfo(BuildInfo buildInfo)
        {
            if (!myBuildInfoList.Any(x => x.BuildType.Equals(buildInfo.BuildType) && x.BuildNumber.Equals(buildInfo.BuildNumber)))
            {
                myBuildInfoList.Add(buildInfo);
                BuildInfoListUpdated?.Invoke(null, null);
            }
        }

        public static void AddBuildType(string buildType)
        {
            if (!myBuildTypesList.Contains(buildType))
            {
                myBuildTypesList.Add(buildType);
                BuildTypesListUpdated?.Invoke(null, buildType);
            }
        }

        public static List<BuildInfo> GetBuildInfoList()
        {
            return myBuildInfoList.OrderByDescending(x => x.BuildDate).ToList();
        }

        public static List<string> GetBuildTypesList()
        {
            return myBuildTypesList.ToList();
        }

        public static void DeleteBuild(BuildInfo buildInfo)
        {
            var index = myBuildInfoList.FindIndex(x => x.BuildType.Equals(buildInfo.BuildType) && x.BuildNumber.Equals(buildInfo.BuildNumber));
            if (index <= -1) return;

            myBuildInfoList.RemoveAt(index);
            BuildInfoListUpdated?.Invoke(null, null);

            if (!myBuildInfoList.Any(x => x.BuildType.Equals(buildInfo.BuildType)))
            {
                myBuildTypesList.Remove(buildInfo.BuildType);
                BuildTypesListUpdated?.Invoke(null, myBuildTypesList.FirstOrDefault());
            }
        }

        public static void ClearData()
        {
            myBuildInfoList.Clear();
            myBuildTypesList.Clear();
            BuildTypesListUpdated?.Invoke(null, null);
            BuildInfoListUpdated?.Invoke(null, null);
            GotBuilds = false;
        }
    }
}

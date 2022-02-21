using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Repositories
{
    internal static class BuildDataRepository
    {
        private static readonly List<BuildInfo> myAnalyzedBuildData = new List<BuildInfo>();

        public static void AddData(BuildInfo buildInfo)
        {
            if (!myAnalyzedBuildData.Any(x => x.BuildType.Equals(buildInfo.BuildType) && x.BuildNumber.Equals(buildInfo.BuildNumber)))
            {
                myAnalyzedBuildData.Add(buildInfo);
            }
        }

        public static async Task<BuildInfo> GetData(string buildType, string buildNumber)
        {
            var analyzedBuildData  = myAnalyzedBuildData.FirstOrDefault(x => x.BuildType.Equals(buildType) && x.BuildNumber.Equals(buildNumber));

            if (analyzedBuildData == null && TfsBuildAnalyzerConfiguration.ConnectToDatabase)
            {
                analyzedBuildData = await TfsBuildAnalyzerDbConnect.GetResultsForBuild(buildType, buildNumber);
            }

            return analyzedBuildData;
        }
    }
}

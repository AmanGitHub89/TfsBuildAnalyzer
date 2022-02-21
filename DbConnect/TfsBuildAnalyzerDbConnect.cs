using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using TfsBuildAnalyzerModels;


namespace DbConnect
{
    public static class TfsBuildAnalyzerDbConnect
    {
        public static async Task<SaveResultsResponse> SaveResults(BuildInfo buildInfo)
        {
            var testResults = buildInfo.TestResults.Select(TestResultDbStruct.FromTestResult).ToList();
            using (var dataSet = await new ExecuteSp()
                                     .AddVarCharParameter(SqlParameterNames.BuildType, buildInfo.BuildType, 100)
                                     .AddVarCharParameter(SqlParameterNames.BuildNumber, buildInfo.BuildNumber, 100)
                                     .AddVarCharParameter(SqlParameterNames.BuildStatus, buildInfo.BuildStatus, 30)
                                     .AddVarCharParameter(SqlParameterNames.BuildUrl, buildInfo.BuildUrl, 1000)
                                     .AddParameter(SqlParameterNames.BuildDate, SqlDbType.DateTime, buildInfo.BuildDate)
                                     .AddParameter(SqlParameterNames.PassedCount, SqlDbType.Int, buildInfo.PassedCount)
                                     .AddParameter(SqlParameterNames.FailedCount, SqlDbType.Int, buildInfo.FailedCount)
                                     .AddParameter(SqlParameterNames.IgnoredCount, SqlDbType.Int, buildInfo.IgnoredCount)
                                     .AddParameter(SqlParameterNames.TestResults, SqlDbType.Structured, CommonFunctions.ConvertToDataTable(testResults))
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.InsertResults))
            {
                var saveResultsResponse = new SaveResultsResponse {IsSaved = CommonFunctions.DoesFirstRowColEqualOne(dataSet)};

                if (saveResultsResponse.IsSaved && dataSet.Tables.Count >= 2)
                {
                    saveResultsResponse.OldVsNewTestIdList = dataSet.Tables[1].DataTableToList<OldVsNewTestId>();
                }

                return saveResultsResponse;
            }
        }

        public static async Task<bool> DeleteResult(BuildInfo buildInfo)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddVarCharParameter(SqlParameterNames.BuildType, buildInfo.BuildType, 100)
                                     .AddVarCharParameter(SqlParameterNames.BuildNumber, buildInfo.BuildNumber, 100)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.DeleteResult))
            {
                return CommonFunctions.DoesFirstRowColEqualOne(dataSet);
            }
        }

        public static async Task<List<BuildInfo>> GetRecentBuilds()
        {
            using (var dataSet = await new ExecuteSp()
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.GetRecentBuilds))
            {
                return CommonFunctions.DataSetHasTable(dataSet) ? dataSet.Tables[0].DataTableToList<BuildInfo>() : new List<BuildInfo>();
            }
        }

        public static async Task<List<string>> GetBuildTypes()
        {
            using (var dataSet = await new ExecuteSp()
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.GetBuildTypes))
            {
                var buildTypes = new List<string>();
                if (CommonFunctions.DataSetHasTable(dataSet))
                {
                    foreach (DataRow row in dataSet.Tables[0].Rows)
                    {
                        buildTypes.Add(row[0].ToString());
                    }
                }

                return buildTypes;
            }
        }

        public static async Task<BuildInfo> GetResultsForBuild(string buildType, string buildNumber)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddVarCharParameter(SqlParameterNames.BuildType, buildType, 100)
                                     .AddVarCharParameter(SqlParameterNames.BuildNumber, buildNumber, 100)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.GetResults))
            {
                var isValidDataSet = dataSet != null && dataSet.Tables.Count == 2 && dataSet.Tables[0].Rows.Count == 1;
                if (!isValidDataSet)
                {
                    return null;
                }
                var buildInfo = dataSet.Tables[0].Rows[0].ToObject<BuildInfo>();
                buildInfo.TestResults = dataSet.Tables[1].DataTableToList<TestResult>();
                return buildInfo;
            }
        }

        public static async Task<List<TestBacklogItem>> GetBacklogItemsForTestList(List<TestIdType> testIdList)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddParameter(SqlParameterNames.TestIds, SqlDbType.Structured, CommonFunctions.ConvertToDataTable(testIdList))
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.GetBacklogItemsForTestList))
            {
                if (!CommonFunctions.DataSetHasTable(dataSet))
                {
                    return null;
                }
                return dataSet.Tables[0].DataTableToList<TestBacklogItem>();
            }
        }

        public static async Task<string> InsertNewBuildType(string buildType)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddVarCharParameter(SqlParameterNames.BuildType, buildType, 100)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.InsertNewBuildType))
            {
                return CommonFunctions.DataSetHasRowsInFirstTable(dataSet) ? CommonFunctions.FirstRowColString(dataSet) : "-1";
            }
        }

        public static async Task<string> InsertBacklogItem(Guid testId, int backlogId, string backlogTitle)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddParameter(SqlParameterNames.TestId, SqlDbType.UniqueIdentifier, testId)
                                     .AddParameter(SqlParameterNames.BacklogId, SqlDbType.Int, backlogId)
                                     .AddParameter(SqlParameterNames.BacklogTitle, SqlDbType.NVarChar, backlogTitle)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.InsertBacklogItem))
            {
                return CommonFunctions.DataSetHasRowsInFirstTable(dataSet) ? CommonFunctions.FirstRowColString(dataSet) : "-1";
            }
        }

        public static async Task<List<TestHistory>> GetTestHistory(Guid testId, string buildType, string buildNumber)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddParameter(SqlParameterNames.TestId, SqlDbType.UniqueIdentifier, testId)
                                     .AddVarCharParameter(SqlParameterNames.BuildType, buildType, 100)
                                     .AddVarCharParameter(SqlParameterNames.BuildNumber, buildNumber, 100)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.GetTestHistory))
            {
                if (!CommonFunctions.DataSetHasTable(dataSet))
                {
                    return null;
                }
                return dataSet.Tables[0].DataTableToList<TestHistory>();
            }
        }

        public static async Task<List<TestResult>> GetConsistentFailures(string buildType, int failureCount)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddVarCharParameter(SqlParameterNames.BuildType, buildType, 100)
                                     .AddParameter(SqlParameterNames.FailureCount, SqlDbType.SmallInt, failureCount)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.ConsistentFailures))
            {
                if (!CommonFunctions.DataSetHasTable(dataSet))
                {
                    return null;
                }
                return dataSet.Tables[0].DataTableToList<TestResult>();
            }
        }

        public static async Task<bool> InsertAppIssue(string issueDescription)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddParameter(SqlParameterNames.IssueDescription, SqlDbType.NVarChar, issueDescription)
                                     .AddVarCharParameter(SqlParameterNames.UserInfo, GetUserInfo(), 200)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.InsertAppIssue))
            {
                return CommonFunctions.DoesFirstRowColEqualOne(dataSet);
            }
        }

        public static async Task<bool> InsertAppFeedback(string feedbackDescription)
        {
            using (var dataSet = await new ExecuteSp()
                                     .AddParameter(SqlParameterNames.FeedbackDescription, SqlDbType.NVarChar, feedbackDescription)
                                     .AddVarCharParameter(SqlParameterNames.UserInfo, GetUserInfo(), 200)
                                     .ExecuteSpReturnDataSet(StoredProcedureNames.InsertAppFeedback))
            {
                return CommonFunctions.DoesFirstRowColEqualOne(dataSet);
            }
        }

        private static string GetUserInfo()
        {
            try
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

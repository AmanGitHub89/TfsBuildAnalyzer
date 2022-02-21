using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DbConnect;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;
using TfsBuildAnalyzer.Windows;


namespace TfsBuildAnalyzer
{
    internal class TestReportGenerator
    {
        private readonly BuildInfo myBuildInfo;

        public TestReportGenerator(BuildInfo buildInfo)
        {
            myBuildInfo = buildInfo;
        }

        public async void GenerateReport(string csvFileName)
        {
            try
            {
                var testNamePairList = File.ReadAllText(csvFileName).Split(new[] {'#'}, StringSplitOptions.RemoveEmptyEntries).ToList()
                    .Select(x => new TestNamePair {TestName = x, TestSearchName = x.Split('.')[1]});

                var reportDataList = new List<ReportData>();
                foreach (var testNamePair in testNamePairList)
                {
                    var testResult = myBuildInfo.TestResults.FirstOrDefault(x => x.TestName.Contains(testNamePair.TestSearchName) && x.TestStatus == TestStatus.Passed) ??
                                     myBuildInfo.TestResults.FirstOrDefault(x => x.TestName.Contains(testNamePair.TestSearchName) && x.TestStatus == TestStatus.Ignored) ??
                                      myBuildInfo.TestResults.FirstOrDefault(x => x.TestName.Contains(testNamePair.TestSearchName));

                    var status = string.Empty;
                    var testId = Guid.NewGuid();

                    var excelCellColor = string.Empty;
                    if (testResult != null)
                    {
                        status = testResult.TestStatus.ToString();
                        testId = testResult.TestId;
                        excelCellColor = testResult.TestStatus == TestStatus.Passed ? ResultColors.Passed :
                                         testResult.TestStatus == TestStatus.Ignored ? ResultColors.Ignored : ResultColors.Failed;
                    }

                    reportDataList.Add(new ReportData { TestId = testId, TestName = testNamePair.TestName, Status = status, BacklogItems = new List<int>(), ExcelCellColor = excelCellColor });
                }

                var testIdTypeList = reportDataList.Select(x => new TestIdType { TestId = x.TestId }).ToList();

                List<TestBacklogItem> backlogItemsForTestList = null;
                try
                {
                    backlogItemsForTestList = await TfsBuildAnalyzerDbConnect.GetBacklogItemsForTestList(testIdTypeList);
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.ShowErrorMessage("Database error.", "Could not retrieve data from Database.");
                    LogHelper.LogException(ex);
                }

                if (backlogItemsForTestList == null)
                {
                    MessageBoxHelper.ShowErrorMessage("Database error.", "Could not retrieve data from Database.");
                    return;
                }

                foreach (var backlogItemForTest in backlogItemsForTestList.ToList())
                {
                    foreach (var reportData in reportDataList.Where(x => x.TestId == backlogItemForTest.TestId))
                    {
                        reportData.BacklogItems.Add(backlogItemForTest.BacklogId);
                    }
                }

                var exportWindow = new ExportWindow();
                exportWindow.Export(reportDataList.OrderByDescending(x => x.Status).ThenBy(y => y.TestName).ToList());
                exportWindow.ShowDialog();
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception);
                MessageBoxHelper.ShowErrorMessage("Error occurred.", "Error occurred while gathering data to export.");
            }
        }

        private class ReportData
        {
            public Guid TestId { get; set; }

            [ExportToExcelAttribute]
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string TestName { get; set; }

            [ExportToExcelAttribute]
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Status { get; set; }

            [ExportToExcelAttribute]
            // ReSharper disable once CollectionNeverQueried.Local
            public List<int> BacklogItems { get; set; }

            [ExcelCellColor]
            public string ExcelCellColor { get; set; }
        }

        private class TestNamePair
        {
            public string TestName { get; set; }
            public string TestSearchName { get; set; }
        }
    }
}

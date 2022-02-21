/* -------------------------------------------------------------------------------------------------
   Restricted - Copyright (C) Siemens Healthcare GmbH/Siemens Medical Solutions USA, Inc., 2020. All rights reserved
   ------------------------------------------------------------------------------------------------- */

using System;

using TfsBuildAnalyzerModels;


namespace TfsBuildAnalyzer
{
    public class GridResult : IGridResult
    {
        public Guid TestId { get; set; }

        public string BuildType { get; set; }

        public string BuildNumber { get; set; }

        public bool HasItems { get; set; }

        [ExportToExcel]
        public string TestName { get; set; }

        [ExportToExcel]
        public string Agent { get; set; }

        [ExportToExcel]
        public string Error { get; set; }

        [ExportToExcel]
        public TestStatus Status { get; set; }

        [ExportToExcel]
        public string Duration { get; set; }

        [ExportToExcel]
        public string FilePath { get; set; }

        public int FailCount { get; set; }

        [ExcelCellColor]
        public string ExcelCellColor => Status == TestStatus.Passed ? ResultColors.Passed : Status == TestStatus.Ignored ? ResultColors.Ignored : ResultColors.Failed;

        internal static GridResult ToGridResult(TestResult testResult)
        {
            var gridResult = new GridResult
            {
                TestId = testResult.TestId,
                TestName = testResult.TestName,
                BuildType = testResult.BuildType,
                BuildNumber = testResult.BuildNumber,
                Agent = testResult.TestAgent,
                Error = testResult.ExceptionMessage + Environment.NewLine + Environment.NewLine + testResult.ExceptionTrace,
                Status = testResult.TestStatus,
                FilePath = testResult.FilePath,
                HasItems = testResult.HasItems,
                FailCount = testResult.FailCount,
                Duration = testResult.Duration
            };
            return gridResult;
        }
    }
}

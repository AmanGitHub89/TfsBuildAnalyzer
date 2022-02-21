using System;

using TfsBuildAnalyzerModels;


namespace DbConnect
{
    internal class TestResultDbStruct
    {
        public Guid TestId { get; set; }
        public string TestName { get; set; }
        public string TestAgent { get; set; } = "";
        public string ExceptionMessage { get; set; }
        public string ExceptionTrace { get; set; }
        public string TestStatus { get; set; }
        public string FilePath { get; set; }
        public string Duration { get; set; }

        public static TestResultDbStruct FromTestResult(TestResult testResult)
        {
            return new TestResultDbStruct
            {
                TestId = testResult.TestId,
                TestName = testResult.TestName,
                TestAgent = testResult.TestAgent,
                ExceptionMessage = testResult.ExceptionMessage,
                ExceptionTrace = testResult.ExceptionTrace,
                TestStatus = testResult.TestStatus.ToString(),
                FilePath = testResult.FilePath,
                Duration = testResult.Duration
            };
        }
    }
}

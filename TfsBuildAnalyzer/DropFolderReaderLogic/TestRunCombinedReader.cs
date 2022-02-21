using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.DropFolderReaderLogic
{
    internal static class TestRunCombinedReader
    {
        public static IList<TestResult> Read(string fileName)
        {
            var testDataList = new List<TestResult>();

            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    var document = XDocument.Load(fileStream);
                    var rootElement = document.Root;
                    if (rootElement == null)
                    {
                        throw new Exception("Couldn't read document.");
                    }

                    var hostName = rootElement.Descendants().First(x => x.Name.LocalName.Equals("hostName")).Value;

                    var testElements = rootElement.Descendants().Where(x => IsTestCaseNode(x) || SitTestFixtureSetUpFailureNode(x)).ToList();

                    foreach (var testElement in testElements)
                    {
                        var testData = ReadTestElement(testElement, fileName);
                        testData.TestAgent = hostName;
                        testDataList.Add(testData);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    throw;
                }
            }

            return testDataList;
        }

        private static bool IsTestCaseNode(XElement element)
        {
            const string testCaseNodeName = "testCase";

            return element.Name.LocalName.Equals(testCaseNodeName) && !IsTestFixtureSetup(element);
        }

        private static bool IsTestFixtureSetup(XElement element)
        {
            var testCallNode = element.Elements().First(x => x.Name.LocalName.Equals("testCall"));
            return testCallNode.Elements().Any(x => x.Name.LocalName.Equals("purpose"));
        }

        private static bool SitTestFixtureSetUpFailureNode(XElement element)
        {
            const string testCaseNodeName = "testCase";
            return element.Name.LocalName.Equals(testCaseNodeName) && IsTestFixtureSetup(element) && IsTestFixtureSetupFailed(element);
        }

        private static bool IsTestFixtureSetupFailed(XElement element)
        {
            var resultNode = element.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("result"));
            if (resultNode == null) return false;

            var outcomeNode = resultNode.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("outcome"));
            return outcomeNode != null && !outcomeNode.Value.Equals("Passed");
        }

        private static TestResult ReadTestElement(XElement element, string fileName)
        {
            const string testNameNode = "name";
            try
            {
                var testCaseName = element.Elements().First(x => x.Name.LocalName.Equals(testNameNode)).Value;
                var testData = new TestResult { TestName = testCaseName, FilePath = fileName };

                var resultNode = element.Elements().First(x => x.Name.LocalName.Equals("result"));

                var result = resultNode.Elements().First(x => x.Name.LocalName.Equals("outcome")).Value;

                switch (result)
                {
                    case "Passed":
                        testData.TestStatus = TestStatus.Passed;
                        break;
                    case "Ignored":
                        testData.TestStatus = TestStatus.Ignored;
                        GetErrorMessageAndException(resultNode, testData);
                        break;
                    default:
                        testData.TestStatus = TestStatus.Failed;
                        GetErrorMessageAndException(resultNode, testData);
                        break;
                }

                SetTestDuration(element, testData);

                return testData;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                throw;
            }
        }

        private static void SetTestDuration(XElement element, TestResult testData)
        {
            var startDateTimeElement = element.Elements().FirstOrDefault(x => GetTimeStamp(x) != null);
            var endDateTimeElement = element.Elements().LastOrDefault(x => GetTimeStamp(x) != null);
            if (startDateTimeElement != null && endDateTimeElement != null)
            {
                var startDateTimeAttribute = GetTimeStamp(startDateTimeElement);
                var endDateTimeAttribute = GetTimeStamp(endDateTimeElement);
                if (DateTime.TryParse(startDateTimeAttribute.Value, out var startDateTime) &&
                    DateTime.TryParse(endDateTimeAttribute.Value, out var endDateTime))
                {
                    testData.Duration = (endDateTime - startDateTime).ToString("hh':'mm':'ss'.'fff");
                }
            }
        }

        private static XAttribute GetTimeStamp(XElement element)
        {
            return element.HasAttributes ? element.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("timeStamp")) : null;
        }

        private static void GetErrorMessageAndException(XElement element, TestResult testData)
        {
            testData.ExceptionMessage = element.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("message"))?.Value ?? "";
            testData.ExceptionTrace = element.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("exception"))?.Value ?? "";
        }
    }
}

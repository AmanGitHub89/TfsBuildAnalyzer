using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.DropFolderReaderLogic
{
    internal static class NUnitReader
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

                    var hostName = string.Empty;
                    var hostNameNode = rootElement.Descendants().First(x => x.Name.LocalName.Equals("environment"));
                    if (hostNameNode != null && hostNameNode.HasAttributes)
                    {
                        hostName = hostNameNode.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("machine-name"))?.Value;
                    }

                    //var testFixtureNodes = rootElement.Descendants().Where(IsTestFixtureNode).ToList();

                    //foreach (var testFixtureNode in testFixtureNodes)
                    //{
                    //}

                    var testElements = rootElement.Descendants().Where(IsTestCaseNode).ToList();
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

        private static TestResult ReadTestElement(XElement element, string fileName)
        {
            try
            {
                var testCaseName = element.Attributes().First(x => x.Name.LocalName.Equals("name")).Value;
                var testData = new TestResult { TestName = testCaseName, FilePath = fileName };

                var result = element.Attributes().First(x => x.Name.LocalName.Equals("result")).Value;

                switch (result)
                {
                    case "Success":
                        testData.TestStatus = TestStatus.Passed;
                        break;
                    case "Ignored":
                        testData.TestStatus = TestStatus.Ignored;
                        GetErrorMessageAndException(element, testData);
                        break;
                    default:
                        testData.TestStatus = TestStatus.Failed;
                        GetErrorMessageAndException(element, testData);
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
            var timeAttribute = element.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("time"));
            if (timeAttribute == null) return;

            var timeString = timeAttribute.Value;

            try
            {
                var dotIndex = timeString.IndexOf('.');
                if (dotIndex > -1)
                {
                    var milliSecondsString = timeString.Substring(dotIndex, timeString.Length - 1);
                    milliSecondsString = milliSecondsString.Remove(0, 1);
                    if (milliSecondsString.Length == 1)
                    {
                        timeString += "00";
                    }
                    else if (milliSecondsString.Length == 2)
                    {
                        timeString += "0";
                    }
                }
                else
                {
                    timeString += ".000";
                }

                var culture = new CultureInfo("en-US");
                var formats = new string[] {
                    @"s\.fff",
                    @"ss\.fff",
                    @"m\:ss\.fff",
                    @"mm\:ss\.fff",
                    @"h\:mm\:ss\.fff",
                    @"hh\:mm\:ss\.fff"
                };
                var timeSpan = TimeSpan.ParseExact(timeString, formats, culture.NumberFormat);
                testData.Duration = timeSpan.ToString("hh':'mm':'ss'.'fff");
            }
            catch (Exception)
            {
                LogHelper.LogMessage($"SetTestDuration - Error parsing value {timeString}");
                testData.Duration = timeAttribute.Value;
            }
        }
        
        private static void GetErrorMessageAndException(XElement element, TestResult testData)
        {
            var failureNode = element.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("failure"));
            if (failureNode == null) return;

            testData.ExceptionMessage = failureNode.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("message"))?.Value ?? "";
            testData.ExceptionTrace = failureNode.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("stack-trace"))?.Value ?? "";
        }

        private static bool IsTestFixtureNode(XElement element)
        {
            const string nodeName = "test-suite";
            var isTestSuiteNode = element.Name.LocalName.Equals(nodeName);
            if (!isTestSuiteNode || !element.HasAttributes) return false;

            var typeAttribute = element.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("type"));
            if (typeAttribute == null) return false;

            return !string.IsNullOrEmpty(typeAttribute.Value) && typeAttribute.Value.Equals("TestFixture");
        }

        private static bool IsTestCaseNode(XElement element)
        {
            const string nodeName = "test-case";
            var isTestCaseNode = element.Name.LocalName.Equals(nodeName);
            if (!isTestCaseNode || !element.HasAttributes) return false;

            var nameAttribute = element.Attributes().FirstOrDefault(x => x.Name.LocalName.Equals("name"));
            return nameAttribute != null && !string.IsNullOrEmpty(nameAttribute.Value);
        }
    }
}

/* -------------------------------------------------------------------------------------------------
   Restricted - Copyright (C) Siemens Healthcare GmbH/Siemens Medical Solutions USA, Inc., 2020. All rights reserved
   ------------------------------------------------------------------------------------------------- */
   
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.DropFolderReaderLogic
{
    internal class DropFolderReader
    {
        private const string TestRunCombinedFileName = "TestRun-combined.xml";
        private const string NUnitFileName = "NUnit.xml";

        public EventHandler<int> ProgressChanged;
        public EventHandler<IList<TestResult>> Completed;

        public EventHandler<string> ActionChanged;

        public async void Read(string dropFolderPath)
        {
            var analyzedFilesCount = 0;
            var testDataList = new List<TestResult>();

            ActionChanged?.Invoke(this, "Collecting Result files from drop folder.");

            var fileNames = await GetFiles(dropFolderPath);

            ActionChanged?.Invoke(this, "Processing result files.");

            var asyncWorker = new AsyncWorker(onDoWork: (o, progressAction) =>
            {
                progressAction(0, testDataList);
                Parallel.ForEach(fileNames, file =>
                {
                    var isTestRunCombinedFile = file.CaseInsensitiveContains(TestRunCombinedFileName);
                    testDataList.AddRange(isTestRunCombinedFile ? TestRunCombinedReader.Read(Path.GetFullPath(file)) : NUnitReader.Read(Path.GetFullPath(file)));
                    analyzedFilesCount++;
                    var percentage = (decimal) analyzedFilesCount / fileNames.Count * 100;
                    progressAction((int) Math.Round(percentage), testDataList);
                });
            }, data: null)
            {
                ProgressChanged = (sender, progress) => { ProgressChanged?.Invoke(sender, progress); },
                OnComplete = (sender, data) => { Completed?.Invoke(sender, (IList<TestResult>) data); }
            };


            asyncWorker.RunAsync();
        }

        private static async Task<List<string>> GetFiles(string dropFolderPath)
        {
            var subDirectoryPaths = GetDropLocationSubDirectories(dropFolderPath);
            var files = new List<string>();

            await Task.Run(() =>
            {
                foreach (var subDirectories in subDirectoryPaths.Select(Directory.GetDirectories))
                {
                    Parallel.ForEach(subDirectories, subDirectory =>
                    {
                        var testRunCombinedFilePath = $"{subDirectory}\\{TestRunCombinedFileName}";
                        if (File.Exists(testRunCombinedFilePath))
                        {
                            files.Add(testRunCombinedFilePath);
                        }
                        else
                        {
                            var nUnitFilePath = $"{subDirectory}\\{NUnitFileName}";
                            if (File.Exists(nUnitFilePath))
                            {
                                files.Add(nUnitFilePath);
                            }
                        }
                    });
                }
            });

            return files;
        }

        public static bool IsValidParentDropFolder(string dropFolderPath)
        {
            return GetDropLocationSubDirectories(dropFolderPath).Count > 0;
        }

        private static List<string> GetDropLocationSubDirectories(string dropFolderPath)
        {
            return new List<string> {dropFolderPath + @"\FORMAL", dropFolderPath + @"\logs\x64\Release\testOutput", dropFolderPath + @"\logs\x64\Debug\testOutput"}
                .Where(Directory.Exists).ToList();
        }
    }
}

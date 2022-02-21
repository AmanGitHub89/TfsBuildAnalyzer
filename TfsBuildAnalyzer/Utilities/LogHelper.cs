using System;
using System.IO;


namespace TfsBuildAnalyzer.Utilities
{
    internal static class LogHelper
    {
        private static string myDirectory;

        static LogHelper()
        {
            myDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        public static void LogMessage(string message)
        {
            // ReSharper disable once LocalizableElement
            File.AppendAllText($"{myDirectory}\\TfsBuildAnalyzerLog.txt", $"{message}{Environment.NewLine}");
        }

        public static void LogException(Exception ex)
        {
            // ReSharper disable once LocalizableElement
            File.AppendAllText($"{myDirectory}\\TfsBuildAnalyzerLog.txt", $"{ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
            if (ex.InnerException != null)
            {
                File.AppendAllText($"{myDirectory}\\TfsBuildAnalyzerLog.txt", $"{ex.InnerException.Message}{Environment.NewLine}{ex.InnerException.StackTrace}{Environment.NewLine}");
            }
        }
    }
}

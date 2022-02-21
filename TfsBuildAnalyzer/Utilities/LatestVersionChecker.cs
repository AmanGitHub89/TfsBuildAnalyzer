using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace TfsBuildAnalyzer.Utilities
{
    internal static class LatestVersionChecker
    {
        private const string ConfigFilePath = @"\\INBHCRELPSRV002\TeamTool_Config_MiDevTools$\MI_Development_Tools.xml";

        public static void CheckLatestVersion(Action<bool> onCheckCompleted)
        {
            Task.Run(async () =>
            {
                var isLatestVersion = await IsLatestVersionAsync();
                onCheckCompleted(isLatestVersion);
            });
        }

        private static async Task<bool> IsLatestVersionAsync()
        {
            var isLatestVersion = true;
            await Task.Run(() =>
            {
                try
                {
                    using (var fileStream = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var document = XDocument.Load(fileStream);
                        var rootElement = document.Root;
                        if (rootElement == null)
                        {
                            throw new Exception("Couldn't read document.");
                        }

                        var elements = rootElement.Descendants().Where(x => x.Name.LocalName.Equals("Element") && x.HasAttributes);
                        var toolElement = elements.FirstOrDefault(IsMyToolElement);
                        if (toolElement == null)
                        {
                            throw new Exception("Tool element not found.");
                        }
                        var versionString = toolElement.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("VersionString"))?.Value;
                        if (string.IsNullOrEmpty(versionString))
                        {
                            throw new Exception("Version string not found.");
                        }
                        var version = new Version(versionString);

                        var result = GetCurrentVersion().CompareTo(version);
                        if (result < 0)
                        {
                            isLatestVersion = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogMessage($"Could not load team tool config file. {ex.Message}");
                }
            });
            return isLatestVersion;
        }

        private static Version GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return new Version(fvi.FileVersion);
        }

        private static bool IsMyToolElement(XElement element)
        {
            var isMsi = element.Attribute("ElementType") != null && element.Attribute("ElementType").Value.Equals("Msi");
            if (!isMsi) return false;
            var nameElement = element.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("Name"));
            return nameElement != null && nameElement.Value.Equals("Night-Run/RTC analyzer tool and extension");
        }
    }
}

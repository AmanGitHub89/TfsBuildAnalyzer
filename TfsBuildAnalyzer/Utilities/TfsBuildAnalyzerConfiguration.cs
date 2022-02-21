using System;
using System.Configuration;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

using TfsBuildAnalyzer.Types;


namespace TfsBuildAnalyzer.Utilities
{
    internal static class TfsBuildAnalyzerConfiguration
    {
        private static int? myConsistentFailureMinimumFailCount;
        private static string myLastSelectedBuildType;
        public static bool? myConnectToDatabase;
        public static int? myConnectionTimeout;
        public static int? myCommandTimeout;
        private static string mySqlServer;
        public static bool myHasCheckedSqlServerName;

        private static string myTfsServerPath;
        private static string myIncludedProjectCollections;
        private static string myIncludedProjectTypes;
        private static string myMyFavoriteBuilds;
        private static string myLastSelectedProjectCatalogName;
        private static string myLastSelectedProjectTypeName;
        private static ProjectStructure myProjectStructure;

        private static readonly string myProjectStructureFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}\\ProjectStructure.txt";

        public static int ConsistentFailureMinimumFailCount
        {
            get => myConsistentFailureMinimumFailCount ?? (myConsistentFailureMinimumFailCount = int.Parse(GetValue(nameof(ConsistentFailureMinimumFailCount)))).GetValueOrDefault();
            set
            {
                myConsistentFailureMinimumFailCount = value;
                UpdateValue(nameof(ConsistentFailureMinimumFailCount), value);
            }
        }

        public static string LastSelectedBuildType
        {
            get => myLastSelectedBuildType ?? (myLastSelectedBuildType = GetValue(nameof(LastSelectedBuildType)));
            set
            {
                myLastSelectedBuildType = value;
                UpdateValue(nameof(LastSelectedBuildType), value);
            }
        }

        public static bool ConnectToDatabase
        {
            get => myConnectToDatabase ?? (myConnectToDatabase = bool.Parse(GetValue(nameof(ConnectToDatabase)))).GetValueOrDefault();
            set
            {
                myConnectToDatabase = value;
                UpdateValue(nameof(ConnectToDatabase), value);
            }
        }

        public static int ConnectionTimeout
        {
            get => myConnectionTimeout ?? (myConnectionTimeout = int.Parse(GetValue(nameof(ConnectionTimeout)))).GetValueOrDefault();
            set
            {
                myConnectionTimeout = value;
                UpdateValue(nameof(ConnectionTimeout), value);
            }
        }

        public static int CommandTimeout
        {
            get => myCommandTimeout ?? (myCommandTimeout = int.Parse(GetValue(nameof(CommandTimeout)))).GetValueOrDefault();
            set
            {
                myCommandTimeout = value;
                UpdateValue(nameof(CommandTimeout), value);
            }
        }

        public static string SqlServer
        {
            get
            {
                if (string.IsNullOrEmpty(mySqlServer))
                {
                    mySqlServer = GetValue(nameof(SqlServer));
                    if (string.IsNullOrEmpty(mySqlServer))
                    {
                        mySqlServer = $"{Environment.MachineName}\\MSSQLSERVER_SYDS";
                    }
                }
                return mySqlServer;
            }
            set
            {
                mySqlServer = value;
                UpdateValue(nameof(SqlServer), value);
            }
        }

        public static bool HasCheckedSqlServerName
        {
            get
            {
                bool.TryParse(GetValue(nameof(HasCheckedSqlServerName)), out myHasCheckedSqlServerName);
                return myHasCheckedSqlServerName;
            }
            set
            {
                myHasCheckedSqlServerName = value;
                UpdateValue(nameof(HasCheckedSqlServerName), value);
            }
        }

        public static string TfsServerPath
        {
            get => myTfsServerPath ?? (myTfsServerPath = GetValue(nameof(TfsServerPath)));
            set
            {
                myTfsServerPath = value;
                UpdateValue(nameof(TfsServerPath), value);
            }
        }

        public static string IncludedProjectCollections
        {
            get => myIncludedProjectCollections ?? (myIncludedProjectCollections = GetValue(nameof(IncludedProjectCollections)));
            set
            {
                myIncludedProjectCollections = value;
                UpdateValue(nameof(IncludedProjectCollections), value);
            }
        }

        public static string IncludedProjectTypes
        {
            get => myIncludedProjectTypes ?? (myIncludedProjectTypes = GetValue(nameof(IncludedProjectTypes)));
            set
            {
                myIncludedProjectTypes = value;
                UpdateValue(nameof(IncludedProjectTypes), value);
            }
        }

        public static string MyFavoriteBuilds
        {
            get => myMyFavoriteBuilds ?? (myMyFavoriteBuilds = GetValue(nameof(MyFavoriteBuilds)));
            set
            {
                myMyFavoriteBuilds = value;
                UpdateValue(nameof(MyFavoriteBuilds), value);
            }
        }

        public static string LastSelectedProjectCatalogName
        {
            get => myLastSelectedProjectCatalogName ?? (myLastSelectedProjectCatalogName = GetValue(nameof(LastSelectedProjectCatalogName)));
            set
            {
                myLastSelectedProjectCatalogName = value;
                UpdateValue(nameof(LastSelectedProjectCatalogName), value);
            }
        }

        public static string LastSelectedProjectTypeName
        {
            get => myLastSelectedProjectTypeName ?? (myLastSelectedProjectTypeName = GetValue(nameof(LastSelectedProjectTypeName)));
            set
            {
                myLastSelectedProjectTypeName = value;
                UpdateValue(nameof(LastSelectedProjectTypeName), value);
            }
        }

        public static ProjectStructure ProjectStructure
        {
            get
            {
                try
                {
                    if (myProjectStructure != null) return myProjectStructure;

                    if (!File.Exists(myProjectStructureFilePath)) return null;

                    var jsonString = File.ReadAllText(myProjectStructureFilePath);
                    if (string.IsNullOrEmpty(jsonString)) return null;

                    var settings = new JsonSerializerSettings { ContractResolver = new ProjectStructureJsonContractResolver() };
                    var projectStructure = JsonConvert.DeserializeObject<ProjectStructure>(jsonString, settings);
                    myProjectStructure = projectStructure;
                    return myProjectStructure;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                    return null;
                }
            }
            set
            {
                try
                {
                    myProjectStructure = value;
                    var serializedValue = JsonConvert.SerializeObject(value);
                    File.WriteAllText(myProjectStructureFilePath, serializedValue);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
        }


        private static string GetValue(string name)
        {
            return ConfigurationManager.AppSettings[name] ?? string.Empty;
        }

        private static void UpdateValue(string name, object value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (!config.AppSettings.Settings.AllKeys.Contains(name))
            {
                config.AppSettings.Settings.Add(name, value.ToString());
            }
            else
            {
                config.AppSettings.Settings[name].Value = value.ToString();
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}

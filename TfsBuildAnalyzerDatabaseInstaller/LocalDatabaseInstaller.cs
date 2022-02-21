using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Win32;


namespace TfsBuildAnalyzerDatabaseInstaller
{
    internal static class LocalDatabaseInstaller
    {
        private static readonly string myDatabaseName = "TfsBuildAnalyzer";
        private static readonly string myAnalyzerInstallPath;
        private static SqlConnection myMasterSqlConnection;
        private static SqlConnection myTfsBuildAnalyzerSqlConnection;
        private static string myServerName;
        private static Action<string> myOnFileProcessed;

        static LocalDatabaseInstaller()
        {
            myAnalyzerInstallPath = GetAnalyzerInstallPath();
            myServerName = $"{Environment.MachineName}\\MSSQLSERVER_SYDS";
            UpdateSqlConnection();
        }

        internal static string GetServer()
        {
            return myServerName;
        }

        internal static void SetServer(string serverName)
        {
            myServerName = serverName;
            UpdateSqlConnection();
        }

        private static void UpdateSqlConnection()
        {
            myMasterSqlConnection?.Dispose();
            myTfsBuildAnalyzerSqlConnection?.Dispose();
            var sqlConnectionString = $"Server={myServerName};Database=master;Integrated security=SSPI;Connection Timeout=5";
            myMasterSqlConnection = new SqlConnection(sqlConnectionString);
            sqlConnectionString = $"Server={myServerName};Database={myDatabaseName};Integrated security=SSPI;Connection Timeout=10";
            myTfsBuildAnalyzerSqlConnection = new SqlConnection(sqlConnectionString);
        }

        internal static async Task<bool> CanConnectToServer()
        {
            var canConnect = false;
            await Task.Run(() =>
            {
                try
                {
                    const string query = "SELECT 1";

                    using (var sqlCmd = new SqlCommand(query, myMasterSqlConnection))
                    {
                        sqlCmd.CommandTimeout = 5;
                        myMasterSqlConnection.Open();

                        var resultObj = sqlCmd.ExecuteScalar();

                        if (resultObj != null)
                        {
                            int.TryParse(resultObj.ToString(), out var result);
                            canConnect = result == 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(ex.StackTrace);
                }
                finally
                {
                    if (myMasterSqlConnection.State == ConnectionState.Open)
                    {
                        myMasterSqlConnection.Close();
                    }
                }
            });
            return canConnect;
        }

        public static bool? SetupDatabase(Action<string> onFileProcessed)
        {
            string message;
            if (string.IsNullOrEmpty(myAnalyzerInstallPath))
            {
                message = $"Invalid install path from registry {myAnalyzerInstallPath}";
                LogMessage(message);
                myOnFileProcessed?.Invoke(message);
                return false;
            }

            myOnFileProcessed = onFileProcessed;

            LogMessage($"ConnectionString : {myMasterSqlConnection.ConnectionString}");
            LogMessage($"ConnectionString : {myTfsBuildAnalyzerSqlConnection.ConnectionString}");

            var databaseAlreadyExists = DatabaseExists();

            if (databaseAlreadyExists)
            {
                var dbExistsMessage = $"Database '{myDatabaseName}' already exists in sql server '{myServerName}'!{Environment.NewLine}{Environment.NewLine}Overwrite database?";
                var result = MessageBox.Show(dbExistsMessage, "Database already exists!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.No) return null;

                message = "Over-writing database.";
                LogMessage(message);
                myOnFileProcessed?.Invoke(message);
            }

            var databaseExists = databaseAlreadyExists || CreateDatabase();

            bool isSuccess;
            if (databaseExists)
            {
                isSuccess = RunScripts();
                if (!isSuccess)
                {
                    message = "Database installation failed.";
                    LogMessage(message);
                    myOnFileProcessed?.Invoke(message);
                }
            }
            else
            {
                isSuccess = false;
                message = "Could not create Database. Skip running scripts. Aborting database installation...";
                LogMessage(message);
                myOnFileProcessed?.Invoke(message);
            }

            message = $"Log file generated at {myAnalyzerInstallPath}\\SqlInstallLog.txt";
            LogMessage(message);
            myOnFileProcessed?.Invoke(message);

            return isSuccess;
        }

        private static bool CreateDatabase()
        {
            var databaseFilesDirectoryPath = $"{myAnalyzerInstallPath}\\DatabaseFiles\\";
            if (!Directory.Exists(databaseFilesDirectoryPath))
            {
                Directory.CreateDirectory(databaseFilesDirectoryPath);
            }
            var queryString = $"CREATE DATABASE {myDatabaseName} ON PRIMARY " +
                      $"(NAME = {myDatabaseName}_Data, " +
                      $"FILENAME = '{databaseFilesDirectoryPath}{myDatabaseName}Data.mdf', " +
                      "SIZE = 2MB, MAXSIZE = 10240MB, FILEGROWTH = 10%) " +
                      $"LOG ON (NAME = {myDatabaseName}_Log, " +
                      $"FILENAME = '{databaseFilesDirectoryPath}{myDatabaseName}Log.ldf', " +
                      "SIZE = 1MB, " +
                      "MAXSIZE = 200MB, " +
                      "FILEGROWTH = 10%)";

            var message = $"Creating new database {myDatabaseName}";
            LogMessage(message);
            LogMessage(queryString);
            myOnFileProcessed?.Invoke(message);

            using (var sqlCommand = new SqlCommand(queryString, myMasterSqlConnection))
            {
                try
                {
                    myMasterSqlConnection.Open();
                    sqlCommand.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage(ex.StackTrace);
                    return false;
                }
                finally
                {
                    if (myMasterSqlConnection.State == ConnectionState.Open)
                    {
                        myMasterSqlConnection.Close();
                    }
                }
            }
        }

        private static bool RunScripts()
        {
            const string createTablesScriptFileName = "CreateTable.sql";
            var scriptFileLocation = $"{myAnalyzerInstallPath}\\DatabaseScripts\\";
            var filesList = new DirectoryInfo(scriptFileLocation).GetFiles();

            var createTableScript = filesList.FirstOrDefault(x => x.Name.Equals(createTablesScriptFileName));
            if (createTableScript == null)
            {
                return false;
            }

            var allPassed = ExecuteCommand(File.ReadAllText(createTableScript.FullName));
            if (allPassed)
            {
                var message = $"Script PASSED : {createTableScript.Name}";
                LogMessage(message);
                myOnFileProcessed?.Invoke(message);
            }
            else
            {
                var message = $"Script FAILED : {createTableScript.Name}{Environment.NewLine}Aborting database installation...";
                LogMessage(message);
                myOnFileProcessed?.Invoke(message);
            }

            foreach (var file in filesList.Where(x => !x.Name.Equals(createTablesScriptFileName)))
            {
                if (ExecuteCommand(File.ReadAllText(file.FullName)))
                {
                    var message = $"Script PASSED : {file.Name}";
                    LogMessage(message);
                    myOnFileProcessed?.Invoke(message);
                }
                else
                {
                    var message = $"Script FAILED : {file.Name}{Environment.NewLine}Aborting database installation...";
                    LogMessage(message);
                    myOnFileProcessed?.Invoke(message);
                    allPassed = false;
                    break;
                }
            }


            return allPassed;
        }

        private static bool ExecuteCommand(string command)
        {
            var commandsList = command.Split(new[] { "\r\nGO" }, StringSplitOptions.RemoveEmptyEntries);

            var allCommandSucceeded = true;
            foreach (var commandToRun in commandsList)
            {
                using (var sqlCommand = new SqlCommand(commandToRun, myTfsBuildAnalyzerSqlConnection))
                {
                    try
                    {
                        myTfsBuildAnalyzerSqlConnection.Open();
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        LogMessage(ex.StackTrace);
                        allCommandSucceeded = false;
                        break;
                    }
                    finally
                    {
                        if (myTfsBuildAnalyzerSqlConnection.State == ConnectionState.Open)
                        {
                            myTfsBuildAnalyzerSqlConnection.Close();
                        }
                    }
                }
            }

            return allCommandSucceeded;
        }

        private static string GetAnalyzerInstallPath()
        {
            try
            {
                using (var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = localMachine.OpenSubKey("Software\\TfsBuildAnalyzer"))
                    {
                        var value = key?.GetValue("InstallPath");
                        if (value != null)
                        {
                            var path = value.ToString();
                            if (path.EndsWith("\\"))
                            {
                                path = path.TrimEnd('\\');
                            }
                            return path;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
            return string.Empty;
        }

        private static bool DatabaseExists()
        {
            try
            {
                var query = $"SELECT database_id FROM sys.databases WHERE Name = '{myDatabaseName}'";

                using (var sqlCmd = new SqlCommand(query, myMasterSqlConnection))
                {
                    myMasterSqlConnection.Open();

                    var resultObj = sqlCmd.ExecuteScalar();

                    var databaseId = 0;

                    if (resultObj != null)
                    {
                        int.TryParse(resultObj.ToString(), out databaseId);
                    }

                    var databaseExists = databaseId > 0;

                    if (databaseExists)
                    {
                        var message = $"Database {myDatabaseName} already exists. Skipping database creation...";
                        LogMessage(message);
                        myOnFileProcessed?.Invoke(message);
                    }

                    return databaseExists;
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.StackTrace);
                return false;
            }
            finally
            {
                if (myMasterSqlConnection.State == ConnectionState.Open)
                {
                    myMasterSqlConnection.Close();
                }
            }
        }

        private static void LogMessage(string message)
        {
            File.AppendAllText($"{myAnalyzerInstallPath}\\SqlInstallLog.txt", $"{DateTime.Now} {message}{Environment.NewLine}");
        }
    }
}

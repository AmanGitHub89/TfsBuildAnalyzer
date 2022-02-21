#region Imports
using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

#endregion

namespace DbConnect
{
    // ReSharper disable once InconsistentNaming
    internal class DBConnection : IDisposable
    {
        #region Variable Declarations
        private SqlConnection mySqlConnection;
        private static string myConnectionString;

        internal static int CommandTimeout { get; private set; }

        #endregion

        static DBConnection()
        {
            UpdateConnectionString();
        }

        #region UpdateConnectionString
        internal static void UpdateConnectionString()
        {
            var server = ConfigurationManager.AppSettings["SqlServer"];
            var database = ConfigurationManager.AppSettings["Database"];
            bool.TryParse(ConfigurationManager.AppSettings["UseWindowsLogin"], out var useWindowsLogin);
            var username = ConfigurationManager.AppSettings["Username"];
            var password = Decryptor.Decrypt(ConfigurationManager.AppSettings["Password"]);
            var dbConnectionTimeout = int.TryParse(ConfigurationManager.AppSettings["ConnectionTimeout"], out var connectionTimeout) ? connectionTimeout : 10;

            CommandTimeout = int.TryParse(ConfigurationManager.AppSettings["CommandTimeout"], out var commandTimeout) ? commandTimeout : 10;

            myConnectionString =
                $"Data Source={server};Initial Catalog={database};Integrated Security={useWindowsLogin.ToString().ToUpper()};User Id={username};Password={password};Connection Timeout={dbConnectionTimeout}";
        }
        #endregion

        #region OpenConnection
        public SqlConnection OpenConnection()
        {
            mySqlConnection = new SqlConnection(myConnectionString);
            mySqlConnection.Open();
            return mySqlConnection;
        }
        #endregion

        #region IsConnectionOpen
        public bool IsConnectionOpen()
        {
            return mySqlConnection.State == ConnectionState.Open;
        }
        #endregion

        #region CloseConnection
        //Closes the DataBase connection.
        public void CloseConnection()
        {
            try
            {
                mySqlConnection.Close();
            }
            catch
            {
                // ignored
            }
        }
        #endregion

        public void Dispose()
        {
            if (IsConnectionOpen())
            {
                CloseConnection();
            }
            mySqlConnection.Dispose();
            mySqlConnection = null;
        }
    }
}

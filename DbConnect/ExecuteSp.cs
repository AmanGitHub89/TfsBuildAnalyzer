#region Imports
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

#endregion

namespace DbConnect
{
    public class ExecuteSp
    {
        private readonly List<SqlParameter> mySqlParameters = new List<SqlParameter>();

        #region AddParameter
        public ExecuteSp AddParameter(string name, SqlDbType type, object value, int size = 0, ParameterDirection direction = ParameterDirection.Input)
        {
            mySqlParameters.Add(new SqlParameter(name, type, 0, direction, false, 1, 1, "A", DataRowVersion.Default,
                value));
            return this;
        }

        public ExecuteSp AddVarCharParameter(string name, object value, int size, ParameterDirection direction = ParameterDirection.Input)
        {
            return AddParameter(name, SqlDbType.VarChar, value, size, direction);
        }

        public ExecuteSp AddParameter(SqlParameter sqlParameter)
        {
            mySqlParameters.Add(sqlParameter);
            return this;
        }
        #endregion

        #region ExecuteSPReturnDataSet
        public async Task<DataSet> ExecuteSpReturnDataSet(string storedProcedureName, string[] tableNames = null)
        {
            DataSet dataSet = null;
            await Task.Run(() =>
            {
                using (var dbConnection = new DBConnection())
                {
                    var sqlConnection = dbConnection.OpenConnection();
                    if (dbConnection.IsConnectionOpen())
                    {
                        using (var sqlCommand = new SqlCommand(storedProcedureName, sqlConnection)
                        {
                            CommandType = CommandType.StoredProcedure,
                            CommandTimeout = DBConnection.CommandTimeout
                        })
                        {
                            sqlCommand.Parameters.AddRange(mySqlParameters.ToArray());
                            using (var dataAdapter = new SqlDataAdapter(sqlCommand))
                            {
                                if (tableNames != null)
                                {
                                    dataAdapter.TableMappings.Add("Table", tableNames.First());
                                    for (var index = 1; index < tableNames.Length; index++)
                                    {
                                        dataAdapter.TableMappings.Add("Table" + index, tableNames[index]);
                                    }
                                }

                                dataSet = new DataSet();
                                dataAdapter.Fill(dataSet);
                            }
                        }
                    }
                }
            });
            return dataSet;
        }
        #endregion

        #region ExecuteSPReturnScalar
        //Returns the First row as an object.
        public object ExecuteSpReturnScalar(string storedProcedureName)
        {
            using (var dbConnection = new DBConnection())
            {
                var sqlConnection = dbConnection.OpenConnection();
                if (!dbConnection.IsConnectionOpen()) return null;

                using (var sqlCommand = new SqlCommand(storedProcedureName, sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = DBConnection.CommandTimeout
                })
                {
                    sqlCommand.Parameters.AddRange(mySqlParameters.ToArray());
                    return sqlCommand.ExecuteScalar();
                }
            }
        }
        #endregion

        #region ExecuteNonQuery
        public bool ExecuteNonQuery(string storedProcedureName)
        {
            using (var dbConnection = new DBConnection())
            {
                var sqlConnection = dbConnection.OpenConnection();
                if (!dbConnection.IsConnectionOpen()) return false;

                using (var sqlCommand = new SqlCommand(storedProcedureName, sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = DBConnection.CommandTimeout
                })
                {
                    sqlCommand.Parameters.AddRange(mySqlParameters.ToArray());
                    sqlCommand.ExecuteNonQuery();
                    return true;
                }
            }
        }
        #endregion

    }
}

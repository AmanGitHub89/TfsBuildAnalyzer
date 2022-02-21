using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;


namespace TfsBuildAnalyzerModels
{
    public class CommonFunctions
    {
        #region DataTbale_Functions
        public static DataTable ConvertToDataTable<T>(List<T> items)
        {
            var dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }
            foreach (var item in items)
            {
                var values = new object[properties.Length];
                for (var i = 0; i < properties.Length; i++)
                {
                    //inserting property values to dataTable rows
                    values[i] = properties[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check dataTable
            return dataTable;
        }
        public static DataTable ConvertStringListToDataTable(List<string> list)
        {
            var table = new DataTable();
            table.Columns.Add("UserID");
            list.ForEach(str => table.Rows.Add(str));
            return table;
        }
        #endregion

        #region IsStringGUID
        public static bool IsStringGuid(string guidString)
        {
            return Guid.TryParse(guidString, out _);
        }
        #endregion

        #region AreStringsNullOrEmpty
        public static bool AreStringsNullOrEmpty(params string[] inputStrings)
        {
            return inputStrings.Any(string.IsNullOrEmpty);
        }
        #endregion

        #region DataSet_Functions
        public static bool DoesFirstRowColEqualOne(DataSet dataSet)
        {
            return FirstRowColString(dataSet).Equals("1");
        }
        public static string FirstRowColString(DataSet dataSet)
        {
            if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                return dataSet.Tables[0].Rows[0][0].ToString();
            }

            return "-1";
        }
        public static bool DataSetHasRowsInFirstTable(DataSet dataSet)
        {
            return dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0;
        }
        public static bool DataSetHasTable(DataSet dataSet)
        {
            return dataSet != null && dataSet.Tables.Count > 0;
        }
        #endregion
    }
}

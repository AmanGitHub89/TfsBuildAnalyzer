using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace DbConnect
{
    internal static class DataTableHelper
    {
        public static T ToObject<T>(this DataRow row) where T : class, new()
        {
            var obj = new T();

            foreach (var prop in obj.GetType().GetProperties())
            {
                try
                {
                    if (prop.PropertyType.IsGenericType && prop.PropertyType.Name.Contains("Nullable"))
                    {
                        if (!string.IsNullOrEmpty(row[prop.Name].ToString()))
                            prop.SetValue(obj, Convert.ChangeType(row[prop.Name], Nullable.GetUnderlyingType(prop.PropertyType), null));
                        //else do nothing
                    }
                    else if (prop.PropertyType.IsEnum)
                    {
                        var enumValue = Enum.Parse(prop.PropertyType, row[prop.Name].ToString());
                        prop.SetValue(obj, enumValue, null);
                    }
                    else
                        prop.SetValue(obj, Convert.ChangeType(row[prop.Name], prop.PropertyType), null);
                }
                catch
                {
                    continue;
                }
            }
            return obj;
        }

        /// <summary>
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="table">DataTable</param>
        /// <returns>List with generic objects</returns>
        public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            try
            {
                return Enumerable.Select(table.AsEnumerable(), row => row.ToObject<T>()).ToList();
            }
            catch
            {
                return null;
            }
        }
    }
}

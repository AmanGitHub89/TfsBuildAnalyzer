using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;

using Microsoft.Office.Interop.Excel;

using TfsBuildAnalyzerModels;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer
{
    internal static class ExportToExcel
    {
        public static bool Export<T>(List<T> exportObjectList, Action<int, object> progressChanged) where T : class
        {
            if (exportObjectList.Count == 0)
            {
                MessageBoxHelper.ShowErrorMessage("Nothing to export.", "List contains no data to export.");
                progressChanged?.Invoke(100, null);
                return false;
            }


            Application excelApplication = null;
            Worksheet excelSheet = null;
            try
            {
                excelApplication = new Application {Visible = false, DisplayAlerts = false};

                var excelWorkbook = excelApplication.Workbooks.Add(Type.Missing);
                excelSheet = (Worksheet)excelWorkbook.ActiveSheet;
                excelSheet.Name = "Test_Results";

                var properties = GetProperties(exportObjectList.First());
                var totalCells = properties.Count * exportObjectList.Count + properties.Count;

                var cellDataToWriteInExcel = GetCellDataToWriteInExcel(exportObjectList, properties);

                var cellsWrittenCount = 0;

                Parallel.ForEach(cellDataToWriteInExcel, cellData =>
                {
                    var value = cellData.Data == null ? string.Empty : cellData.Data.ToString(); 
                    excelSheet.Cells[cellData.RowNumber, cellData.ColumnNumber] = value;

                    if (cellData.ColumnNumber == 1 && !string.IsNullOrEmpty(cellData.Color))
                    {
                        var range = excelApplication.Range[excelSheet.Cells[cellData.RowNumber, 1], excelSheet.Cells[cellData.RowNumber, properties.Count]];
                        range.Interior.Color = System.Drawing.ColorTranslator.FromHtml(cellData.Color);
                    }

                    cellsWrittenCount += 1;
                    var percentage = (decimal)cellsWrittenCount / totalCells * 100;
                    var percentageInt = (int) Math.Round(percentage);
                    progressChanged(percentageInt, null);
                });

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                throw;
            }
            finally
            {
                if (excelSheet != null)
                {
                    excelSheet.UsedRange.EntireRow.RowHeight = 30;
                    excelSheet.Columns.AutoFit();
                }
                if (excelApplication != null)
                {
                    excelApplication.WindowState = XlWindowState.xlMaximized;
                    excelApplication.Visible = true;
                }
                progressChanged?.Invoke(100, null);
            }
        }

        private static List<ExcelCellData> GetCellDataToWriteInExcel<T>(List<T> exportObjectList, List<PropertyInfo> properties)
        {
            var excelCellDataList = new List<ExcelCellData>();

            for (var i = 1; i <= properties.Count; i++)
            {
                excelCellDataList.Add(new ExcelCellData {RowNumber = 1, ColumnNumber = i, Data = properties[i - 1].Name, Color = ResultColors.ExcelHeader });

                for (var j = 1; j <= exportObjectList.Count; j++)
                {
                    var exportObject = exportObjectList[j - 1];
                    var excelCellData = new ExcelCellData { RowNumber = j + 1, ColumnNumber = i };
                    if (i == 1)
                    {
                        excelCellData.Color = GetBackgroundColor(exportObject);
                    }

                    var property = properties[i - 1];
                    var value = GetPropertyValue(property, exportObject);
                    if (!string.IsNullOrEmpty(value))
                    {
                        excelCellData.Data = value;
                    }
                    excelCellDataList.Add(excelCellData);
                }
            }

            return excelCellDataList;
        }

        public static string GetPropertyValue(PropertyInfo property, object exportObject)
        {
            var value = property.GetValue(exportObject);
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var stringList = new List<string>();

                if (value is IEnumerable enumerable)
                {
                    stringList.AddRange(from object val in enumerable select val.ToString());
                }

                return string.Join(Environment.NewLine, stringList);
            }

            return value != null ? value.ToString() : string.Empty;
        }

        private static string GetBackgroundColor(object exportObject)
        {
            var colorProperty = exportObject.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttributes(typeof(ExcelCellColorAttribute), true).Length == 1);
            return colorProperty != null ? GetPropertyValue(colorProperty, exportObject) : string.Empty;
        }

        private static List<PropertyInfo> GetProperties(object exportObject)
        {
            return exportObject.GetType().GetProperties().Where(x => x.GetCustomAttributes(typeof(ExportToExcelAttribute), true).Length == 1).ToList();
        }


        private class ExcelCellData
        {
            public int ColumnNumber { get; set; }
            public int RowNumber { get; set; }
            public object Data { get; set; }
            public string Color { get; set; }
        }
    }
}

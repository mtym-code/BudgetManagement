using ClosedXML.Excel;
using BudgetManagement.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace BudgetManagement.Services
{
    public class ExpenseNonOperatingForecastExcelService
    {
        public void CreatePreviewExcel(string templatePath, string tempFilePath, IEnumerable<ForecastData> dataList)
        {
            File.Copy(templatePath, tempFilePath, true);
            using (var workbook = new XLWorkbook(tempFilePath))
            {
                if (workbook.TryGetWorksheet("経費営業外見通し", out var worksheet))
                {
                    PopulateDataToWorksheet(worksheet, dataList);
                }
                workbook.RecalculateAllFormulas();
                workbook.Save();
            }
        }

        public void ExportExcel(string templatePath, string outputPath, string year, string companyCode, string companyName, string targetMonth, IEnumerable<ForecastData> dataList)
        {
            File.Copy(templatePath, outputPath, true);
            using (var workbook = new XLWorkbook(outputPath))
            {
                if (workbook.TryGetWorksheet("Settings", out var settingsSheet))
                {
                    settingsSheet.Cell("B1").Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    settingsSheet.Cell("B4").Value = "経費営業外見通し";
                    settingsSheet.Cell("B5").Value = year;
                    settingsSheet.Cell("B6").Value = $"{companyCode}：{companyName}";
                    settingsSheet.Cell("B7").Value = $"{targetMonth}月";
                    settingsSheet.Hide();
                }

                if (workbook.TryGetWorksheet("経費営業外見通し", out var worksheet))
                {
                    worksheet.Protect();
                    PopulateDataToWorksheet(worksheet, dataList);
                }
                workbook.Save();
            }
        }

        private void PopulateDataToWorksheet(IXLWorksheet worksheet, IEnumerable<ForecastData> dataList)
        {
            var dataDict = new Dictionary<string, ForecastData>();
            foreach (var row in dataList)
            {
                dataDict[row.DeptCode] = row;
            }

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 5;

            for (int r = 5; r <= lastRow; r++)
            {
                string rawDeptString = worksheet.Cell(r, "A").GetString()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(rawDeptString)) continue;

                string deptCode = rawDeptString;
                if (rawDeptString.StartsWith("[") && rawDeptString.Contains("]"))
                {
                    int endIndex = rawDeptString.IndexOf("]");
                    deptCode = rawDeptString.Substring(1, endIndex - 1);
                }

                if (dataDict.TryGetValue(deptCode, out var rowData))
                {
                    decimal? displayKeihi1 = rowData.Keihimitoshi1 ?? rowData.Keihi1;
                    decimal? displayKeihi2 = rowData.Keihimitoshi2 ?? rowData.Keihi2;
                    decimal? displaySyueki = rowData.Syuekimitoshi ?? rowData.Syueki;

                    // 🌟 ここが原因でした！ H, I, K ではなく、正しい I, J, L 列に書き込むように直しています！
                    UnlockAndSetValue(worksheet.Cell(r, "I"), displayKeihi1);
                    UnlockAndSetValue(worksheet.Cell(r, "J"), displayKeihi2);
                    UnlockAndSetValue(worksheet.Cell(r, "L"), displaySyueki);
                }
            }
        }

        private void UnlockAndSetValue(IXLCell cell, decimal? value)
        {
            cell.Style.Protection.SetLocked(false);
            cell.Style.Fill.SetBackgroundColor(XLColor.White);

            // Excel本来の「マイナスなら赤字」という書式設定
            cell.Style.NumberFormat.Format = "#,##0;[Red]-#,##0";

            if (value.HasValue)
            {
                cell.Value = value.Value;
            }
            else
            {
                cell.Value = string.Empty;
            }
        }
    }
}
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;

namespace BudgetManagement.Services
{
    /// <summary>
    /// 部門別予算のExcelファイル出力およびプレビュー生成に関する操作を専門に行うサービスクラスです。
    /// </summary>
    public class DepartmentBudgetExcelService
    {
        public void CreatePreviewExcel(string templatePath, string tempFilePath, IEnumerable<dynamic> dataList)
        {
            File.Copy(templatePath, tempFilePath, true);
            using (var workbook = new XLWorkbook(tempFilePath))
            {
                if (workbook.TryGetWorksheet("部門別経費予算", out var worksheet))
                {
                    PopulateBudgetDataToWorksheet(worksheet, dataList);
                }
                workbook.RecalculateAllFormulas();
                workbook.Save();
            }
        }

        public void ExportBudgetExcel(string templatePath, string outputPath, string year, string companyCode, string companyName, string sectionDisplayName, string currentOperationType, IEnumerable<dynamic> budgetDataList)
        {
            File.Copy(templatePath, outputPath, true);
            using (var workbook = new XLWorkbook(outputPath))
            {
                UpdateSettingsSheet(workbook, year, companyCode, companyName, sectionDisplayName);

                if (workbook.TryGetWorksheet("部門別経費予算", out var worksheet))
                {
                    worksheet.Protect();
                    ApplyCellProtectionAndColors(worksheet, currentOperationType);
                    PopulateBudgetDataToWorksheet(worksheet, budgetDataList);
                    worksheet.Columns(1, 6).Hide();
                }
                workbook.Save();
            }
        }

        private void PopulateBudgetDataToWorksheet(IXLWorksheet worksheet, IEnumerable<dynamic> dataList)
        {
            var budgetDataDict = new Dictionary<string, dynamic>();
            foreach (var row in dataList)
            {
                budgetDataDict[$"{row.AccountCode}_{row.SubAccountCode}"] = row;
            }

            var targetCells = worksheet.Column("E").CellsUsed(c => c.GetString()?.Trim().ToUpper() == "Y" || c.GetString()?.Trim() == "Ｙ");

            foreach (var cell in targetCells)
            {
                int rowNum = cell.Address.RowNumber;
                if (rowNum < 5) continue;

                string accountCode = worksheet.Cell(rowNum, "H").GetString()?.Trim() ?? string.Empty;
                string subAccountCode = worksheet.Cell(rowNum, "I").GetString()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(accountCode) || string.IsNullOrEmpty(subAccountCode)) continue;

                if (budgetDataDict.TryGetValue($"{accountCode}_{subAccountCode}", out var rowData))
                {
                    worksheet.Cell(rowNum, "M").Value = rowData.Month04;
                    worksheet.Cell(rowNum, "N").Value = rowData.Month05;
                    worksheet.Cell(rowNum, "O").Value = rowData.Month06;
                    worksheet.Cell(rowNum, "P").Value = rowData.Month07;
                    worksheet.Cell(rowNum, "Q").Value = rowData.Month08;
                    worksheet.Cell(rowNum, "R").Value = rowData.Month09;
                    worksheet.Cell(rowNum, "T").Value = rowData.Month10;
                    worksheet.Cell(rowNum, "U").Value = rowData.Month11;
                    worksheet.Cell(rowNum, "V").Value = rowData.Month12;
                    worksheet.Cell(rowNum, "W").Value = rowData.Month01;
                    worksheet.Cell(rowNum, "X").Value = rowData.Month02;
                    worksheet.Cell(rowNum, "Y").Value = rowData.Month03;
                }
            }
        }

        private void UpdateSettingsSheet(XLWorkbook workbook, string year, string companyCode, string companyName, string sectionName)
        {
            if (workbook.TryGetWorksheet("Settings", out var settingsSheet))
            {
                settingsSheet.Cell("B1").Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                settingsSheet.Cell("B4").Value = "部門別経費予算";
                settingsSheet.Cell("B5").Value = year;
                settingsSheet.Cell("B6").Value = $"{companyCode}：{companyName}";
                settingsSheet.Cell("B7").Value = sectionName;
                settingsSheet.Hide();
            }
        }

        private void ApplyCellProtectionAndColors(IXLWorksheet worksheet, string currentOperationType)
        {
            string targetControlColumn = currentOperationType switch
            {
                "01" or "1" => "A", "02" or "2" => "B", "03" or "3" => "C", _ => "E"
            };

            foreach (var targetRow in worksheet.RowsUsed())
            {
                int r = targetRow.RowNumber();
                if (r < 5) continue;

                string controlFlag = targetRow.Cell(targetControlColumn).GetString()?.Trim().ToUpper() ?? string.Empty;
                var targetRange1 = worksheet.Range($"M{r}:R{r}");
                var targetRange2 = worksheet.Range($"T{r}:Y{r}");

                if (controlFlag == "Y" || controlFlag == "Ｙ")
                {
                    targetRange1.Style.Protection.SetLocked(false);
                    targetRange1.Style.Fill.SetBackgroundColor(XLColor.White);
                    targetRange2.Style.Protection.SetLocked(false);
                    targetRange2.Style.Fill.SetBackgroundColor(XLColor.White);
                }
                else
                {
                    targetRange1.Style.Protection.SetLocked(true);
                    targetRange1.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                    targetRange2.Style.Protection.SetLocked(true);
                    targetRange2.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                }
            }
        }
    }
}
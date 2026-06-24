using ClosedXML.Excel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BudgetManagement.Services
{
    /// <summary>
    /// Excelファイルを読み込み、WebView2で表示するためのHTMLプレビュー文字列を生成する専門クラスです。
    /// </summary>
    public class DepartmentBudgetHtmlService
    {
        private static readonly string[] TotalRowContainsKeywords = { "合計", "小計", "（計）", "(計)", "給料" };
        private static readonly string[] TotalRowExactKeywords = { "計" };

        public string GenerateHtmlFromExcel(string filePath, string currentOperationType, bool applyGrayOut)
        {
            var html = new StringBuilder();
            html.Append("<html><head><style>");
            html.Append("table { border-collapse: collapse; width: 100%; font-family: 'Segoe UI', sans-serif; } th, td { border: 1px solid #D1D5DB; padding: 6px; font-size: 12px; } th { background-color: #FFF5F0; font-weight: bold; text-align: center; position: sticky; top: 0; } .bg-light-orange { background-color: #FFF5F0 !important; } .bg-total { background-color: #E2EFDA !important; color: #9CA3AF !important; } .bg-readonly { background-color: #E5E7EB !important; color: #9CA3AF; } .bg-editable { background-color: #FFFFFF !important; } .text-left { text-align: left; } .text-right { text-align: right; } .col-group { min-width: 60px; } .col-name { min-width: 100px; }");
            html.Append("</style></head><body>");

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var workbook = new XLWorkbook(stream))
                {
                    html.Append("<table>");
                    var worksheet = workbook.Worksheets.First();
                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 4;
                    int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 7;

                    int targetControlColumn = currentOperationType switch { "01" or "1" => 1, "02" or "2" => 2, "03" or "3" => 3, _ => 5 };
                    var skippedCells = new HashSet<string>();
                    bool isFirstRow = true;

                    for (int r = 4; r <= lastRow; r++)
                    {
                        bool isTotalRow = false;
                        bool isEditableRow = false;

                        if (!isFirstRow)
                        {
                            for (int checkCol = 7; checkCol <= 10; checkCol++)
                            {
                                string cellText = worksheet.Cell(r, checkCol).GetFormattedString()?.Replace(" ", "").Replace(" ", "") ?? "";
                                if (TotalRowContainsKeywords.Any(k => cellText.Contains(k)) || TotalRowExactKeywords.Contains(cellText))
                                {
                                    isTotalRow = true; break;
                                }
                            }
                            string controlFlag = worksheet.Cell(r, targetControlColumn).GetString()?.Trim().ToUpper() ?? "";
                            isEditableRow = (controlFlag == "Y" || controlFlag == "Ｙ");
                        }

                        var row = worksheet.Row(r);
                        html.Append("<tr>");

                        for (int c = 7; c <= lastColumn; c++)
                        {
                            var cell = row.Cell(c);
                            string cellAddress = cell.Address.ToString();
                            if (skippedCells.Contains(cellAddress)) continue;

                            string displayValue = cell.GetFormattedString();
                            string mergeAttributes = "";

                            if (cell.IsMerged())
                            {
                                var range = cell.MergedRange();
                                if (range.ColumnCount() > 1) mergeAttributes += $" colspan='{range.ColumnCount()}'";
                                if (range.RowCount() > 1) mergeAttributes += $" rowspan='{range.RowCount()}'";
                                foreach (var rangeCell in range.Cells()) { skippedCells.Add(rangeCell.Address.ToString()); }
                            }

                            if (isFirstRow) html.Append($"<th{mergeAttributes}>{displayValue}</th>");
                            else
                            {
                                string cellClass = (c >= 11) ? "text-right" : "text-left";
                                if (c >= 7 && c <= 10) cellClass += " bg-light-orange";
                                if (isTotalRow && c >= 11 || c == 11 || c == 12 || c == 19) cellClass += " bg-total";
                                else if (c >= 13 && c <= 25 && c != 19 && applyGrayOut) cellClass += isEditableRow ? " bg-editable" : " bg-readonly";
                                if (c == 7) cellClass += " col-group"; else if (c == 10) cellClass += " col-name";

                                html.Append($"<td class='{cellClass}'{mergeAttributes}>{displayValue}</td>");
                            }
                        }
                        html.Append("</tr>");
                        isFirstRow = false;
                    }
                    html.Append("</table>");
                }
            }
            catch (IOException) { return "<html><body style='padding:24px; text-align:center;'><h3>プレビューを表示できません</h3><p>ファイルが使用中です。</p></body></html>"; }
            catch { return "<html><body style='padding:24px; text-align:center;'><h3>プレビューを表示できません</h3><p>対応していない形式です。</p></body></html>"; }

            html.Append("</body></html>");
            return html.ToString();
        }
    }
}
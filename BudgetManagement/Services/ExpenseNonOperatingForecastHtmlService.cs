using ClosedXML.Excel;
using System.IO;
using System.Text;
using System.Linq;

namespace BudgetManagement.Services
{
    public class ExpenseNonOperatingForecastHtmlService
    {
        public string GenerateHtmlFromExcel(string filePath)
        {
            var html = new StringBuilder();
            html.Append("<html><head><style>");
            html.Append("table { border-collapse: collapse; width: 100%; font-family: 'Segoe UI', sans-serif; } th, td { border: 1px solid #D1D5DB; padding: 6px; font-size: 12px; } th { background-color: #FFF5F0; font-weight: bold; text-align: center; position: sticky; top: 0; }");
            html.Append(".bg-editable { background-color: #FFFFFF !important; } .text-right { text-align: right; }");
            html.Append("</style></head><body>");

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 4;
                int lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 15;

                html.Append("<table>");
                for (int r = 4; r <= lastRow; r++)
                {
                    html.Append("<tr>");
                    for (int c = 1; c <= lastColumn; c++)
                    {
                        // B列（2列目）は削除して詰める
                        if (c == 2) continue;

                        // 🌟 【修正】「部」のヘッダーを縦に結合（Excelを再現）してズレを直す
                        if (r == 4 && c == 1)
                        {
                            var valStr = worksheet.Cell(r, c).GetFormattedString();
                            html.Append($"<th rowspan='2' style='vertical-align: middle;'>{valStr}</th>");
                            continue;
                        }
                        if (r == 5 && c == 1)
                        {
                            continue; // 4行目で rowspan='2' にしたのでスキップ
                        }

                        var cell = worksheet.Cell(r, c);
                        string val = cell.GetFormattedString();

                        // C列（3列目）以降は数値を右寄せにする
                        string cellClass = (c >= 3) ? "text-right" : "";

                        // 正しい入力列 I(9), J(10), L(12) の背景を白くする
                        if (c == 9 || c == 10 || c == 12) cellClass += " bg-editable";

                        // 値がマイナス(0未満)なら、赤字にするスタイルを追加
                        string cellStyle = "";
                        if (r > 5 && cell.TryGetValue<decimal>(out decimal numVal) && numVal < 0)
                        {
                            cellStyle = " style='color: red;'";
                        }

                        // 4行目と5行目はヘッダー（th）として扱う
                        if (r == 4 || r == 5) html.Append($"<th>{val}</th>");
                        else html.Append($"<td class='{cellClass}'{cellStyle}>{val}</td>");
                    }
                    html.Append("</tr>");
                }
                html.Append("</table>");
            }
            catch { return "<html><body><h3>プレビューを表示できません</h3></body></html>"; }

            html.Append("</body></html>");
            return html.ToString();
        }
    }
}
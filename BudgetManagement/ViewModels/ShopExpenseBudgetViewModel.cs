using BudgetManagement.Models;
using BudgetManagement.Repositories;
using BudgetManagement.Services;
using BudgetManagement.Services.ShopExpenseBudget;
using CommunityToolkit.Mvvm.ComponentModel;
using ClosedXML.Excel; // 🌟追加：Excel操作用
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;     // 🌟追加
using System.Text;     // 🌟追加
using System.Threading.Tasks;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public class ExpenseRateItem
    {
        public string DivisionName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public partial class ShopExpenseBudgetViewModel : ObservableObject
    {
        private readonly ShopExpenseBudgetService _service;
        private readonly ShopExpenseBudgetImportService _importService;
        private readonly ShopExpenseBudgetExcelService _excelService;
        private readonly ShopExpenseBudgetHtmlService _htmlService;

        public event EventHandler<string>? HtmlRenderRequested;
        public event EventHandler<string>? DbHtmlRenderRequested;

        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private string _year = DateTime.Now.Year.ToString();

        // 得意先リストと選択された得意先
        [ObservableProperty] private ObservableCollection<OperationMeiItem> _customers = new();
        [ObservableProperty] private OperationMeiItem? _selectedCustomer;

        // 経費率リスト
        [ObservableProperty] private ObservableCollection<ExpenseRateItem> _expenseRates = new();

        // 🛠️ コンストラクタ
        public ShopExpenseBudgetViewModel(
            ShopExpenseBudgetService service,
            ShopExpenseBudgetImportService importService,
            ShopExpenseBudgetExcelService excelService,
            ShopExpenseBudgetHtmlService htmlService)
        {
            _service = service;
            _importService = importService;
            _excelService = excelService;
            _htmlService = htmlService;
        }

        // =========================================================
        // 画面を開いたとき（Loadedイベントから呼ばれる）の処理
        // =========================================================
        public async Task LoadInitialDataAsync()
        {
            await LoadCustomersAsync();
        }

        partial void OnYearChanged(string value)
        {
            if (value?.Length == 4)
            {
                _ = LoadCustomersAsync();
            }
            else
            {
                Customers.Clear();
                SelectedCustomer = null;
            }
        }

        private async Task LoadCustomersAsync()
        {
            if (string.IsNullOrEmpty(Year) || Year.Length != 4) return;

            try
            {
                IsLoading = true;

                // DBから得意先一覧を取得
                var customers = await _service.GetCustomersAsync(Year, "00000");
                Customers = new ObservableCollection<OperationMeiItem>(customers);

                SelectedCustomer = null;
                ExpenseRates.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"得意先の取得に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 🌟 得意先選択が変更された時に自動フックするメソッド
        partial void OnSelectedCustomerChanged(OperationMeiItem? value)
        {
            if (value == null)
            {
                ExpenseRates.Clear();
                HtmlRenderRequested?.Invoke(this, "<html><body></body></html>");
                return;
            }

            // 1. 経費率データを読み込む
            _ = LoadExpenseRatesAsync(value.NameCode);

            // 2. プレビューHTMLを生成して表示する
            _ = LoadPreviewHtmlAsync(value.NameCode, value.AbbreviationName);
        }

        // =========================================================
        // 🌟 予算表のプレビューHTMLを生成・送信するメソッド
        // =========================================================
        private async Task LoadPreviewHtmlAsync(string customerCode, string customerName)
        {
            try
            {
                IsLoading = true;
                await Task.Delay(50);

                string FormatMonthlyLog(MonthlyBudgetData? data)
                {
                    if (data == null)
                    {
                        return "null";
                    }

                    return
                        $"AccountCode={data.AccountCode}, " +
                        $"04={data.Month04}, 05={data.Month05}, 06={data.Month06}, " +
                        $"07={data.Month07}, 08={data.Month08}, 09={data.Month09}, " +
                        $"10={data.Month10}, 11={data.Month11}, 12={data.Month12}, " +
                        $"01={data.Month01}, 02={data.Month02}, 03={data.Month03}";
                }

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[ShopExpenseBudgetViewModel.LoadPreviewHtmlAsync] Start " +
                    $"Year={Year}, CompanyCode=00000, CustomerCode={customerCode}, CustomerName={customerName}");

                // 1. DBから該当得意先の月次予算データを取得
                using var conn = await BudgetManagement.Common.Database.DbConnectionFactory.CreateAndOpenAsync();
                var repo = new ShopExpenseBudgetRepository();

                // SH0210 など通常の月別予算データを取得
                var rawBudgets = (await repo.GetMonthlyBudgetDataAsync(conn, Year, "00000", customerCode)).ToList();

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[ShopExpenseBudgetViewModel.LoadPreviewHtmlAsync] GetMonthlyBudgetDataAsync completed. " +
                    $"Count={rawBudgets.Count}");

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[SH0210 Check Before Calculation] " +
                    $"{FormatMonthlyLog(rawBudgets.FirstOrDefault(x => x.AccountCode == "SH0210"))}");

                // FY350_13相当：SH0220 バックス固定を取得して追加
                var backsFixedBudget = await repo.GetBacksFixedBudgetDataAsync(conn, Year, "00000", customerCode);
                rawBudgets.Add(backsFixedBudget);

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[SH0220 Check From GetBacksFixedBudgetDataAsync] " +
                    $"{FormatMonthlyLog(backsFixedBudget)}");

                // 算出行をC#側で計算
                var calculationService = new ShopExpenseBudgetCalculationService();
                var calculationResult = calculationService.Apply(rawBudgets);

                var dbBudgets = calculationResult.Items.ToList();

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[ShopExpenseBudgetViewModel.LoadPreviewHtmlAsync] Calculation completed. " +
                    $"Count={dbBudgets.Count}");

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[SH0220 Check After Calculation] " +
                    $"{FormatMonthlyLog(dbBudgets.FirstOrDefault(x => x.AccountCode == "SH0220"))}");

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[SH0230 Check After Calculation] " +
                    $"{FormatMonthlyLog(dbBudgets.FirstOrDefault(x => x.AccountCode == "SH0230"))}");

                foreach (var warning in calculationResult.Warnings)
                {
                    BudgetManagement.Common.Helper.LogHelper.Debug(
                        $"[ShopExpenseBudgetCalculationService Warning] {warning}");
                }

                var budgetDict = dbBudgets
                    .GroupBy(x => x.AccountCode)
                    .ToDictionary(g => g.Key, g => g.First());

                string templatePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Templates",
                    "ShopExpenseBudgetTemplate.xlsx");

                StringBuilder tableRows = new StringBuilder();

                using (var workbook = new XLWorkbook(templatePath))
                {
                    var worksheet = workbook.Worksheet("ショップ経費予算");

                    string GetMergedCellText(IXLWorksheet sheet, int rowNumber, int columnNumber)
                    {
                        var cell = sheet.Cell(rowNumber, columnNumber);

                        if (cell.IsMerged())
                        {
                            return cell.MergedRange()
                                       .FirstCell()
                                       .GetString()
                                       .Trim();
                        }

                        return cell.GetString().Trim();
                    }

                    string BuildAccountName(string h, string i, string j)
                    {
                        if (!string.IsNullOrWhiteSpace(j)) return j;
                        if (!string.IsNullOrWhiteSpace(i)) return i;
                        return h;
                    }

                    int FindRowByAccountCode(string accountCode)
                    {
                        var found = worksheet.Column(7)
                            .CellsUsed()
                            .FirstOrDefault(c =>
                                GetMergedCellText(worksheet, c.Address.RowNumber, 7) == accountCode);

                        return found?.Address.RowNumber ?? int.MaxValue;
                    }

                    string ResolveLookupCode(
                        int row,
                        string accountCode,
                        string accountName,
                        int limitProfitRow,
                        int storeProfitRow)
                    {
                        if (!string.IsNullOrWhiteSpace(accountCode))
                        {
                            return accountCode;
                        }

                        if (accountName == "人件費計")
                        {
                            return ShopExpenseBudgetDisplayKeys.PersonnelTotal;
                        }

                        if (accountName == "計")
                        {
                            if (row < limitProfitRow)
                            {
                                return ShopExpenseBudgetDisplayKeys.VariableExpenseTotal;
                            }

                            if (row < storeProfitRow)
                            {
                                return ShopExpenseBudgetDisplayKeys.FixedExpenseTotal;
                            }
                        }

                        return string.Empty;
                    }

                    int limitProfitRow = FindRowByAccountCode("SH0130");
                    int storeProfitRow = FindRowByAccountCode("SH0200");

                    int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 5;

                    for (int row = 5; row <= lastRow; row++)
                    {
                        string accountCode = GetMergedCellText(worksheet, row, 7);

                        string accountNameH = GetMergedCellText(worksheet, row, 8);
                        string accountNameI = GetMergedCellText(worksheet, row, 9);
                        string accountNameJ = GetMergedCellText(worksheet, row, 10);

                        string categoryName = accountNameH;
                        string accountName = BuildAccountName(accountNameH, accountNameI, accountNameJ);

                        if (string.IsNullOrWhiteSpace(accountCode) &&
                            string.IsNullOrWhiteSpace(accountName))
                        {
                            continue;
                        }

                        string lookupCode = ResolveLookupCode(
                            row,
                            accountCode,
                            accountName,
                            limitProfitRow,
                            storeProfitRow);

                        budgetDict.TryGetValue(lookupCode, out var monthData);

                        decimal m4 = monthData?.Month04 ?? 0m;
                        decimal m5 = monthData?.Month05 ?? 0m;
                        decimal m6 = monthData?.Month06 ?? 0m;
                        decimal m7 = monthData?.Month07 ?? 0m;
                        decimal m8 = monthData?.Month08 ?? 0m;
                        decimal m9 = monthData?.Month09 ?? 0m;

                        decimal m10 = monthData?.Month10 ?? 0m;
                        decimal m11 = monthData?.Month11 ?? 0m;
                        decimal m12 = monthData?.Month12 ?? 0m;
                        decimal m1 = monthData?.Month01 ?? 0m;
                        decimal m2 = monthData?.Month02 ?? 0m;
                        decimal m3 = monthData?.Month03 ?? 0m;

                        decimal firstHalf = m4 + m5 + m6 + m7 + m8 + m9;
                        decimal secondHalf = m10 + m11 + m12 + m1 + m2 + m3;
                        decimal yearTotal = firstHalf + secondHalf;

                        bool isTotalRow =
                            accountName == "計" ||
                            accountName == "人件費計" ||
                            accountCode == "SH0070" ||
                            accountCode == "SH0130" ||
                            accountCode == "SH0140" ||
                            accountCode == "SH0200" ||
                            accountCode == "SH0230" ||
                            accountCode == "SH0240" ||
                            accountCode == "SH0250" ||
                            accountCode == "SH0260" ||
                            accountCode == "SH0270";

                        string RenderCell(decimal val, bool isSubtotal = false)
                        {
                            string bgClass = isSubtotal ? " bg-gray font-bold" : "";

                            if (val == 0 && string.IsNullOrWhiteSpace(accountCode) && !isTotalRow)
                            {
                                return $"<td class='text-end align-middle{bgClass}'></td>";
                            }

                            return $"<td class='text-end align-middle{bgClass}'>{val:#,##0}</td>";
                        }

                        string displayAccountCode = accountCode;

                        tableRows.Append("<tr>");
                        tableRows.Append($"<td class='align-middle'>{System.Net.WebUtility.HtmlEncode(displayAccountCode)}</td>");

                        bool showCategorySeparately =
                            categoryName == "変動費" ||
                            categoryName == "固定費";

                        if (showCategorySeparately)
                        {
                            tableRows.Append($"<td class='align-middle category-cell'>{System.Net.WebUtility.HtmlEncode(categoryName)}</td>");
                            tableRows.Append($"<td class='align-middle'>{System.Net.WebUtility.HtmlEncode(accountName)}</td>");
                        }
                        else
                        {
                            tableRows.Append($"<td class='align-middle' colspan='2'>{System.Net.WebUtility.HtmlEncode(accountName)}</td>");
                        }

                        tableRows.Append(RenderCell(yearTotal, true));
                        tableRows.Append(RenderCell(firstHalf, true));

                        tableRows.Append(RenderCell(m4, isTotalRow));
                        tableRows.Append(RenderCell(m5, isTotalRow));
                        tableRows.Append(RenderCell(m6, isTotalRow));
                        tableRows.Append(RenderCell(m7, isTotalRow));
                        tableRows.Append(RenderCell(m8, isTotalRow));
                        tableRows.Append(RenderCell(m9, isTotalRow));

                        tableRows.Append(RenderCell(secondHalf, true));

                        tableRows.Append(RenderCell(m10, isTotalRow));
                        tableRows.Append(RenderCell(m11, isTotalRow));
                        tableRows.Append(RenderCell(m12, isTotalRow));
                        tableRows.Append(RenderCell(m1, isTotalRow));
                        tableRows.Append(RenderCell(m2, isTotalRow));
                        tableRows.Append(RenderCell(m3, isTotalRow));

                        tableRows.Append("</tr>");
                    }
                }

                string html = $@"
<!DOCTYPE html>
<html lang='ja'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Segoe UI', Meiryo, sans-serif; padding: 20px; color: #333; background-color: #F3F4F6; }}
        h2 {{ color: #2563EB; border-bottom: 2px solid #E5E7EB; padding-bottom: 10px; font-size: 18px; }}
        .content-box {{ background-color: white; padding: 20px; border-radius: 8px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow-x: auto; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 13px; white-space: nowrap; }}
        th, td {{ border: 1px solid #D1D5DB; padding: 8px; }}
        th {{ background-color: #F8FAFC; font-weight: bold; color: #475569; text-align: center; }}
        .text-end {{ text-align: right; }}
        .align-middle {{ vertical-align: middle; }}
        .font-bold {{ font-weight: bold; }}
        .bg-gray {{ background-color: #F9FAFB; }}
        .category-cell {{
            background-color: #FFE0B2;
            text-align: center;
            font-weight: bold;
            color: #374151;
        }}
    </style>
</head>
<body>
    <div class='content-box'>
        <h2>{System.Net.WebUtility.HtmlEncode(customerName)} のショップ経費予算表</h2>
        <table>
            <thead>
                <tr>
                    <th>費目コード</th>
                    <th>区分</th>
                    <th>科目名</th>
                    <th>年計</th>
                    <th>上期計</th>
                    <th>4月</th><th>5月</th><th>6月</th><th>7月</th><th>8月</th><th>9月</th>
                    <th>下期計</th>
                    <th>10月</th><th>11月</th><th>12月</th><th>1月</th><th>2月</th><th>3月</th>
                </tr>
            </thead>
            <tbody>
                {tableRows}
            </tbody>
        </table>
    </div>
</body>
</html>";

                HtmlRenderRequested?.Invoke(this, html);

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[ShopExpenseBudgetViewModel.LoadPreviewHtmlAsync] Completed " +
                    $"Year={Year}, CompanyCode=00000, CustomerCode={customerCode}");

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                BudgetManagement.Common.Helper.LogHelper.Error(
                    ex,
                    $"[ShopExpenseBudgetViewModel.LoadPreviewHtmlAsync] プレビュー表示エラー " +
                    $"Year={Year}, CompanyCode=00000, CustomerCode={customerCode}, CustomerName={customerName}");

                BudgetManagement.Common.Helper.LogHelper.Debug(
                    $"[ShopExpenseBudgetViewModel.LoadPreviewHtmlAsync] Exception Detail\n{ex}");

                MessageBox.Show(
                    $"プレビューの表示に失敗しました。\n{ex.Message}\n\n詳細はログを確認してください。",
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ---------------------------------------------------------
        // 経費率を取得して画面のコレクションに詰め替えるメソッド
        // ---------------------------------------------------------
        private async Task LoadExpenseRatesAsync(string customerCode)
        {
            try
            {
                IsLoading = true;

                using var conn = await BudgetManagement.Common.Database.DbConnectionFactory.CreateAndOpenAsync();
                var repo = new ShopExpenseBudgetRepository();
                var rawRates = await repo.GetShopExpenseRatesAsync(conn, Year, "00000", customerCode);

                ExpenseRates.Clear();

                if (rawRates != null)
                {
                    foreach (var row in rawRates)
                    {
                        ExpenseRates.Add(new ExpenseRateItem
                        {
                            DivisionName = row.ryaku ?? string.Empty,
                            Rate = row.expense_rate != null ? Convert.ToDecimal(row.expense_rate) : 0m
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"経費率の取得に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
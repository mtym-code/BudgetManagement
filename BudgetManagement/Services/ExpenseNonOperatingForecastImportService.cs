using BudgetManagement.Common.Constants;
using BudgetManagement.Common.Database;
using BudgetManagement.Common.Exceptions;
using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using BudgetManagement.Repositories;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    public class ForecastImportResult
    {
        public List<ForecastData> Details { get; set; } = new();
        public string Year { get; set; } = string.Empty;
        public string CompanyInfo { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
    }

    public class ExpenseNonOperatingForecastImportService
    {
        private readonly ExpenseNonOperatingForecastRepository _repository;

        public ExpenseNonOperatingForecastImportService(ExpenseNonOperatingForecastRepository repository)
        {
            _repository = repository;
        }

        public async Task<ForecastImportResult> ReadExcelAsync(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("ファイルが見つかりません。");

            return await Task.Run(() =>
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var workbook = new XLWorkbook(stream);

                    var result = new ForecastImportResult();

                    // 👇 ================= ここから追加 ================= 👇
                    if (workbook.TryGetWorksheet("Settings", out var settingsSheet))
                    {
                        // Settingsシートから年度・会社・月を読み取って、箱（プロパティ）に詰める
                        result.Year = settingsSheet.Cell("B5").GetString()?.Trim() ?? "";
                        result.CompanyInfo = settingsSheet.Cell("B6").GetString()?.Trim() ?? "";
                        result.Month = settingsSheet.Cell("B7").GetString()?.Trim() ?? "";
                    }
                    else
                    {
                        throw new ImportValidationException("Settingsシートが見つかりません。");
                    }
                    // 👆 ================= ここまで追加 ================= 👆

                    // 以下、既存の「経費営業外見通し」シートの読み取り処理...
                    if (!workbook.TryGetWorksheet("経費営業外見通し", out var dataSheet))
                    {
                        throw new ImportValidationException("「経費営業外見通し」シートが見つかりません。");
                    }

                    int lastRow = dataSheet.LastRowUsed()?.RowNumber() ?? 5;

                    // 👇 ReadExcelAsync 内のループ部分
                    for (int r = 5; r <= lastRow; r++)
                    {
                        string rawDeptString = dataSheet.Cell(r, "A").GetString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrEmpty(rawDeptString)) continue;

                        string deptCode = rawDeptString;
                        if (rawDeptString.StartsWith("[") && rawDeptString.Contains("]"))
                        {
                            int endIndex = rawDeptString.IndexOf("]");
                            deptCode = rawDeptString.Substring(1, endIndex - 1);
                        }

                        var data = new ForecastData
                        {
                            DeptCode = deptCode,
                            Keihi1 = GetDecimal(dataSheet.Cell(r, "I")), // I列
                            Keihi2 = GetDecimal(dataSheet.Cell(r, "J")), // J列
                            Syueki = GetDecimal(dataSheet.Cell(r, "L")), // L列
                        };
                        result.Details.Add(data);
                    }

                    return result;
                }
                catch (ImportValidationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new ImportValidationException("ファイルを開くことができませんでした。\n対応していないファイル形式か、\nデータが破損している可能性があります。");
                }
            });
        }

        private bool IsSameVersion(string v1, string v2)
        {
            if (string.IsNullOrWhiteSpace(v1) || string.IsNullOrWhiteSpace(v2)) return v1 == v2;
            var parts1 = v1.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToList();
            var parts2 = v2.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToList();
            int maxLen = Math.Max(parts1.Count, parts2.Count);
            while (parts1.Count < maxLen) parts1.Add(0);
            while (parts2.Count < maxLen) parts2.Add(0);
            for (int i = 0; i < maxLen; i++) { if (parts1[i] != parts2[i]) return false; }
            return true;
        }

        private decimal? GetDecimal(IXLCell cell)
        {
            if (cell.IsEmpty()) return null; // セルが空っぽの場合は NULL を返す
            if (!cell.TryGetValue<decimal>(out decimal amount)) throw new ImportValidationException($"セル {cell.Address.ColumnLetter}{cell.Address.RowNumber} に不正な値が含まれています。");
            return amount;
        }

        public async Task SaveImportDataAsync(string year, string companyCode, string targetMonth, List<ForecastData> importData, string userId)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                string programId = BusinessCodes.FY360;
                string yearMonth = $"{year}{targetMonth.PadLeft(2, '0')}";

                foreach (var detail in importData)
                {
                    // 値が存在する（0円含む）場合のみデータベースに登録（UPSERT）する
                    if (detail.Keihi1.HasValue) await _repository.UpsertForecastDataAsync(conn, year, detail.DeptCode, "711000", yearMonth, targetMonth, detail.Keihi1.Value, programId, false, userId);
                    if (detail.Keihi2.HasValue) await _repository.UpsertForecastDataAsync(conn, year, detail.DeptCode, "762000", yearMonth, targetMonth, detail.Keihi2.Value, programId, false, userId);
                    if (detail.Syueki.HasValue) await _repository.UpsertForecastDataAsync(conn, year, detail.DeptCode, "811000", yearMonth, targetMonth, detail.Syueki.Value, programId, false, userId);
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                LogHelper.Error(ex, "DB更新処理中にシステムエラーが発生しました。");
                throw;
            }
        }
    }
}
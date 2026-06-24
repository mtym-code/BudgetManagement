using BudgetManagement.Common.Constants;
using BudgetManagement.Common.Database;
using BudgetManagement.Common.Exceptions;
using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using BudgetManagement.Repositories;
using ClosedXML.Excel;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    public class DepartmentBudgetImportService
    {
        private readonly DepartmentBudgetRepository _repository;

        public DepartmentBudgetImportService(DepartmentBudgetRepository repository)
        {
            _repository = repository;
        }

        // =========================================================
        // ① Excelを読み込み、データを返す（DB更新はしない）
        // =========================================================
        public async Task<ImportResult> ReadExcelAsync(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("ファイルが見つかりません。");

            return await Task.Run(() =>
            {
                // =========================================================
                // ★修正: 変なファイルでシステムが落ちないように全体を try-catch で安全に包む
                // =========================================================
                try
                {
                    // FileShare.ReadWrite を指定し、Excelで開いたままでも読み込めるようにする
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var workbook = new XLWorkbook(stream);
                    var settingsSheet = workbook.Worksheet("Settings");

                    // 1. バージョンチェック (文字列の完全一致ではなく、バージョン値として比較)
                    var currentVersion = settingsSheet.Cell("B2").GetString();
                    var expectedVersion = ConfigurationHelper.Get($"ExcelTemplateVersions:{BusinessCodes.FY330}");

                    if (!IsSameVersion(currentVersion, expectedVersion))
                    {
                        throw new ImportValidationException($"バージョンが一致しません。(Excel:{currentVersion} 期待値:{expectedVersion})");
                    }

                    // 2. 業務コードチェック
                    var businessCode = settingsSheet.Cell("B3").GetString();
                    if (businessCode != BusinessCodes.FY330)
                    {
                        throw new ImportValidationException("取り込み対象のExcelファイル（部門別経費予算 FY330）ではありません。");
                    }

                    // 3. 共通データの取得
                    var fiscalYearStr = settingsSheet.Cell("B5").GetString();
                    var companyInfo = settingsSheet.Cell("B6").GetString();
                    var sectionInfo = settingsSheet.Cell("B7").GetString();

                    var companyCode = companyInfo.PadRight(5).Substring(0, 5);
                    var departmentCode = sectionInfo.PadRight(5).Substring(0, 5);

                    if (!int.TryParse(fiscalYearStr, out int fiscalYear))
                        throw new ImportValidationException("年度が正しく設定されていません。");

                    if (!workbook.TryGetWorksheet("部門別経費予算", out var dataSheet))
                        throw new ImportValidationException("「部門別経費予算」シートが見つかりません。");

                    var result = new ImportResult
                    {
                        Year = fiscalYearStr,
                        CompanyCode = companyCode,
                        SectionCode = departmentCode,
                        CompanyInfo = companyInfo,
                        SectionInfo = sectionInfo
                    };

                    // 1. ClosedXMLのリストに頼らず、物理的な行番号(5行目～)で強制ループする
                    int lastRow = dataSheet.LastRowUsed()?.RowNumber() ?? 5;

                    for (int r = 5; r <= lastRow; r++)
                    {
                        var row = dataSheet.Row(r);

                        // 2. セルの値を最も安全な方法で取得し、空白を除去、大文字に変換
                        string flagValue = row.Cell("F").GetString()?.Trim().ToUpper() ?? string.Empty;

                        // 3. 半角の「Y」だけでなく、入力ミスの全角「Ｙ」にも対応させる
                        if (flagValue != "Y" && flagValue != "Ｙ")
                        {
                            continue;
                        }

                        var data = new ImportedBudgetData
                        {
                            AccountCode = row.Cell("H").GetString()?.Trim() ?? string.Empty,
                            SubAccountCode = row.Cell("I").GetString()?.Trim() ?? string.Empty,
                            Month04 = GetDecimal(row.Cell(13)),
                            Month05 = GetDecimal(row.Cell(14)),
                            Month06 = GetDecimal(row.Cell(15)),
                            Month07 = GetDecimal(row.Cell(16)),
                            Month08 = GetDecimal(row.Cell(17)),
                            Month09 = GetDecimal(row.Cell(18)),
                            Month10 = GetDecimal(row.Cell(20)),
                            Month11 = GetDecimal(row.Cell(21)),
                            Month12 = GetDecimal(row.Cell(22)),
                            Month01 = GetDecimal(row.Cell(23)),
                            Month02 = GetDecimal(row.Cell(24)),
                            Month03 = GetDecimal(row.Cell(25))
                        };
                        result.Details.Add(data);
                    }

                    return result;
                }
                catch (ImportValidationException)
                {
                    // 既に自分で投げた「業務エラー（バージョン違い等）」はそのまま上に通す
                    throw;
                }
                catch (Exception)
                {
                    // ★追加: ライブラリがパニックを起こした場合は、システムエラーにせず安全な警告に変換する
                    throw new ImportValidationException("ファイルを開くことができませんでした。\n対応していないファイル形式か、\nデータが破損している可能性があります。");
                }
            });
        }

        // =========================================================
        // ★新規追加: バージョンを賢く比較する処理
        // ("1", "1.0", "1.00", "1.0.0" をすべて同一とみなす)
        // =========================================================
        private bool IsSameVersion(string v1, string v2)
        {
            if (string.IsNullOrWhiteSpace(v1) || string.IsNullOrWhiteSpace(v2)) return v1 == v2;

            // "." で分割し、それぞれ数値に変換する (変換できない文字は0とする)
            var parts1 = v1.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToList();
            var parts2 = v2.Split('.').Select(s => int.TryParse(s, out int n) ? n : 0).ToList();

            // 桁数が多い方に合わせる
            int maxLen = Math.Max(parts1.Count, parts2.Count);

            // 足りない桁を0で埋める (例: "1" を [1,0,0] に変換)
            while (parts1.Count < maxLen) parts1.Add(0);
            while (parts2.Count < maxLen) parts2.Add(0);

            // 各桁を比較
            for (int i = 0; i < maxLen; i++)
            {
                if (parts1[i] != parts2[i]) return false;
            }

            return true;
        }

        private decimal GetDecimal(IXLCell cell)
        {
            if (!cell.TryGetValue<decimal>(out decimal amount))
            {
                if (cell.IsEmpty()) return 0;
                throw new ImportValidationException($"セル {cell.Address.ColumnLetter}{cell.Address.RowNumber} に不正な値が含まれています。");
            }
            return amount;
        }

        // =========================================================
        // ② 保持しているデータをDBへ更新（確定ボタン用）
        // =========================================================
        public async Task SaveImportDataAsync(ImportResult importData, string userId)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                int fiscalYear = int.Parse(importData.Year);

                foreach (var detail in importData.Details)
                {
                    var monthParams = new[]
                    {
                        new { Month = 4, YearOffset = 0, Amount = detail.Month04 },
                        new { Month = 5, YearOffset = 0, Amount = detail.Month05 },
                        new { Month = 6, YearOffset = 0, Amount = detail.Month06 },
                        new { Month = 7, YearOffset = 0, Amount = detail.Month07 },
                        new { Month = 8, YearOffset = 0, Amount = detail.Month08 },
                        new { Month = 9, YearOffset = 0, Amount = detail.Month09 },
                        new { Month = 10, YearOffset = 0, Amount = detail.Month10 },
                        new { Month = 11, YearOffset = 0, Amount = detail.Month11 },
                        new { Month = 12, YearOffset = 0, Amount = detail.Month12 },
                        new { Month = 1, YearOffset = 1, Amount = detail.Month01 },
                        new { Month = 2, YearOffset = 1, Amount = detail.Month02 },
                        new { Month = 3, YearOffset = 1, Amount = detail.Month03 }
                    };

                    foreach (var m in monthParams)
                    {
                        var param = new
                        {
                            FiscalYear = importData.Year,
                            OrgCode = importData.SectionCode,
                            AccountExpenseType = "0",
                            AccountCode = detail.AccountCode,
                            SubAccountCode = detail.SubAccountCode,
                            YearMonth = $"{fiscalYear + m.YearOffset}{m.Month:D2}",
                            FiscalMonth = m.Month,
                            BudgetAmount = m.Amount,
                            DeleteFlag = false,
                            CreatedProgram = BusinessCodes.FY330,
                            UpdatedProgram = BusinessCodes.FY330,
                            CreatedBy = userId,
                            UpdatedBy = userId
                        };
                        await _repository.UpsertAsync(conn, param);
                    }
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
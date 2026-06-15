using System.Collections.Generic;

namespace BudgetManagement.Models
{
    /// <summary>
    /// Excelから読み込んだ1行分の予算明細データ
    /// </summary>
    public class ImportedBudgetData
    {
        public string AccountCode { get; set; } = string.Empty;
        public string SubAccountCode { get; set; } = string.Empty;

        public decimal Month04 { get; set; }
        public decimal Month05 { get; set; }
        public decimal Month06 { get; set; }
        public decimal Month07 { get; set; }
        public decimal Month08 { get; set; }
        public decimal Month09 { get; set; }
        public decimal Month10 { get; set; }
        public decimal Month11 { get; set; }
        public decimal Month12 { get; set; }
        public decimal Month01 { get; set; }
        public decimal Month02 { get; set; }
        public decimal Month03 { get; set; }
    }

    /// <summary>
    /// Excel読み込み結果全体（ヘッダ情報＋明細）
    /// </summary>
    public class ImportResult
    {
        public string Year { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        
        // 画面表示用の結合文字列（例: "OOOOO : キング"）
        public string CompanyInfo { get; set; } = string.Empty;
        public string SectionInfo { get; set; } = string.Empty;

        public List<ImportedBudgetData> Details { get; set; } = new();
    }
}
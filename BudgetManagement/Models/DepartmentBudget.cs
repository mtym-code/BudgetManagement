using System;

namespace BudgetManagement.Models
{
    // ① 元々あった部門予算データのメインモデル
    public class DepartmentBudget
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
        public int FiscalYear { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ② 追加：会社情報を格納するモデル
    public class Company
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
    }

    // ③ 追加：課の情報を格納するモデル
    public class SectionInfo
    {
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;

        // コンボボックスなどで表示する用のプロパティ
        public string DisplayName => $"{SectionCode} : {SectionName}";
    }

    public class ExcelBudgetData
    {
        public string AccountCode { get; set; } = string.Empty;
        public string SubAccountCode { get; set; } = string.Empty;
        public decimal BudgetApr { get; set; }
        public decimal BudgetMay { get; set; }
        public decimal BudgetJun { get; set; }
        public decimal BudgetJul { get; set; }
        public decimal BudgetAug { get; set; }
        public decimal BudgetSep { get; set; }
        public decimal BudgetOct { get; set; }
        public decimal BudgetNov { get; set; }
        public decimal BudgetDec { get; set; }
        public decimal BudgetJan { get; set; }
        public decimal BudgetFeb { get; set; }
        public decimal BudgetMar { get; set; }
    }
}
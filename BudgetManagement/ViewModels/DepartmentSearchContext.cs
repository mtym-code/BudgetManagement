using BudgetManagement.Models;
using System.Collections.Generic;

namespace BudgetManagement.ViewModels
{
    /// <summary>
    /// 検索に関する状態（入力履歴や内部の対応表）をカプセル化して保持するクラスです。
    /// </summary>
    public class DepartmentSearchContext
    {
        public string LastSearchedYear { get; set; } = string.Empty;
        public Dictionary<string, (string Code, string Name)> SectionCompanyMap { get; set; } = new();
        public List<SectionInfo> AllSections { get; set; } = new();

        public void Clear()
        {
            LastSearchedYear = string.Empty;
            SectionCompanyMap.Clear();
            AllSections.Clear();
        }
    }
}
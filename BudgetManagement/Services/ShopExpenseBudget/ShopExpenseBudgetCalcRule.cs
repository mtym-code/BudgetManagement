namespace BudgetManagement.Services.ShopExpenseBudget
{
    /// <summary>
    /// ショップ経費予算専用の計算ルール。
    /// 他の予算画面では使用しない。
    /// </summary>
    internal sealed class ShopExpenseBudgetCalcRule
    {
        public string TargetAccountCode { get; }
        public string TargetName { get; }
        public IReadOnlyList<ShopExpenseBudgetCalcTerm> Terms { get; }

        public ShopExpenseBudgetCalcRule(
            string targetAccountCode,
            string targetName,
            params ShopExpenseBudgetCalcTerm[] terms)
        {
            TargetAccountCode = targetAccountCode;
            TargetName = targetName;
            Terms = terms;
        }
    }

    /// <summary>
    /// ショップ経費予算専用の計算式明細。
    /// Sign は +1 または -1 を想定。
    /// </summary>
    internal sealed class ShopExpenseBudgetCalcTerm
    {
        public string SourceAccountCode { get; }
        public decimal Sign { get; }

        public ShopExpenseBudgetCalcTerm(string sourceAccountCode, decimal sign)
        {
            SourceAccountCode = sourceAccountCode;
            Sign = sign;
        }
    }
}
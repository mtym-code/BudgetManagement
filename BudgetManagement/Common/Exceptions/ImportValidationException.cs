using System;

namespace BudgetManagement.Common.Exceptions
{
    /// <summary>
    /// 取り込み処理におけるデータの入力チェックや業務ルールの違反を表す例外
    /// </summary>
    public class ImportValidationException : Exception
    {
        public ImportValidationException(string message) : base(message)
        {
        }
    }
}
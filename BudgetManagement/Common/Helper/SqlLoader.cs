using System.IO;

namespace BudgetManagement.Common.Helper
{
    public static class SqlLoader
    {
        public static string Load(string fileName)
        {
            return File.ReadAllText(Path.Combine("Sql", fileName));
        }
    }
}

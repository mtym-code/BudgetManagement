using Microsoft.Extensions.Configuration;

namespace BudgetManagement.Common.Helper
{
    public static class ConfigurationHelper
    {

        private static IConfiguration _config = null!;

        public static void Initialize()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string? Get(string key)
        {
            return _config[key];
        }

    }
}

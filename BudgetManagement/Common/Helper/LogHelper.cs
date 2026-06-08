using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace BudgetManagement.Common.Helper
{
    public static class LogHelper
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                // 通常ログ
                .WriteTo.File(
                    "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )

                // エラー専用ログ
                .WriteTo.File(
                    "logs/error-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )

                .CreateLogger();
        }

        public static void Information(string message)
        {
            Log.Information(message);
        }

        public static void Error(Exception ex, string message = "")
        {
            Log.Error(ex, message);
        }

        public static void Warning(string message)
        {
            Log.Warning(message);
        }
    }
}

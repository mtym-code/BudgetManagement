using Serilog;
using Serilog.Events;
using System;
using System.IO;

using System.Collections.Generic;
using System.Text;

namespace BudgetManagement.Common.Helper
{
    public static class LogHelper
    {
        public static void Initialize()
        {
            // アプリの実行ファイルの場所を取得
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 絶対パスを構築（bin/Debug/netX.X-windows/logs/ への出力になりますが、パスを見失うのを防ぎます）
            string logPath = Path.Combine(baseDir, "logs", "log-.txt");
            string errorLogPath = Path.Combine(baseDir, "logs", "error-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug() // ★Debugレベルに下げる

                // 通常ログ
                .WriteTo.File(
                    logPath, // ★相対パス("logs/log-.txt")から絶対パス変数に変更
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )

                // エラー専用ログ
                .WriteTo.File(
                    errorLogPath, // ★相対パスから絶対パス変数に変更
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    outputTemplate:
                        "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
        }

        // =========================================================
        // ★追加: Debugレベルのログ出力メソッド
        // =========================================================
        public static void Debug(string message)
        {
            Log.Debug(message);
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
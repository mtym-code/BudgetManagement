using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using BudgetManagement.Repositories;
using BudgetManagement.Services;
using BudgetManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text;
using System.Windows;

namespace BudgetManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Shift-jis対応
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 設定読み込み
            ConfigurationHelper.Initialize();

            // ログ初期化
            LogHelper.Initialize();
            LogHelper.Information("アプリケーション起動");

            // DI追加
            var services = new ServiceCollection();

            // Repository
            services.AddTransient<UserRepository>();
            // 👇 ここに新設したRepositoryを登録
            services.AddTransient<DepartmentBudgetRepository>();

            // Service
            services.AddTransient<UserService>();
            services.AddTransient<DepartmentBudgetService>();
            services.AddTransient<DepartmentBudgetImportService>(); // 追加

            // ViewModel
            services.AddTransient<SampleViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DepartmentBudgetViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
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
            services.AddTransient<DepartmentBudgetRepository>();
            // 👇 追加：経費営業外見通し用 Repository
            services.AddTransient<BudgetManagement.Repositories.ExpenseNonOperatingForecastRepository>();

            // Service
            services.AddTransient<UserService>();
            services.AddTransient<DepartmentBudgetService>();
            services.AddTransient<DepartmentBudgetImportService>();
            
            // 👇 追加：経費営業外見通し用 Service群
            services.AddTransient<BudgetManagement.Services.ExpenseNonOperatingForecastService>();
            services.AddTransient<BudgetManagement.Services.ExpenseNonOperatingForecastExcelService>();
            services.AddTransient<BudgetManagement.Services.ExpenseNonOperatingForecastHtmlService>();
            services.AddTransient<BudgetManagement.Services.ExpenseNonOperatingForecastImportService>();

            // ViewModel
            services.AddTransient<SampleViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DepartmentBudgetViewModel>();
            // 👇 追加：経費営業外見通し用 ViewModel
            services.AddTransient<BudgetManagement.ViewModels.ExpenseNonOperatingForecastViewModel>();
            
            services.AddTransient<DepartmentBudgetExcelService>();
            services.AddTransient<DepartmentBudgetHtmlService>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
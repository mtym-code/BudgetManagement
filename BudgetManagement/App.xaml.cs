using BudgetManagement.Common.Helper;
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
        public static IServiceProvider ServiceProvider { get; private set; }
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

            // Service
            services.AddTransient<UserService>();

            // ViewModel
            services.AddTransient<SampleViewModel>();

            ServiceProvider = services.BuildServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}

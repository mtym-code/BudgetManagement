using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using BudgetManagement.ViewModel; // 名前空間をプロジェクトに統一

namespace BudgetManagement.Views
{
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();

            var vm = App.Services.GetService<LoginViewModel>();
            DataContext = vm;

            vm.LoginSucceeded += OnLoginSucceeded;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var vm = (LoginViewModel)DataContext;
            vm.Password = PasswordBox.Password;
        }

        private void OnLoginSucceeded()
        {
            // 【変更】元々あった UserCsvImport への遷移処理は、ここからメニュー画面へと移植しました。
            // ログイン成功時はまずハブとなるメニュー画面を開きます。
            this.NavigationService.Navigate(new MenuPage());
        }
    }
}
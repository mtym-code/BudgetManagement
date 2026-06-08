using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using BudgetManagement.ViewModels;

namespace BudgetManagement.Views
{
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();

            var vm = App.ServiceProvider.GetService<LoginViewModel>();
            DataContext = vm;

            if (vm != null)
            {
                vm.LoginSucceeded += OnLoginSucceeded;
            }
        }

        // ⭕ async を追加して非同期メソッドに変更
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var vm = (LoginViewModel)DataContext;
            if (vm != null)
            {
                // 1. パスワードを確実にViewModelへ格納
                vm.Password = LoginPassword.Password;
                
                // 2. ログイン処理を直接安全に呼び出す
                await vm.LoginAsync();
            }
        }

        private void OnLoginSucceeded()
        {
            this.NavigationService.Navigate(new MenuPage());
        }
    }
}
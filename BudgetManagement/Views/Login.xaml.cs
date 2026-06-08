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

            var vm = App.ServiceProvider.GetRequiredService<LoginViewModel>();
            DataContext = vm;

            if (vm != null)
            {
                vm.LoginSucceeded += OnLoginSucceeded;

                // ⭕ 【新規追加】ViewModelからのメッセージ表示イベントを購読
                vm.ShowMessage += OnShowMessage;
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            // ⭕ 最初に一度だけ sender を Button 型として変数に格納します
            var btn = sender as Button;

            if (btn != null)
            {
                btn.IsEnabled = false; // 無効化
            }

            try
            {
                var vm = DataContext as LoginViewModel;
                if (vm != null)
                {
                    vm.Password = LoginPassword.Password;
                    await vm.LoginAsync();
                }
            }
            finally
            {
                // ⭕ 上で宣言した btn をそのまま使って有効化します
                if (btn != null)
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private void OnLoginSucceeded()
        {
            this.NavigationService.Navigate(new MenuPage());
        }

        // ⭕ 【新規追加】メッセージを表示する処理
        private void OnShowMessage(string title, string message, MessageBoxImage icon)
        {
            // 現在このログイン画面（Page）をホストしている親ウィンドウを確実に取得する
            var ownerWindow = Window.GetWindow(this);

            if (ownerWindow != null)
            {
                // 親ウィンドウの中央にロックして表示
                MessageBox.Show(ownerWindow, message, title, MessageBoxButton.OK, icon);
            }
            else
            {
                // 万が一ウィンドウが取得できなかった場合のフォールバック
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
        }
    }
}
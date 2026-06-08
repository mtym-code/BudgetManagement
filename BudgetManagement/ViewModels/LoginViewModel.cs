using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace BudgetManagement.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        public event Action LoginSucceeded;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [RelayCommand]
        private void Login()
        {
            // TODO: 今後、UserServiceなどを注入して実際の認証ロジックを実装します
            // 現在は、IDとパスワードが入力されていれば成功とみなします
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                LoginSucceeded?.Invoke();
            }
            else
            {
                System.Windows.MessageBox.Show("ユーザーIDまたはパスワードを入力してください。", "入力エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
// 【修正】以下の1行だけにしてください（末尾の s が重要です）
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

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var vm = (LoginViewModel)DataContext;
            if (vm != null)
            {
                vm.Password = LoginPassword.Password;
                // Commandがバインドされている場合は直接Executeを呼ばなくても動作しますが
                // もし動かない場合はここで vm.LoginCommand.Execute(null); を呼んでください
            }
        }

        private void OnLoginSucceeded()
        {
            this.NavigationService.Navigate(new MenuPage());
        }
    }
}
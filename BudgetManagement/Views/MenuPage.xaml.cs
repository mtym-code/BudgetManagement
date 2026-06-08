using Microsoft.Extensions.DependencyInjection; // App.Services を使うために追加
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BudgetManagement.Views
{
    public partial class MenuPage : Page
    {
        public MenuPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 【新規追加】Login.xaml.cs から引っ越してきた遷移ロジック
        /// メニューのボタンが押されたときに、元々の機能画面（UserCsvImport）を起動します。
        /// </summary>
        private void UserCsvImport_Click(object sender, RoutedEventArgs e)
        {
            // 元々 Login.xaml.cs に書いてあったコードをそのままここで実行します
            this.NavigationService.Navigate(
                App.Services.GetService<UserCsvImport>()
            );
        }

        private void TabBudget_Click(object sender, MouseButtonEventArgs e)
        {
            BudgetPanel.Visibility = Visibility.Visible;
            MaintenancePanel.Visibility = Visibility.Collapsed;

            TabBudget.Background = Brushes.White;
            TabBudget.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));

            TabMaintenance.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
            TabMaintenance.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        }

        private void TabMaintenance_Click(object sender, MouseButtonEventArgs e)
        {
            BudgetPanel.Visibility = Visibility.Collapsed;
            MaintenancePanel.Visibility = Visibility.Visible;

            TabMaintenance.Background = Brushes.White;
            TabMaintenance.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 102, 241));

            TabBudget.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
            TabBudget.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        }
    }
}
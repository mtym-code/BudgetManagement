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
        /// ユーザーCSVインポートの代わりとして、既存の SampleView（ユーザー一覧）を起動します
        /// </summary>
        private void UserCsvImport_Click(object sender, RoutedEventArgs e)
        {
            // DIコンテナの登録状況に左右されないよう、シンプルに新しくインスタンスを生成して遷移します
            this.NavigationService.Navigate(new SampleView());
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
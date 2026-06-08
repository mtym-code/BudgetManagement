using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BudgetManagement.Views
{
    public partial class MainMenuPage : Page
    {
        public MainMenuPage()
        {
            InitializeComponent();
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

        // ⭕ 【新規追加】部門別経費予算画面へ遷移
        private void DepartmentBudget_Click(object sender, RoutedEventArgs e)
        {
            // この NavigationService は、親の Frame に対して遷移を指示します
            this.NavigationService.Navigate(new DepartmentBudgetPage());
        }

        private void UserCsvImport_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new SampleView());
        }
    }
}
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection; // ⭕これが必要
using BudgetManagement.ViewModels; // ⭕これが必要

namespace BudgetManagement.Views
{
    public partial class DepartmentBudgetPage : Page
    {
        public DepartmentBudgetPage()
        {
            InitializeComponent();

            // 🔴 最重要：ここでViewModelを画面にセット（接続）していますか？
            this.DataContext = App.ServiceProvider.GetRequiredService<DepartmentBudgetViewModel>();
        }
    }
}
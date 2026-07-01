using BudgetManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace BudgetManagement.Views
{
    /// <summary>
    /// BudgetOperationMasterPage.xaml の相互作用ロジック
    /// </summary>
    public partial class BudgetOperationMasterPage : Page
    {
        public BudgetOperationMasterPage()
        {
            // XAMLで定義されたUIを構築・初期化する（必須）
            InitializeComponent();

            // DIコンテナからViewModelを取得し、DataContext（画面のデータ元）に設定する
            this.DataContext = App.ServiceProvider.GetRequiredService<BudgetOperationMasterViewModel>();
        }
    }
}
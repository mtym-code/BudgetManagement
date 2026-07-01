using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows.Input;

namespace BudgetManagement.Views
{
    public partial class MenuPage : Page
    {
        public MenuPage()
        {
            InitializeComponent();

            // ⭕【新規追加】起動時は「メインメニュー画面（MainMenuPage）」をカード内に表示する
            MainFrame.Navigate(new MainMenuPage());
        }

        // =========================================================
        // 最上部ヘッダーのクリックでPopup（ドロップダウン）を開く
        // =========================================================
        private void NavBudget_Click(object sender, MouseButtonEventArgs e)
        {
            NavBudgetPopup.IsOpen = true;
        }

        private void NavMaintenance_Click(object sender, MouseButtonEventArgs e)
        {
            NavMaintenancePopup.IsOpen = true;
        }

        // =========================================================
        // ドロップダウンからの画面遷移イベント
        // =========================================================

        /// <summary>
        /// ドロップダウンメニュー内の「部門経費予算」リンククリック時
        /// </summary>
        private void DepartmentBudgetLink_Click(object sender, MouseButtonEventArgs e)
        {
            // ドロップダウンを閉じる
            NavBudgetPopup.IsOpen = false;
            
            // ⭕ 中央のカードの中身（MainFrame）を部門経費予算画面に差し替える
            MainFrame.Navigate(new DepartmentBudgetPage());
        }

        /// <summary>
        /// ドロップダウンメニュー内の「ユーザーCSVインポート」リンククリック時
        /// </summary>
        private void UserCsvImportLink_Click(object sender, MouseButtonEventArgs e)
        {
            // ドロップダウンを閉じる
            NavBudgetPopup.IsOpen = false;
            
            // ⭕ 中央のカードの中身（MainFrame）を差し替える
            MainFrame.Navigate(new SampleView());
        }

        // ▼ 新規追加：経費営業外見通しリンククリック時 ▼
        /// <summary>
        /// ドロップダウンメニュー内の「経費・営業外」リンククリック時
        /// </summary>
        private void ExpenseNonOperatingForecastLink_Click(object sender, MouseButtonEventArgs e)
        {
            // ドロップダウンを閉じる
            NavBudgetPopup.IsOpen = false;
            
            // 中央のカードの中身を 経費営業外見通し画面 に差し替える
            MainFrame.Navigate(new ExpenseNonOperatingForecastPage());
        }

        private void BudgetOperationMasterLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            NavMaintenancePopup.IsOpen = false; // ポップアップを閉じる

            // Frame (MainFrame) の中身を予算運用マスタ画面に切り替える
            var page = App.ServiceProvider.GetRequiredService<BudgetOperationMasterPage>();
            MainFrame.Navigate(page);
        }
        // 👇 新規追加：ショップ別経費リンククリック時
        private void ShopExpenseBudgetLink_Click(object sender, MouseButtonEventArgs e)
        {
            // ドロップダウンを閉じる
            NavBudgetPopup.IsOpen = false;

            // 中央のカードの中身を ショップ経費予算画面 に差し替える
            MainFrame.Navigate(new ShopExpenseBudgetPage());
        }
    }
}
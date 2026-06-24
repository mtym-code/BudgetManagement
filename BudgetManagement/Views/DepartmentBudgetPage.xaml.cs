using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using BudgetManagement.ViewModels;

namespace BudgetManagement.Views
{
    public partial class DepartmentBudgetPage : Page
    {
        public DepartmentBudgetPage()
        {
            InitializeComponent();

            var vm = App.ServiceProvider.GetRequiredService<DepartmentBudgetViewModel>();
            this.DataContext = vm;

            // ViewModelのイベントをキャッチする設定
            vm.HtmlRenderRequested += ViewModel_HtmlRenderRequested;
            // ★追加: DB検索側のイベントもキャッチする
            vm.DbHtmlRenderRequested += ViewModel_DbHtmlRenderRequested;

            InitializeWebViewAsync();

            // ※LoadingPopupを廃止したため、Popupの位置調整（UpdatePopupPosition）の処理は削除しました
        }

        private void SectionComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || !comboBox.IsEditable) return;

            var textBox = e.OriginalSource as TextBox;
            if (textBox == null || !textBox.IsFocused) return;

            var vm = this.DataContext as DepartmentBudgetViewModel;

            if (vm != null && !vm.IsValidSectionName(comboBox.Text))
            {
                if (comboBox.SelectedIndex != -1)
                {
                    comboBox.SelectedIndex = -1;
                }

                if (comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = false;
                }
            }

            if (!e.Changes.Any(c => c.AddedLength > 0)) return;

            if (comboBox.Text.Length > 5)
            {
                if (vm != null && vm.IsValidSectionName(comboBox.Text)) return;

                int caret = textBox.SelectionStart;
                comboBox.Text = comboBox.Text.Substring(0, 5);
                textBox.SelectionStart = Math.Min(caret, 5);
            }
        }

        private void SectionComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || !comboBox.IsEditable) return;

            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape) return;

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textBox != null && textBox.IsFocused)
            {
                if (!string.IsNullOrEmpty(textBox.Text) && !comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
        }

        private void SectionComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textBox != null && textBox.IsFocused)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (textBox.SelectionLength > 0)
                    {
                        textBox.SelectionLength = 0;
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                }, DispatcherPriority.Input);
            }
        }

        private async void InitializeWebViewAsync()
        {
            // ★修正: Excelプレビュー用とDB検索用の両方のエンジンを準備する
            await PreviewWebView.EnsureCoreWebView2Async(null);
            await SearchPreviewWebView.EnsureCoreWebView2Async(null);
        }

        // =========================================================
        // Excel取り込みプレビュー用の処理
        // =========================================================
        private async void ViewModel_HtmlRenderRequested(object? sender, string htmlContent)
        {
            if (PreviewWebView != null)
            {
                // ★修正：WebView2の準備がまだなら、ここで準備完了を待つ
                await PreviewWebView.EnsureCoreWebView2Async(null);

                if (string.IsNullOrEmpty(htmlContent))
                    PreviewWebView.NavigateToString("<html><body></body></html>");
                else
                    PreviewWebView.NavigateToString(htmlContent);
            }
        }

        // =========================================================
        // DB検索プレビュー用の処理
        // =========================================================
        private async void ViewModel_DbHtmlRenderRequested(object? sender, string htmlContent)
        {
            if (SearchPreviewWebView != null)
            {
                // ★修正：WebView2の準備がまだなら、ここで準備完了を待つ
                await SearchPreviewWebView.EnsureCoreWebView2Async(null);

                if (string.IsNullOrEmpty(htmlContent))
                    SearchPreviewWebView.NavigateToString("<html><body></body></html>");
                else
                    SearchPreviewWebView.NavigateToString(htmlContent);
            }
        }

        // ページを閉じるときにイベントの繋がりを解除する（メモリ対策）
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is DepartmentBudgetViewModel vm)
            {
                vm.HtmlRenderRequested -= ViewModel_HtmlRenderRequested;
                // ★追加: DB検索用のイベントも解除する
                vm.DbHtmlRenderRequested -= ViewModel_DbHtmlRenderRequested;
            }

            PreviewWebView?.Dispose();
            // ★追加: DB検索用のWebView2も破棄する
            SearchPreviewWebView?.Dispose();
        }
    }
}
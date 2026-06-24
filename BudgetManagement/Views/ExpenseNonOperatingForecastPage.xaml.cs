using BudgetManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace BudgetManagement.Views
{
    public partial class ExpenseNonOperatingForecastPage : Page
    {
        public ExpenseNonOperatingForecastPage()
        {
            InitializeComponent();

            // ViewModelをセットアップする
            var vm = App.ServiceProvider.GetRequiredService<ExpenseNonOperatingForecastViewModel>();
            this.DataContext = vm;

            // ViewModelのイベントをキャッチする設定
            vm.DbHtmlRenderRequested += ViewModel_DbHtmlRenderRequested;
            vm.ImportHtmlRenderRequested += ViewModel_ImportHtmlRenderRequested;

            // WebView2の初期化
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            // 照会タブ用と取込タブ用の両方のWebViewを初期化します
            await SearchPreviewWebView.EnsureCoreWebView2Async(null);
            await PreviewWebView.EnsureCoreWebView2Async(null);
        }

        private async void ViewModel_DbHtmlRenderRequested(object? sender, string htmlContent)
        {
            if (SearchPreviewWebView != null)
            {
                await SearchPreviewWebView.EnsureCoreWebView2Async(null);
                SearchPreviewWebView.NavigateToString(string.IsNullOrEmpty(htmlContent) ? "<html><body></body></html>" : htmlContent);
            }
        }

        private async void ViewModel_ImportHtmlRenderRequested(object? sender, string htmlContent)
        {
            if (PreviewWebView != null)
            {
                await PreviewWebView.EnsureCoreWebView2Async(null);
                PreviewWebView.NavigateToString(string.IsNullOrEmpty(htmlContent) ? "<html><body></body></html>" : htmlContent);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ExpenseNonOperatingForecastViewModel vm)
            {
                vm.DbHtmlRenderRequested -= ViewModel_DbHtmlRenderRequested;
                vm.ImportHtmlRenderRequested -= ViewModel_ImportHtmlRenderRequested;
            }
            SearchPreviewWebView?.Dispose();
            PreviewWebView?.Dispose();
        }
    }
}
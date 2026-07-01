using BudgetManagement.ViewModels;
using System;
using System.Windows.Controls;

namespace BudgetManagement.Views
{
    public partial class ShopExpenseBudgetPage : Page
    {
        private readonly ShopExpenseBudgetViewModel _viewModel;

        public ShopExpenseBudgetPage()
        {
            InitializeComponent();

            // DIコンテナからViewModelを取得してDataContextにセット
_viewModel = App.ServiceProvider.GetService(typeof(ShopExpenseBudgetViewModel)) as ShopExpenseBudgetViewModel 
             ?? throw new InvalidOperationException("ViewModelの取得に失敗しました。");

            DataContext = _viewModel;

            // WebView2の起動とイベント紐づけ
            _ = InitializeWebViewAsync();
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            // WebView2エンジンの初期化を待機
            await PreviewWebView.EnsureCoreWebView2Async(null);

            // ViewModelから「HTMLを描画して！」という指示（イベント）が来たら、WebViewに流し込む設定
            _viewModel.HtmlRenderRequested += (sender, htmlContent) =>
            {
                if (PreviewWebView.CoreWebView2 != null && !string.IsNullOrEmpty(htmlContent))
                {
                    PreviewWebView.NavigateToString(htmlContent);
                }
            };
        }
    }
}
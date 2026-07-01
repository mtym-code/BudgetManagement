using BudgetManagement.Repositories;
using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public class InputScreenItem
    {
        public string ScreenCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public partial class BudgetOperationMasterViewModel : ObservableObject
    {
        private readonly BudgetOperationMasterService _service;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading = false;
        public bool IsNotLoading => !IsLoading;

        // 業務プルダウンの選択肢
        public ObservableCollection<InputScreenItem> InputScreens { get; } = new ObservableCollection<InputScreenItem>
        {
            new InputScreenItem { ScreenCode = "322", DisplayName = "商品課別営業予算" },
            new InputScreenItem { ScreenCode = "323", DisplayName = "商品課別営業見通し" },
            new InputScreenItem { ScreenCode = "330", DisplayName = "部門別経費予算" },
            new InputScreenItem { ScreenCode = "340", DisplayName = "得意先別営業予算" },
            new InputScreenItem { ScreenCode = "360", DisplayName = "経費営業外見通し" }
        };

        [ObservableProperty] private InputScreenItem? _selectedInputScreen;
        [ObservableProperty] private string _year = string.Empty;

        [ObservableProperty] private ObservableCollection<OperationMeiItem> _companies = new();
        [ObservableProperty] private OperationMeiItem? _selectedCompany;

        public ObservableCollection<string> Months { get; } = new ObservableCollection<string>
        {
            "01月", "02月", "03月", "04月", "05月", "06月",
            "07月", "08月", "09月", "10月", "11月", "12月"
        };
        [ObservableProperty] private string? _selectedMonth;

        [ObservableProperty] private bool _isMonthEnabled = false;
        [ObservableProperty] private ObservableCollection<BudgetOperationItem> _operationItems = new();
        [ObservableProperty] private bool _isUpdateEnabled = false;

        public BudgetOperationMasterViewModel(BudgetOperationMasterService service)
        {
            _service = service;
            _ = LoadMastersAsync();
        }

        private async Task LoadMastersAsync()
        {
            try
            {
                IsLoading = true;
                Companies = new ObservableCollection<OperationMeiItem>(await _service.GetCompaniesAsync());
                Year = DateTime.Now.Year.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"マスタの取得に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================================
        // 🌟 自動検索トリガー（各項目が変更されたときに自動で呼ばれるメソッド群）
        // =========================================================================

        partial void OnSelectedInputScreenChanged(InputScreenItem? value)
        {
            if (value == null) return;

            // 月の活性制御
            IsMonthEnabled = value.ScreenCode == "360" || value.ScreenCode == "323";
            if (!IsMonthEnabled) SelectedMonth = null;

            _ = TrySearchAsync();
        }

        partial void OnSelectedCompanyChanged(OperationMeiItem? value)
        {
            _ = TrySearchAsync();
        }

        partial void OnSelectedMonthChanged(string? value)
        {
            _ = TrySearchAsync();
        }

        partial void OnYearChanged(string value)
        {
            // 年度は4文字入力し終わった段階で検索を試みる
            if (value?.Length == 4)
            {
                _ = TrySearchAsync();
            }
            else
            {
                // 4桁未満の場合は未確定なので表をクリア
                OperationItems.Clear();
                IsUpdateEnabled = false;
            }
        }

        // =========================================================================
        // 🌟 自動検索処理の本体
        // =========================================================================
        private async Task TrySearchAsync()
        {
            // 必須項目が揃っていなければ表を空にして終了（警告ポップアップは出さない）
            if (SelectedInputScreen == null || string.IsNullOrWhiteSpace(Year) || Year.Length != 4 || SelectedCompany == null)
            {
                OperationItems.Clear();
                IsUpdateEnabled = false;
                return;
            }
            // 🌟 追加：月が必須の画面（見通し系）で、月が未選択の場合は表を空にして終了
            if (IsMonthEnabled && string.IsNullOrEmpty(SelectedMonth))
            {
                OperationItems.Clear();
                IsUpdateEnabled = false;
                return;
            }

            try
            {
                IsLoading = true;
                await Task.Delay(300); // 連続入力を防ぐための少しのディレイ（Debounce的効果）

                string month = SelectedMonth ?? string.Empty;

                var rawData = await _service.GetOperationMasterListAsync(Year, SelectedInputScreen.ScreenCode, SelectedCompany.NameCode, string.Empty, string.Empty, month);

                OperationItems = new ObservableCollection<BudgetOperationItem>(rawData);
                IsUpdateEnabled = OperationItems.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの検索に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // =========================================================================
        // 更新ボタン処理
        // =========================================================================
        [RelayCommand]
        private async Task UpdateAsync()
        {
            if (OperationItems.Count == 0) return;

            var result = MessageBox.Show("表示されている内容でデータベースを更新します。よろしいですか？", "更新確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                string userId = BudgetManagement.Common.SessionManager.UserId;
                await _service.UpdateOperationMasterAsync(Year, SelectedInputScreen!.ScreenCode, OperationItems, userId);

                MessageBox.Show("データを更新しました。", "更新完了", MessageBoxButton.OK, MessageBoxImage.Information);

                // 🌟 更新完了後も新しいメソッドで再検索をかける
                await TrySearchAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新処理中にエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
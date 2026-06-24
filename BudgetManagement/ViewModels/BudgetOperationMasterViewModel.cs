using BudgetManagement.Repositories;
using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        [ObservableProperty] private bool _isLoading = false;
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

        [ObservableProperty] private ObservableCollection<OperationMeiItem> _departments = new();
        [ObservableProperty] private OperationMeiItem? _selectedDepartment;

        [ObservableProperty] private ObservableCollection<OperationMeiItem> _sections = new();
        [ObservableProperty] private OperationMeiItem? _selectedSection;

        public ObservableCollection<string> Months { get; } = new ObservableCollection<string>
        {
            "01月", "02月", "03月", "04月", "05月", "06月",
            "07月", "08月", "09月", "10月", "11月", "12月"
        };
        [ObservableProperty] private string? _selectedMonth;

        // 🌟 Converterを使わず、個別のboolプロパティでラジオボタンを管理
        [ObservableProperty] private bool _isInputFilterAll = true;
        [ObservableProperty] private bool _isInputFilterDone = false;
        [ObservableProperty] private bool _isInputFilterNotDone = false;

        [ObservableProperty] private bool _isSendFilterAll = true;
        [ObservableProperty] private bool _isSendFilterDone = false;
        [ObservableProperty] private bool _isSendFilterNotDone = false;

        // 活性・非活性制御フラグ
        [ObservableProperty] private bool _isDepartmentEnabled = true;
        [ObservableProperty] private bool _isSectionEnabled = true;
        [ObservableProperty] private bool _isMonthEnabled = false;

        [ObservableProperty] private ObservableCollection<BudgetOperationItem> _operationItems = new();
        [ObservableProperty] private bool _isUpdateEnabled = false;

        public BudgetOperationMasterViewModel(BudgetOperationMasterService service)
        {
            _service = service;
            LoadMastersAsync().ConfigureAwait(false); // 画面起動時にマスタ取得
        }

        partial void OnSelectedInputScreenChanged(InputScreenItem? value)
        {
            if (value == null) return;

            IsDepartmentEnabled = value.ScreenCode != "360";
            IsSectionEnabled = value.ScreenCode != "360";
            IsMonthEnabled = value.ScreenCode == "360" || value.ScreenCode == "323";

            if (!IsDepartmentEnabled) SelectedDepartment = null;
            if (!IsSectionEnabled) SelectedSection = null;
            if (!IsMonthEnabled) SelectedMonth = null;

            OperationItems.Clear();
            IsUpdateEnabled = false;
        }

        private async Task LoadMastersAsync()
        {
            try
            {
                IsLoading = true;
                Companies = new ObservableCollection<OperationMeiItem>(await _service.GetCompaniesAsync());
                Departments = new ObservableCollection<OperationMeiItem>(await _service.GetDepartmentsAsync());
                Sections = new ObservableCollection<OperationMeiItem>(await _service.GetSectionsAsync());
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

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (SelectedInputScreen == null || string.IsNullOrWhiteSpace(Year) || SelectedCompany == null)
            {
                MessageBox.Show("「入力画面」「年度」「会社」は必須入力です。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;
                await Task.Delay(300);

                string deptCode = SelectedDepartment?.NameCode ?? string.Empty;
                string sectionCode = SelectedSection?.NameCode ?? string.Empty;
                string month = SelectedMonth ?? string.Empty;

                var rawData = await _service.GetOperationMasterListAsync(Year, SelectedInputScreen.ScreenCode, SelectedCompany.NameCode, deptCode, sectionCode, month);

                // 🌟 boolプロパティを使ってリストを絞り込み
                var filtered = rawData.Where(d =>
                    (IsInputFilterAll ||
                    (IsInputFilterDone && d.IsStaffInputCompleted) ||
                    (IsInputFilterNotDone && !d.IsStaffInputCompleted))
                    &&
                    (IsSendFilterAll ||
                    (IsSendFilterDone && d.IsSendFlag) ||
                    (IsSendFilterNotDone && !d.IsSendFlag))
                ).ToList();

                OperationItems = new ObservableCollection<BudgetOperationItem>(filtered);
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

        [RelayCommand]
        private async Task UpdateAsync()
        {
            if (OperationItems.Count == 0) return;

            var result = MessageBox.Show("表示されている内容でデータベースを更新します。よろしいですか？", "更新確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;

                // ※SessionManagerが存在しない/使わない場合は、適宜固定文字列や他のユーザー情報変数に置き換えてください
                string userId = BudgetManagement.Common.SessionManager.UserId;
                await _service.UpdateOperationMasterAsync(Year, SelectedInputScreen!.ScreenCode, OperationItems, userId);

                MessageBox.Show("データを更新しました。", "更新完了", MessageBoxButton.OK, MessageBoxImage.Information);

                await SearchAsync();
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
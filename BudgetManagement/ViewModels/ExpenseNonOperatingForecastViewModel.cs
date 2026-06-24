using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Views;
using BudgetManagement.Common;
using BudgetManagement.Common.Constants;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public partial class ExpenseNonOperatingForecastViewModel : ObservableObject
    {
        private readonly ExpenseNonOperatingForecastService _service;
        private readonly ExpenseNonOperatingForecastImportService _importService;
        private readonly ExpenseNonOperatingForecastExcelService _excelService;
        private readonly ExpenseNonOperatingForecastHtmlService _htmlService;

        [ObservableProperty] private string _year = string.Empty;
        [ObservableProperty] private Company? _selectedCompany;
        [ObservableProperty] private SystemYearMonth? _selectedMonth;

        [ObservableProperty] private ObservableCollection<Company> _companies = new();
        [ObservableProperty] private ObservableCollection<SystemYearMonth> _months = new();

        [ObservableProperty] private string? _statusText;
        [ObservableProperty] private Visibility _dbPreviewPlaceholderVisibility = Visibility.Visible;
        [ObservableProperty] private bool _isPreviewLoading = false;
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private bool _isCompleteButtonEnabled = false;
        [ObservableProperty] private bool _isCommitButtonEnabled = false;

        [ObservableProperty] private string _importYearInfo = string.Empty;
        [ObservableProperty] private string _importCompanyInfo = string.Empty;
        [ObservableProperty] private string _importMonthInfo = string.Empty;
        [ObservableProperty] private string? _importStatusText;

        private ForecastImportResult? _currentImportResult;

        public event EventHandler<string>? DbHtmlRenderRequested;
        public event EventHandler<string>? ImportHtmlRenderRequested;

        public bool IsNotLoading => !IsLoading && !IsPreviewLoading;

        public ExpenseNonOperatingForecastViewModel(
            ExpenseNonOperatingForecastService service,
            ExpenseNonOperatingForecastImportService importService,
            ExpenseNonOperatingForecastExcelService excelService,
            ExpenseNonOperatingForecastHtmlService htmlService)
        {
            _service = service;
            _importService = importService;
            _excelService = excelService;
            _htmlService = htmlService;
        }

        [RelayCommand]
        private async Task HandleYearLostFocusAsync()
        {
            if (string.IsNullOrWhiteSpace(Year) || Year.Length != 4) return;

            try
            {
                IsLoading = true;
                Companies = new ObservableCollection<Company>(await _service.GetCompaniesByYearAsync(Year));
                Months = new ObservableCollection<SystemYearMonth>(await _service.GetSystemYearMonthsAsync(Year));

                SelectedCompany = null;
                SelectedMonth = null;
                StatusText = null;
                DbHtmlRenderRequested?.Invoke(this, string.Empty);
                DbPreviewPlaceholderVisibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"データの取得に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task HandleSelectionChangedAsync()
        {
            if (string.IsNullOrWhiteSpace(Year) || SelectedCompany == null || SelectedMonth == null) return;

            DbPreviewPlaceholderVisibility = Visibility.Visible;
            IsPreviewLoading = true;
            await Task.Delay(50);

            try
            {
                int targetMonthNum = int.Parse(SelectedMonth.Month);
                bool isCompleted = await _service.GetStaffInputCompleteFlagAsync(Year, SelectedCompany.CompanyCode, targetMonthNum);
                StatusText = isCompleted ? StatusConstants.Completed : StatusConstants.Uncompleted;

                var dataList = (await _service.GetForecastDataAsync(Year, SelectedCompany.CompanyCode, SelectedMonth.Month)).ToList();

                bool hasData = dataList.Any(d =>
                    d.Keihi1.HasValue || d.Keihi2.HasValue || d.Syueki.HasValue ||
                    d.Keihimitoshi1.HasValue || d.Keihimitoshi2.HasValue || d.Syuekimitoshi.HasValue);

                IsCompleteButtonEnabled = !isCompleted && hasData;

                string html = await Task.Run(() =>
                {
                    if (!hasData) return "<html><body style='padding:40px; text-align:center;'><h3>データがありません</h3></body></html>";

                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"Preview_{Guid.NewGuid()}.xlsx");
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ExpenseNonOperatingForecastTemplate.xlsx");

                    try
                    {
                        _excelService.CreatePreviewExcel(templatePath, tempFilePath, dataList);
                        return _htmlService.GenerateHtmlFromExcel(tempFilePath);
                    }
                    finally { if (File.Exists(tempFilePath)) { try { File.Delete(tempFilePath); } catch { } } }
                });

                DbHtmlRenderRequested?.Invoke(this, html);
                DbPreviewPlaceholderVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"データの取得に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsPreviewLoading = false;
            }
        }

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            if (string.IsNullOrEmpty(Year) || SelectedCompany == null || SelectedMonth == null)
            {
                CustomMessageBox.Show("年度、会社、月を選択してから実行してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "見通し出力ファイルの保存先を選択してください",
                Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                FileName = $"経費営業外見通し_{Year}_{SelectedCompany.CompanyCode}_{SelectedMonth.Month}月_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            string outputPath = dialog.FileName;
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "ExpenseNonOperatingForecastTemplate.xlsx");

            await ExecuteWithLoadingAsync(
                action: async () =>
                {
                    var dataList = await _service.GetForecastDataAsync(Year, SelectedCompany.CompanyCode, SelectedMonth.Month);
                    await Task.Run(() =>
                    {
                        _excelService.ExportExcel(templatePath, outputPath, Year, SelectedCompany.CompanyCode, SelectedCompany.CompanyName, SelectedMonth.Month, dataList);
                    });
                },
                successMessage: $"Excelを出力しました。\n保存先: {outputPath}",
                errorMessage: "出力中にエラーが発生しました:\n{0}"
            );
        }

        [RelayCommand]
        private async Task SelectAndImportFileAsync()
        {
            var dialog = new OpenFileDialog { Filter = "Excel Files|*.xlsx;*.xlsm", Title = "経費営業外見通しファイルを選択" };

            if (dialog.ShowDialog() == true)
            {
                bool isSuccess = false;
                string? validationError = null;
                string? systemError = null;

                try
                {
                    IsLoading = true;

                    _currentImportResult = await _importService.ReadExcelAsync(dialog.FileName);

                    ImportYearInfo = _currentImportResult.Year;
                    ImportCompanyInfo = _currentImportResult.CompanyInfo;
                    ImportMonthInfo = _currentImportResult.Month;

                    string importCompanyCode = ImportCompanyInfo.Split('：', ':')[0].Trim();
                    string importMonthNumStr = ImportMonthInfo.Replace("月", "").Trim();
                    int targetMonthNum = int.Parse(importMonthNumStr);

                    bool isCompleted = await _service.GetStaffInputCompleteFlagAsync(ImportYearInfo, importCompanyCode, targetMonthNum);
                    ImportStatusText = isCompleted ? StatusConstants.Completed : StatusConstants.Uncompleted;

                    string html = await Task.Run(() => _htmlService.GenerateHtmlFromExcel(dialog.FileName));
                    ImportHtmlRenderRequested?.Invoke(this, html);

                    if (isCompleted)
                    {
                        IsCommitButtonEnabled = false;
                        validationError = "入力完了のデータは更新できません。";
                    }
                    else
                    {
                        IsCommitButtonEnabled = true;
                        isSuccess = true;
                    }
                }
                catch (BudgetManagement.Common.Exceptions.ImportValidationException ex)
                {
                    validationError = ex.Message;
                }
                catch (Exception ex)
                {
                    systemError = ex.Message;
                }
                finally
                {
                    IsLoading = false;
                    await Task.Delay(100);
                }

                if (isSuccess)
                {
                    CustomMessageBox.Show("ファイルの読み込みが完了しました。\n内容を確認後、「DBへ保存」ボタンを押して登録してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (validationError != null)
                {
                    CustomMessageBox.Show(validationError, "データ確認のお願い", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (systemError != null)
                {
                    CustomMessageBox.Show($"システムの読み込みに失敗しました。\n{systemError}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ClearImportFields()
        {
            _currentImportResult = null;
            ImportYearInfo = string.Empty;
            ImportCompanyInfo = string.Empty;
            ImportMonthInfo = string.Empty;
            ImportStatusText = null;
            IsCommitButtonEnabled = false;
            ImportHtmlRenderRequested?.Invoke(this, string.Empty);
        }

        [RelayCommand]
        private async Task CommitImportAsync()
        {
            if (_currentImportResult == null || string.IsNullOrWhiteSpace(Year) || SelectedCompany == null || SelectedMonth == null) return;

            var result = CustomMessageBox.Show("表示されている内容でデータベースに保存します。よろしいですか？", "更新確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            await ExecuteWithLoadingAsync(
                action: async () =>
                {
                    await _importService.SaveImportDataAsync(Year, SelectedCompany.CompanyCode, SelectedMonth.Month, _currentImportResult.Details, SessionManager.UserId);
                },
                successMessage: "データベースへ保存しました。",
                errorMessage: "更新処理中にエラーが発生しました。\n{0}",
                successTitle: "保存完了",
                errorTitle: "エラー"
            );

            ClearImportFields();
            await HandleSelectionChangedAsync();
        }

        [RelayCommand]
        private async Task CompleteInputAsync()
        {
            if (string.IsNullOrWhiteSpace(Year) || SelectedCompany == null || SelectedMonth == null) return;

            var result = CustomMessageBox.Show("現在の内容で「入力完了（確定）」とします。\nよろしいですか？", "最終確定の確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            await ExecuteWithLoadingAsync(
                action: async () =>
                {
                    int targetMonthNum = int.Parse(SelectedMonth.Month);
                    await _service.UpsertStaffInputStatusAsync(Year, SelectedCompany.CompanyCode, targetMonthNum, SessionManager.UserId, false, SessionManager.UserId);
                },
                successMessage: "入力を完了し、データを確定しました。",
                errorMessage: "確定処理中にエラーが発生しました:\n{0}",
                successTitle: "確定完了"
            );

            await HandleSelectionChangedAsync();
        }

        private async Task ExecuteWithLoadingAsync(Func<Task> action, string successMessage, string errorMessage, string successTitle = "完了", string errorTitle = "エラー")
        {
            IsLoading = true;
            await Task.Delay(500);

            bool isSuccess = false;
            string errorText = string.Empty;

            try
            {
                await action();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                errorText = ex.Message;
            }
            finally
            {
                IsLoading = false;
                await Task.Delay(100);
            }

            if (isSuccess && !string.IsNullOrEmpty(successMessage))
            {
                CustomMessageBox.Show(successMessage, successTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (!isSuccess)
            {
                CustomMessageBox.Show(string.Format(errorMessage, errorText), errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
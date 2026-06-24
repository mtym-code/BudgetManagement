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
    public partial class DepartmentBudgetViewModel : ObservableObject
    {
        private const string DummyDeleteFlag = "false";

        private readonly DepartmentBudgetService _service;
        private readonly DepartmentBudgetImportService _importService;
        private readonly DepartmentBudgetExcelService _excelService;
        private readonly DepartmentBudgetHtmlService _htmlService;

        private readonly DepartmentSearchContext _searchContext = new();

        // =========================================================
        // ★修正：防護壁が不要になったため、すべてスッキリとした自動生成プロパティに戻しました！
        // =========================================================
        [ObservableProperty] private bool _isCurrentBudgetTabSelected = true;
        [ObservableProperty] private bool _isImportTabSelected = false;

        [ObservableProperty] private SectionInfo? _selectedSection;
        [ObservableProperty] private string _sectionSearchText = string.Empty;

        [ObservableProperty] private string _year = string.Empty;
        [ObservableProperty] private string _companyCode = string.Empty;
        [ObservableProperty] private string _companyName = string.Empty;
        [ObservableProperty] private string? _statusText;
        [ObservableProperty] private ObservableCollection<SectionInfo> _sections = new();
        [ObservableProperty] private Visibility _dbPreviewPlaceholderVisibility = Visibility.Visible;
        [ObservableProperty] private bool _isPreviewLoading = false;
        [ObservableProperty] private bool _isCompleteButtonEnabled = false;
        [ObservableProperty] private bool _isCommitButtonEnabled = false;
        [ObservableProperty] private string _importYearInfo = string.Empty;
        [ObservableProperty] private string _importCompanyInfo = string.Empty;
        [ObservableProperty] private string _importSectionInfo = string.Empty;
        [ObservableProperty] private string? _importStatusText;
        [ObservableProperty] private ObservableCollection<ImportedBudgetData> _importedDataList = new();
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private Visibility _webViewVisibility = Visibility.Visible;

        public bool IsNotLoading => !IsLoading && !IsPreviewLoading;

        private ImportResult? _currentImportResult;

        public event EventHandler<string>? HtmlRenderRequested;
        public event EventHandler<string>? DbHtmlRenderRequested;

        public DepartmentBudgetViewModel(
            DepartmentBudgetService service,
            DepartmentBudgetImportService importService,
            DepartmentBudgetExcelService excelService,
            DepartmentBudgetHtmlService htmlService)
        {
            _service = service;
            _importService = importService;
            _excelService = excelService;
            _htmlService = htmlService;
        }

        partial void OnIsLoadingChanged(bool value) => UpdateWebViewVisibility();
        partial void OnIsPreviewLoadingChanged(bool value) => UpdateWebViewVisibility();

        private void UpdateWebViewVisibility() => WebViewVisibility = (IsLoading || IsPreviewLoading) ? Visibility.Hidden : Visibility.Visible;

        public bool IsValidSectionName(string name) => _searchContext.AllSections.Any(s => s.DisplayName == name);

        [RelayCommand]
        private async Task HandleYearLostFocusAsync()
        {
            if (string.IsNullOrWhiteSpace(Year) || Year == _searchContext.LastSearchedYear) return;
            _searchContext.LastSearchedYear = Year;

            try
            {
                var companyList = (await _service.GetCompaniesByYearAsync(Year)).ToList();

                _searchContext.Clear();
                CompanyCode = string.Empty;
                CompanyName = string.Empty;

                if (companyList.Any())
                {
                    foreach (var comp in companyList)
                    {
                        var sectionList = await _service.GetSectionsAsync(Year, comp.CompanyCode);
                        foreach (var section in sectionList)
                        {
                            _searchContext.AllSections.Add(section);
                            _searchContext.SectionCompanyMap[section.SectionCode] = (comp.CompanyCode, comp.CompanyName);
                        }
                    }

                    SectionSearchText = string.Empty;
                    SelectedSection = null;
                    StatusText = null;
                    Sections = new ObservableCollection<SectionInfo>(_searchContext.AllSections);
                }
                else
                {
                    CompanyName = "該当データなし";
                    Sections = new();
                    SectionSearchText = string.Empty;
                    SelectedSection = null;
                    StatusText = null;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"組織データの取得に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task HandleSectionChangedAsync()
        {
            if (SelectedSection == null) return;
            if (_searchContext.SectionCompanyMap.TryGetValue(SelectedSection.SectionCode, out var compInfo))
            {
                CompanyCode = compInfo.Code;
                CompanyName = compInfo.Name;
            }
            if (string.IsNullOrEmpty(CompanyCode)) return;

            DbPreviewPlaceholderVisibility = Visibility.Visible;
            IsPreviewLoading = true;
            await Task.Delay(50);

            try
            {
                bool isCompleted = await _service.GetManagementInputFlagsAsync(Year, CompanyCode, SelectedSection.SectionCode);
                StatusText = isCompleted ? StatusConstants.Completed : StatusConstants.Uncompleted;

                var dbData = await _service.GetBudgetDataForExcelAsync(Year, CompanyCode, SelectedSection.SectionCode);
                var dataList = dbData?.Cast<dynamic>().ToList();
                bool hasData = dataList != null && dataList.Any();

                IsCompleteButtonEnabled = !isCompleted && hasData;

                string html = await Task.Run(() =>
                {
                    if (!hasData) return "<html><body style='padding:40px; text-align:center;'><h3>データがありません</h3></body></html>";

                    string tempFilePath = Path.Combine(Path.GetTempPath(), $"Preview_{Guid.NewGuid()}.xlsx");
                    string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "DepartmentBudgetTemplate.xlsx");

                    try
                    {
                        _excelService.CreatePreviewExcel(templatePath, tempFilePath, dataList!);
                        return _htmlService.GenerateHtmlFromExcel(tempFilePath, SessionManager.OperationType, false);
                    }
                    finally { if (File.Exists(tempFilePath)) { try { File.Delete(tempFilePath); } catch { } } }
                });

                DbHtmlRenderRequested?.Invoke(this, html);
                DbPreviewPlaceholderVisibility = Visibility.Collapsed;
            }
            catch (Exception ex) { CustomMessageBox.Show($"データの取得に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { IsPreviewLoading = false; }
        }

        [RelayCommand]
        private async Task CompleteInputAsync()
        {
            if (SelectedSection == null || string.IsNullOrEmpty(CompanyCode)) return;

            var result = CustomMessageBox.Show("現在の内容で「入力完了（確定）」とします。\nよろしいですか？", "最終確定の確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            await ExecuteWithLoadingAsync(
                action: async () =>
                {
                    await _service.UpsertStaffInputStatusAsync(Year, CompanyCode, SelectedSection.SectionCode, SessionManager.UserId, DummyDeleteFlag, SessionManager.UserId, DateTime.Now, SessionManager.UserId, DateTime.Now);
                },
                successMessage: "入力を完了し、データを確定しました。",
                errorMessage: "確定処理中にエラーが発生しました:\n{0}",
                successTitle: "確定完了"
            );

            await HandleSectionChangedAsync();
        }

        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            if (string.IsNullOrEmpty(Year) || SelectedSection == null)
            {
                CustomMessageBox.Show("年度と課を選択してから実行してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "予算出力ファイルの保存先を選択してください",
                Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                FileName = $"部門別予算_{Year}_{SelectedSection.SectionCode}_{SelectedSection.SectionName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            string outputPath = dialog.FileName;
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "DepartmentBudgetTemplate.xlsx");

            await ExecuteWithLoadingAsync(
                action: async () =>
                {
                    var budgetDataList = await _service.GetBudgetDataForExcelAsync(Year, CompanyCode, SelectedSection.SectionCode);
                    string currentOperationType = SessionManager.OperationType;
                    string sectionDisplayName = SelectedSection.DisplayName;

                    await Task.Run(() =>
                    {
                        _excelService.ExportBudgetExcel(templatePath, outputPath, Year, CompanyCode, CompanyName, sectionDisplayName, currentOperationType, budgetDataList);
                    });
                },
                successMessage: $"Excelを出力しました。\n保存先: {outputPath}",
                errorMessage: "出力中にエラーが発生しました:\n{0}"
            );
        }

        [RelayCommand]
        private async Task SelectAndImportFileAsync()
        {
            var dialog = new OpenFileDialog { Filter = "Excel Files|*.xlsx;*.xlsm", Title = "部門別経費予算ファイルを選択" };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _currentImportResult = await _importService.ReadExcelAsync(dialog.FileName);

                    ImportYearInfo = _currentImportResult.Year;
                    ImportCompanyInfo = _currentImportResult.CompanyInfo;
                    ImportSectionInfo = _currentImportResult.SectionInfo;
                    ImportedDataList = new ObservableCollection<ImportedBudgetData>(_currentImportResult.Details);

                    string importCompanyCode = _currentImportResult.CompanyInfo.Split('：', ':')[0].Trim();
                    bool isCompleted = await _service.GetManagementInputFlagsAsync(_currentImportResult.Year, importCompanyCode, _currentImportResult.SectionCode);
                    ImportStatusText = isCompleted ? StatusConstants.Completed : StatusConstants.Uncompleted;

                    string html = _htmlService.GenerateHtmlFromExcel(dialog.FileName, SessionManager.OperationType, true);
                    HtmlRenderRequested?.Invoke(this, html);

                    if (isCompleted)
                    {
                        IsCommitButtonEnabled = false;
                        CustomMessageBox.Show("入力完了のデータは更新できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        IsCommitButtonEnabled = true;
                        CustomMessageBox.Show("ファイルの読み込みが完了しました。\n内容を確認後、「DBへ保存」ボタンを押して仮登録してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (BudgetManagement.Common.Exceptions.ImportValidationException ex) { CustomMessageBox.Show(ex.Message, "データ確認のお願い", MessageBoxButton.OK, MessageBoxImage.Warning); }
                catch (IOException ex) { CustomMessageBox.Show("ファイルにアクセスできません。\n" + ex.Message, "ファイル読み込みエラー", MessageBoxButton.OK, MessageBoxImage.Error); }
                catch (Exception ex) { CustomMessageBox.Show($"システムエラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        [RelayCommand]
        private async Task CommitImportAsync()
        {
            if (ImportStatusText == StatusConstants.Completed) return;
            var result = CustomMessageBox.Show("表示されている内容でデータベースに仮保存します。よろしいですか？", "更新確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            string savedYear = _currentImportResult!.Year;
            string savedSectionCode = _currentImportResult.SectionCode;

            await ExecuteWithLoadingAsync(
                action: async () => { await _importService.SaveImportDataAsync(_currentImportResult, SessionManager.UserId); },
                successMessage: "データベースへ仮保存しました。\n全体を確認して「入力完了」を行ってください。",
                errorMessage: "更新処理中にエラーが発生しました。\n{0}", successTitle: "保存完了", errorTitle: "重大なエラー"
            );

            _currentImportResult = null;
            ImportYearInfo = string.Empty; ImportCompanyInfo = string.Empty; ImportSectionInfo = string.Empty; ImportStatusText = null;
            ImportedDataList.Clear(); IsCommitButtonEnabled = false; HtmlRenderRequested?.Invoke(this, string.Empty);

            // ★保存完了後、タブを切り替えてから、取り込んだ年度と課をセットする
            IsCurrentBudgetTabSelected = true;

            Year = savedYear;
            _searchContext.LastSearchedYear = string.Empty;
            await HandleYearLostFocusAsync();

            SelectedSection = Sections.FirstOrDefault(s => s.SectionCode == savedSectionCode);
            if (SelectedSection != null) await HandleSectionChangedAsync();
        }

        [RelayCommand]
        private void ClearFields()
        {
            _searchContext.Clear();
            Sections = new(); SectionSearchText = string.Empty; SelectedSection = null; StatusText = null;
            Year = string.Empty; CompanyCode = string.Empty; CompanyName = string.Empty;
            ImportYearInfo = string.Empty; ImportCompanyInfo = string.Empty; ImportSectionInfo = string.Empty; ImportStatusText = null;
            ImportedDataList.Clear(); _currentImportResult = null;
            IsCompleteButtonEnabled = false; IsPreviewLoading = false; IsCommitButtonEnabled = false;
            DbHtmlRenderRequested?.Invoke(this, string.Empty); HtmlRenderRequested?.Invoke(this, string.Empty);
            DbPreviewPlaceholderVisibility = Visibility.Visible;
        }

        // =========================================================
        // ★修正：検索ロジックもシンプルに元通り
        // =========================================================
        partial void OnSelectedSectionChanged(SectionInfo? value)
        {
            if (value == null && string.IsNullOrWhiteSpace(SectionSearchText)) StatusText = null;
        }

        partial void OnSectionSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedSection = null; StatusText = null;
                if (Sections.Count != _searchContext.AllSections.Count)
                {
                    Sections = new ObservableCollection<SectionInfo>(_searchContext.AllSections);
                }
                return;
            }
            var exactMatch = _searchContext.AllSections.FirstOrDefault(s => s.DisplayName == value);
            if (exactMatch != null)
            {
                SelectedSection = exactMatch;
                return;
            }

            SelectedSection = null; StatusText = null;
            Sections = new ObservableCollection<SectionInfo>(_searchContext.AllSections.Where(s => s.DisplayName.StartsWith(value, StringComparison.OrdinalIgnoreCase)).ToList());
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
                CustomMessageBox.Show(errorMessage.Replace("{0}", errorText), errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
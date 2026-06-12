using BudgetManagement.Models;
using BudgetManagement.Services;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public partial class DepartmentBudgetViewModel : ObservableObject
    {
        private readonly DepartmentBudgetService _service;

        [ObservableProperty]
        private string _year = string.Empty;

        [ObservableProperty]
        private string _companyCode = string.Empty;

        [ObservableProperty]
        private string _companyName = string.Empty;

        [ObservableProperty]
        private string? _statusText;

        [ObservableProperty]
        private ObservableCollection<SectionInfo> _sections = new();

        [ObservableProperty]
        private SectionInfo? _selectedSection;

        [ObservableProperty]
        private string _sectionSearchText = string.Empty;

        public bool IsValidSectionName(string name)
        {
            return _allSections.Any(s => s.DisplayName == name);
        }

        private List<SectionInfo> _allSections = new();

        public DepartmentBudgetViewModel(DepartmentBudgetService service)
        {
            _service = service;
        }

        // =========================================================
        // 年度入力時
        // =========================================================
        [RelayCommand]
        private async Task HandleYearLostFocusAsync()
        {
            if (string.IsNullOrWhiteSpace(Year)) return;

            try
            {
                var companies = await _service.GetCompaniesByYearAsync(Year);
                var companyList = companies.ToList();

                if (companyList.Any())
                {
                    CompanyCode = companyList[0].CompanyCode;
                    CompanyName = companyList[0].CompanyName;

                    var sectionList = await _service.GetSectionsAsync(Year, CompanyCode);
                    _allSections = sectionList.ToList();

                    SectionSearchText = string.Empty;
                    SelectedSection = null;
                    StatusText = null;

                    Sections = new ObservableCollection<SectionInfo>(_allSections);
                }
                else
                {
                    CompanyCode = string.Empty;
                    CompanyName = "該当会社なし";
                    _allSections.Clear();
                    Sections = new ObservableCollection<SectionInfo>();
                    SectionSearchText = string.Empty;
                    SelectedSection = null;
                    StatusText = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"組織データの取得に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // 課の選択確定時
        // =========================================================
        [RelayCommand]
        private async Task HandleSectionChangedAsync()
        {
            if (SelectedSection == null || string.IsNullOrEmpty(CompanyCode)) return;

            try
            {
                bool isCompleted = await _service.GetManagementInputFlagsAsync(Year, CompanyCode, SelectedSection.SectionCode);
                StatusText = isCompleted ? "確定済" : "未確定";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"入力完了状態のチェックに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // クリアボタン
        // =========================================================
        [RelayCommand]
        private void ClearFields()
        {
            _allSections.Clear();
            Sections = new ObservableCollection<SectionInfo>();

            SectionSearchText = string.Empty;
            SelectedSection = null;
            StatusText = null;

            Year = string.Empty;
            CompanyCode = string.Empty;
            CompanyName = string.Empty;
        }

        // =========================================================
        // 状態テキストのクリア制御
        // =========================================================
        partial void OnSelectedSectionChanged(SectionInfo? value)
        {
            if (value == null && string.IsNullOrWhiteSpace(SectionSearchText))
            {
                StatusText = null;
            }
        }

        // =========================================================
        // 文字入力時の絞り込み
        // =========================================================
        partial void OnSectionSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedSection = null;
                StatusText = null;
                Sections = new ObservableCollection<SectionInfo>(_allSections);
                return;
            }

            var exactMatch = _allSections.FirstOrDefault(s => s.DisplayName == value);
            if (exactMatch != null)
            {
                SelectedSection = exactMatch;
                return;
            }

            SelectedSection = null;
            StatusText = null;

            var filtered = _allSections.Where(s => s.DisplayName.StartsWith(value, StringComparison.OrdinalIgnoreCase)).ToList();
            Sections = new ObservableCollection<SectionInfo>(filtered);
        }

        // =========================================================
        // Excel出力処理
        // =========================================================
        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            if (string.IsNullOrEmpty(Year) || SelectedSection == null)
            {
                MessageBox.Show("年度と課を選択してから実行してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var budgetDataList = await _service.GetBudgetDataForExcelAsync(Year, CompanyCode, SelectedSection.SectionCode);

                var budgetDataDict = budgetDataList.ToDictionary(
                    x => $"{x.AccountCode}_{x.SubAccountCode}",
                    x => x
                );

                string templatePath = @"C:\Templates\BudgetTemplate.xlsx"; // ※実際のテンプレートのパスに直してください
                string outputFolder = @"C:\Output\";                      // ※実際の出力先フォルダに直してください

                // ファイル名に日時を付与
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                // ファイル名："部門別予算"_[課コード]_[年月日時分秒].xlsx
                string newFileName = $"部門別予算_{SelectedSection.SectionCode}_{timestamp}.xlsx";
                string outputPath = Path.Combine(outputFolder, newFileName);

                File.Copy(templatePath, outputPath, true);

                using (var workbook = new XLWorkbook(outputPath))
                {
                    var worksheet = workbook.Worksheet(1);

                    worksheet.Cell("A1").Value = $"年度: {Year}";
                    worksheet.Cell("A2").Value = $"課: {SelectedSection.DisplayName}";

                    int startRow = 5;
                    int lastRow = worksheet.LastRowUsed().RowNumber();

                    for (int row = startRow; row <= lastRow; row++)
                    {
                        string accountCode = worksheet.Cell(row, 1).GetString();
                        string subAccountCode = worksheet.Cell(row, 2).GetString();

                        if (string.IsNullOrEmpty(accountCode)) continue;

                        string searchKey = $"{accountCode}_{subAccountCode}";

                        if (budgetDataDict.TryGetValue(searchKey, out var rowData))
                        {
                            worksheet.Cell(row, 3).Value = rowData.Month04;
                            worksheet.Cell(row, 4).Value = rowData.Month05;
                            worksheet.Cell(row, 5).Value = rowData.Month06;
                            worksheet.Cell(row, 6).Value = rowData.Month07;
                            worksheet.Cell(row, 7).Value = rowData.Month08;
                            worksheet.Cell(row, 8).Value = rowData.Month09;
                            worksheet.Cell(row, 9).Value = rowData.Month10;
                            worksheet.Cell(row, 10).Value = rowData.Month11;
                            worksheet.Cell(row, 11).Value = rowData.Month12;
                            worksheet.Cell(row, 12).Value = rowData.Month01;
                            worksheet.Cell(row, 13).Value = rowData.Month02;
                            worksheet.Cell(row, 14).Value = rowData.Month03;
                        }
                    }

                    workbook.Save();
                }

                MessageBox.Show($"Excelを出力しました。\n保存先: {outputPath}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"出力中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
using BudgetManagement.Models;
using BudgetManagement.Services;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
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
            // 既存のバリデーションを維持
            if (string.IsNullOrEmpty(Year) || SelectedSection == null)
            {
                MessageBox.Show("年度と課を選択してから実行してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 保存先のダイアログ確認
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "予算出力ファイルの保存先を選択してください",
                Filter = "Excel Worksheets (*.xlsx)|*.xlsx",
                // ご提案の「部門別予算_年度_課コード_課名称_タイムスタンプ.xlsx」フォーマット
                FileName = $"部門別予算_{Year}_{SelectedSection.SectionCode}_{SelectedSection.SectionName}_{timestamp}.xlsx"
            };


            if (dialog.ShowDialog() != true) return;

            string outputPath = dialog.FileName;
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "BudgetTemplate.xlsx");

            // ※StatusTextの書き換え処理を削除しました（画面の「確定済/未確定」を維持するため）

            try
            {
                var budgetDataList = await _service.GetBudgetDataForExcelAsync(Year, CompanyCode, SelectedSection.SectionCode);

                var budgetDataDict = budgetDataList.ToDictionary(
                    x => $"{x.AccountCode}_{x.SubAccountCode}",
                    x => x
                );

                // UIをフリーズさせないよう非同期タスク内でExcel操作
                await Task.Run(() =>
                {
                    File.Copy(templatePath, outputPath, true);

                    using (var workbook = new XLWorkbook(outputPath))
                    {
                        // 各シート共通で出力する値の準備
                        string exportDate = DateTime.Now.ToString("yyyy-MM-dd");
                        string reportTitle = "部門別経費予算";
                        string sectionDisplayName = SelectedSection.DisplayName;

                        // 1. Settingsシートの更新
                        if (workbook.TryGetWorksheet("Settings", out var settingsSheet))
                        {
                            settingsSheet.Cell("B1").Value = exportDate;   // 出力日
                            settingsSheet.Cell("B4").Value = reportTitle;  // タイトル
                            settingsSheet.Cell("B5").Value = Year;         // 年度
                            settingsSheet.Cell("B6").Value = $"{CompanyCode}：{CompanyName}"; // 会社情報
                            settingsSheet.Cell("B7").Value = sectionDisplayName; // 課情報
                            settingsSheet.Hide();   // シート非表示
                        }

                        // 2. 部門別経費予算シートの更新
                        if (workbook.TryGetWorksheet("部門別経費予算", out var worksheet))
                        {
                            //// オートシェイプ廃止に伴う、セルへの直接出力
                            //worksheet.Cell("G1").Value = reportTitle;        // SettingsのB4相当
                            //worksheet.Cell("I3").Value = Year;               // SettingsのB5相当
                            //worksheet.Cell("M3").Value = sectionDisplayName; // SettingsのB7相当

                            int lastRow = worksheet.LastRowUsed().RowNumber();

                            // 6行目以降の予算データ出力
                            for (int row = 6; row <= lastRow; row++)
                            {
                                string accountCode = worksheet.Cell(row, "H").GetString()?.Trim() ?? string.Empty;
                                string subAccountCode = worksheet.Cell(row, "I").GetString()?.Trim() ?? string.Empty;

                                if (string.IsNullOrEmpty(accountCode) || string.IsNullOrEmpty(subAccountCode)) continue;

                                string searchKey = $"{accountCode}_{subAccountCode}";

                                if (budgetDataDict.TryGetValue(searchKey, out var rowData))
                                {
                                    // 4月(M) ～ 9月(R)
                                    worksheet.Cell(row, "M").Value = rowData.Month04;
                                    worksheet.Cell(row, "N").Value = rowData.Month05;
                                    worksheet.Cell(row, "O").Value = rowData.Month06;
                                    worksheet.Cell(row, "P").Value = rowData.Month07;
                                    worksheet.Cell(row, "Q").Value = rowData.Month08;
                                    worksheet.Cell(row, "R").Value = rowData.Month09;

                                    // 10月(T) ～ 3月(Y)
                                    worksheet.Cell(row, "T").Value = rowData.Month10;
                                    worksheet.Cell(row, "U").Value = rowData.Month11;
                                    worksheet.Cell(row, "V").Value = rowData.Month12;
                                    worksheet.Cell(row, "W").Value = rowData.Month01;
                                    worksheet.Cell(row, "X").Value = rowData.Month02;
                                    worksheet.Cell(row, "Y").Value = rowData.Month03;
                                }
                            }
                        }
                        workbook.Save();
                    }
                });

                MessageBox.Show($"Excelを出力しました。\n保存先: {outputPath}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"出力中にエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
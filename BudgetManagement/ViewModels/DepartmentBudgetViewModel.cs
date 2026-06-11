using BudgetManagement.Models;
using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public partial class DepartmentBudgetViewModel : ObservableObject
    {
        private readonly DepartmentBudgetService _service;

        // 🌟 WPFのお節介な自動同期イベントの暴発を完全に防ぐガードフラグ
        private bool _isInternalUpdating = false;

        [ObservableProperty]
        private string _year = string.Empty;

        [ObservableProperty]
        private string _companyCode = string.Empty;

        [ObservableProperty]
        private string _companyName = string.Empty;

        [ObservableProperty]
        private string? _statusText;

        // 🌟 インスタンスの再生成(new)によるクラッシュを防ぐため、最初から生成して使い回す
        [ObservableProperty]
        private ObservableCollection<SectionInfo> _sections = new();

        [ObservableProperty]
        private SectionInfo? _selectedSection;

        [ObservableProperty]
        private string _sectionSearchText = string.Empty;

        private List<SectionInfo> _allSections = new();

        public DepartmentBudgetViewModel(DepartmentBudgetService service)
        {
            _service = service;
        }

        // =========================================================
        // ①・② 年度テキストボックスのカーソルが外れたとき（LostFocus）
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

                    // 🌟 ガードをかけて、安全に Clear & Add でリストを更新する
                    _isInternalUpdating = true;
                    try
                    {
                        SectionSearchText = string.Empty;
                        SelectedSection = null;
                        StatusText = null;

                        Sections.Clear();
                        foreach (var item in _allSections)
                        {
                            Sections.Add(item);
                        }
                    }
                    finally
                    {
                        _isInternalUpdating = false;
                    }
                }
                else
                {
                    CompanyCode = string.Empty;
                    CompanyName = "該当会社なし";

                    _isInternalUpdating = true;
                    try
                    {
                        _allSections.Clear();
                        Sections.Clear();
                        SectionSearchText = string.Empty;
                        SelectedSection = null;
                        StatusText = null;
                    }
                    finally
                    {
                        _isInternalUpdating = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"組織データの取得に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // ③ 課のコンボボックス選択が確定したとき
        // =========================================================
        [RelayCommand]
        private async Task HandleSectionChangedAsync()
        {
            if (_isInternalUpdating) return;
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
        // 🌟【最速版】クリアボタンを押したときの処理
        // =========================================================
        [RelayCommand]
        private void ClearFields()
        {
            _isInternalUpdating = true;
            try
            {
                _allSections.Clear();
                Sections.Clear();

                SectionSearchText = string.Empty;
                SelectedSection = null;
                StatusText = null;

                Year = string.Empty;
                CompanyCode = string.Empty;
                CompanyName = string.Empty;
            }
            finally
            {
                _isInternalUpdating = false;
            }
        }

        // =========================================================
        // 💡 入力値が変化するたびに自動で呼ばれるメソッド (課の選択変更)
        // =========================================================
        partial void OnSelectedSectionChanged(SectionInfo? value)
        {
            if (_isInternalUpdating) return;

            if (value == null && string.IsNullOrWhiteSpace(SectionSearchText))
            {
                StatusText = null;
            }
        }

        // =========================================================
        // 🌟 コンボボックスに文字が入力されるたびに自動で呼ばれる絞り込み処理
        // =========================================================
        partial void OnSectionSearchTextChanged(string value)
        {
            // 🌟 最重要ガード: プログラム側でデータを操作している時は、WPFの自動暴発イベントを完全に無視する
            if (_isInternalUpdating) return;

            // ① 全て削除された（空文字になった）場合
            if (string.IsNullOrWhiteSpace(value))
            {
                _isInternalUpdating = true;
                try
                {
                    SelectedSection = null;
                    StatusText = null;

                    Sections.Clear();
                    foreach (var item in _allSections)
                    {
                        Sections.Add(item);
                    }
                }
                finally
                {
                    _isInternalUpdating = false;
                }
                return;
            }

            // ② リストからマウスやEnterで選択され、表示名と入力文字が完全一致している場合はスキップ
            if (SelectedSection != null && value == SelectedSection.DisplayName)
            {
                return;
            }

            // ③ 部分一致での絞り込み処理
            var filtered = _allSections.Where(s => s.DisplayName.Contains(value)).ToList();

            _isInternalUpdating = true;
            try
            {
                Sections.Clear();
                foreach (var item in filtered)
                {
                    Sections.Add(item);
                }
            }
            finally
            {
                _isInternalUpdating = false;
            }
        }
    }
}
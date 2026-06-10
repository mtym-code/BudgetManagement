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

        // =========================================================
        // 画面と連動するプロパティ（自動的に変更通知が飛びます）
        // =========================================================

        [ObservableProperty]
        private string _year = string.Empty;

        [ObservableProperty]
        private string _companyCode = string.Empty;

        [ObservableProperty]
        private string _companyName = string.Empty;

        // 👇 修正: 初期状態では課が未選択なので、nullにして状態テキストを非表示にする
        [ObservableProperty]
        private string? _statusText;

        // 画面のコンボボックスに実際に表示されるリスト
        [ObservableProperty]
        private ObservableCollection<SectionInfo> _sections = new();

        // コンボボックスで現在選択されている課
        [ObservableProperty]
        private SectionInfo? _selectedSection;

        // 🌟【ポイント1】コンボボックスに打ち込まれた文字を受け取るプロパティ
        [ObservableProperty]
        private string _sectionSearchText = string.Empty;

        // 🌟【ポイント1】絞り込み用の「元の全データ」を保存しておくリスト
        private List<SectionInfo> _allSections = new();

        // =========================================================
        // コンストラクター
        // =========================================================
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

                    // 会社コードを使って課の一覧を取得
                    var sectionList = await _service.GetSectionsAsync(Year, CompanyCode);

                    // 🌟【ポイント2】取得したデータを「元の全データ」として裏側に保存
                    _allSections = sectionList.ToList();

                    // 画面表示用のコレクションにもセット
                    Sections = new ObservableCollection<SectionInfo>(_allSections);
                }
                else
                {
                    CompanyCode = string.Empty;
                    CompanyName = "該当会社なし";
                    Sections.Clear();
                    _allSections.Clear(); // 該当なしなら裏側データも消す
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
            if (SelectedSection == null || string.IsNullOrEmpty(CompanyCode)) return;

            try
            {
                bool isCompleted = await _service.GetManagementInputFlagsAsync(Year, CompanyCode, SelectedSection.SectionCode);

                if (isCompleted)
                {
                    StatusText = "確定済";
                }
                else
                {
                    StatusText = "未確定";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"入力完了状態のチェックに失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // ④ Excel出力ボタンを押したとき
        // =========================================================
        [RelayCommand]
        private async Task ExportExcelAsync()
        {
            if (SelectedSection == null)
            {
                MessageBox.Show("課を選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var budgetData = await _service.GetBudgetDataForExcelAsync(Year, CompanyCode, SelectedSection.SectionCode);
                var budgetList = budgetData.ToList();

                if (!budgetList.Any())
                {
                    MessageBox.Show("対象の予算データが存在しません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show($"{budgetList.Count} 件の予算データをExcel出力しました（ファイル作成処理は別途実装）", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel出力処理でエラーが発生しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // ⑤〜⑧ ファイル選択（Excel取り込み）〜DB登録・更新
        // =========================================================
        [RelayCommand]
        private async Task ImportExcelAndSaveAsync()
        {
            if (SelectedSection == null) return;

            try
            {
                string loginUser = "900017";

                await _service.UpsertStaffInputStatusAsync(
                    year: Year,
                    companyCode: CompanyCode,
                    sectionCode: SelectedSection.SectionCode,
                    staffHandlerCode: loginUser,
                    deleteFlag: "false",
                    createdBy: loginUser,
                    createdAt: DateTime.Now,
                    updatedBy: loginUser,
                    updatedAt: DateTime.Now
                );

                await _service.UpdateStaffInputStatusAsync(
                    staffHandlerCode: loginUser,
                    updatedBy: loginUser,
                    updatedAt: DateTime.Now,
                    year: Year,
                    companyCode: CompanyCode,
                    sectionCode: SelectedSection.SectionCode
                );

                await HandleSectionChangedAsync();

                MessageBox.Show("Excelデータの取り込みと完了フラグの更新が成功しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取り込み・登録処理に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // 💡 入力値が変化するたびに自動で呼ばれるメソッド (年度)
        // =========================================================
        partial void OnYearChanged(string? value)
        {
            // 年度が未入力、または3文字以下の場合
            if (string.IsNullOrWhiteSpace(value) || value.Length <= 3)
            {
                // 課のリストをクリアして未選択状態にする
                Sections.Clear();
                SelectedSection = null;
            }
        }

        // 👇 新規追加: 課の選択状態が変化するたびに自動で呼ばれるメソッド
        // =========================================================
        // 💡 入力値が変化するたびに自動で呼ばれるメソッド (課)
        // =========================================================
        partial void OnSelectedSectionChanged(SectionInfo? value)
        {
            // 課が未選択状態になった場合、状態ステータスも消去（非表示）する
            if (value == null)
            {
                StatusText = null;
            }
        }

        // =========================================================
        // 🌟【ポイント3】コンボボックスに文字が入力されるたびに自動で呼ばれる絞り込み処理
        // =========================================================
        partial void OnSectionSearchTextChanged(string value)
        {
            // 入力が空になった場合は、退避しておいたマスターから全件を表示し直す
            if (string.IsNullOrWhiteSpace(value))
            {
                Sections = new ObservableCollection<SectionInfo>(_allSections);
                return;
            }

            // リストからマウスで選択した際にも呼ばれてしまうため、
            // 選択アイテムの表示名と入力文字が完全一致している場合は絞り込みをスキップする
            if (SelectedSection != null && value == SelectedSection.DisplayName)
            {
                return;
            }

            // 入力された文字で、課コード（SectionCode）の先頭一致を検索
            var filtered = _allSections.Where(s => s.SectionCode.StartsWith(value)).ToList();

            // 絞り込んだ結果を画面のリストに再設定する
            Sections = new ObservableCollection<SectionInfo>(filtered);
        }
    }
}
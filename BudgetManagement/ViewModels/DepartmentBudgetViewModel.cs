using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BudgetManagement.Repositories;

namespace BudgetManagement.ViewModels
{
    public partial class DepartmentBudgetViewModel : ObservableObject
    {
        private readonly DepartmentBudgetRepository _repository;

        // =========================================================
        // 画面と連動するプロパティ（自動的に変更通知が飛びます）
        // =========================================================
        
        [ObservableProperty]
        private string _year = string.Empty;

        [ObservableProperty]
        private string _companyCode = string.Empty;

        [ObservableProperty]
        private string _companyName = string.Empty;

        [ObservableProperty]
        private string _statusText = "未確定";

        // 課のコンボボックス用の一覧
        [ObservableProperty]
        private ObservableCollection<SectionInfo> _sections = new();

        // コンボボックスで現在選択されている課
        [ObservableProperty]
        private SectionInfo? _selectedSection;

        // =========================================================
        // コンストラクター（App.xaml.cs から Repository が注入されます）
        // =========================================================
        public DepartmentBudgetViewModel(DepartmentBudgetRepository repository)
        {
            _repository = repository;
        }

        // =========================================================
        // ①・② 年度テキストボックスのカーソルが外れたとき（LostFocus）
        // =========================================================
        [RelayCommand]
        private void HandleYearLostFocus()
        {
            if (string.IsNullOrWhiteSpace(Year)) return;

            try
            {
                // ① 会社コードと名称を取得
                var companies = _repository.GetCompaniesByYear(Year);
                if (companies.Any())
                {
                    CompanyCode = companies[0].CompanyCode;
                    CompanyName = companies[0].CompanyName;

                    // ② 会社コードを使って課の一覧を取得
                    var sectionList = _repository.GetSections(Year, CompanyCode);
                    Sections = new ObservableCollection<SectionInfo>(sectionList);
                }
                else
                {
                    // 該当年度の組織データがない場合、クリア
                    CompanyCode = string.Empty;
                    CompanyName = "該当会社なし";
                    Sections.Clear();
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
        private void HandleSectionChanged()
        {
            if (SelectedSection == null || string.IsNullOrEmpty(CompanyCode)) return;

            try
            {
                // ③ 管理入力完了フラグを取得して状態をチェック
                var flags = _repository.GetManagementInputFlags(Year, CompanyCode, SelectedSection.SectionCode);
                
                // フラグの有無や値に応じて画面の状態テキストを切り替える
                if (flags.Contains("1"))
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
        private void ExportExcel()
        {
            if (SelectedSection == null)
            {
                MessageBox.Show("課を選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ④ 予算データをリポジトリから取得
                List<ExcelBudgetData> budgetData = _repository.GetBudgetDataForExcel(Year, CompanyCode, SelectedSection.SectionCode);

                if (!budgetData.Any())
                {
                    MessageBox.Show("対象の予算データが存在しません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // TODO: ここに実際のExcel出力ライブラリ（ClosedXML や EPPlus など）を使ったファイル書き出し処理を書く
                MessageBox.Show($"{budgetData.Count} 件の予算データをExcel出力しました（ファイル作成処理は別途実装）", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private async Task ImportExcelAndSave()
        {
            if (SelectedSection == null) return;

            // TODO: ここで本来はダイアログを開いてExcelファイルを読み込み、データのリストを作成します。
            // 今回は、取り込みロジックの枠組み（トランザクションとフラグ更新の流れ）を示します。

            try
            {
                // 例として、画面を動かしているログインユーザーの情報を仮定
                string loginUser = "900017"; 
                string programId = "BM330"; 

                // ⑤〜⑧の一連のDB更新処理（トランザクションを貼る場合を想定した疑似ロジック）
                // ※実際の一括Upsertはリポジトリの transaction 引数を利用してループ実行します。
                
                // 例: ⑦の担当入力完了フラグの登録
                _repository.UpsertStaffInputStatus(
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

                // ⑧ もし既存データの更新（Update）だった場合のフラグ更新
                _repository.UpdateStaffInputStatus(
                    staffHandlerCode: loginUser,
                    updatedBy: loginUser,
                    updatedAt: DateTime.Now,
                    year: Year,
                    companyCode: CompanyCode,
                    sectionCode: SelectedSection.SectionCode
                );

                // フラグが変わったため状態表示を再チェック
                HandleSectionChanged();

                MessageBox.Show("Excelデータの取り込みと完了フラグの更新が成功しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取り込み・登録処理に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
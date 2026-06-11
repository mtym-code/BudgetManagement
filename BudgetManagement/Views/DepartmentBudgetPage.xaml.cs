using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using BudgetManagement.ViewModels;

namespace BudgetManagement.Views
{
    public partial class DepartmentBudgetPage : Page
    {
        public DepartmentBudgetPage()
        {
            InitializeComponent();
            this.DataContext = App.ServiceProvider.GetRequiredService<DepartmentBudgetViewModel>();
        }

        // 🌟 ① 画面ロード時：TextBoxの文字変化を監視し、純粋な手入力で5文字を超えたら強制カットする
        private void SectionComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            comboBox.ApplyTemplate();
            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;

            if (textBox != null)
            {
                textBox.TextChanged += (s, args) =>
                {
                    // ユーザーが手入力中（フォーカスがある）で、かつ文字数が5文字を超えた場合
                    if (textBox.IsFocused && textBox.Text.Length > 5)
                    {
                        var vm = this.DataContext as DepartmentBudgetViewModel;
                        if (vm != null)
                        {
                            // 💡 リストから選択された長い名称（例：13516：東京情報...）がバインドされた時はカットせず通す
                            if (vm.Sections.Any(x => x.DisplayName == textBox.Text))
                            {
                                return;
                            }
                        }

                        // 純粋なキーボード手入力で5文字を超えた分だけをカット（無効化）する
                        int caret = textBox.SelectionStart;
                        textBox.Text = textBox.Text.Substring(0, 5);
                        textBox.SelectionStart = Math.Min(caret, 5); // カーソル位置を維持
                    }
                };
            }
        }

        // 🌟 ② 文字入力時：自動でリストを開く
        private void SectionComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || !comboBox.IsEditable) return;

            // 制御系キー（矢印キー、Enter、Tab、Escapeなど）の時はドロップダウンを勝手に開かないよう除外
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape)
            {
                return;
            }

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textBox != null && textBox.IsFocused)
            {
                // 1文字でも入力されていて、ドロップダウンが閉じていれば自動で開く
                if (!string.IsNullOrEmpty(textBox.Text) && !comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
        }

        // 🌟 ③ リストが開いた時：WPFの仕様による「お節介な文字全選択」を解除してカーソルを末尾に維持する
        private void SectionComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textBox != null && textBox.IsFocused)
            {
                // WPF内部の自動全選択処理（SelectAll）が完了した直後に割り込んで解除するため、
                // 一瞬だけ非同期（Dispatcher）で処理を遅らせて実行します
                Dispatcher.InvokeAsync(() =>
                {
                    if (textBox.SelectionLength > 0)
                    {
                        textBox.SelectionLength = 0;                  // 全選択を解除
                        textBox.SelectionStart = textBox.Text.Length; // カーソルを一番後ろへ移動
                    }
                }, DispatcherPriority.Input);
            }
        }
    }
}
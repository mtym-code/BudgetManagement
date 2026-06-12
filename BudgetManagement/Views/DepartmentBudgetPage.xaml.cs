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

        // 🌟 ① 入力制限 ＆ フリーズバグの完全回避
        private void SectionComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || !comboBox.IsEditable) return;

            var textBox = e.OriginalSource as TextBox;
            if (textBox == null || !textBox.IsFocused) return;

            var vm = this.DataContext as DepartmentBudgetViewModel;

            // 入力された文字が完全な課の名前（コード：名称）ではなくなった時
            if (vm != null && !vm.IsValidSectionName(comboBox.Text))
            {
                // 内部の選択状態をリセット
                if (comboBox.SelectedIndex != -1)
                {
                    comboBox.SelectedIndex = -1;
                }

                // 💡 【最大の解決策：リフレッシュ機構】💡
                // ドロップダウンが開いたまま裏側のリストデータが更新されると、WPFがフリーズします。
                // お客様が発見された「一度閉じれば直る」という挙動を利用し、
                // 文字が削除（変更）された時は、プログラム側で一瞬だけドロップダウンを強制的に閉じます。
                if (comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = false;
                }
            }

            // 削除操作のときは5文字制限をスルーする
            if (!e.Changes.Any(c => c.AddedLength > 0)) return;

            // 5文字制限の処理
            if (comboBox.Text.Length > 5)
            {
                if (vm != null && vm.IsValidSectionName(comboBox.Text)) return;

                int caret = textBox.SelectionStart;
                comboBox.Text = comboBox.Text.Substring(0, 5);
                textBox.SelectionStart = Math.Min(caret, 5);
            }
        }

        // 🌟 ② 自動でリストを開く（閉じたリストもここで再び開かれます）
        private void SectionComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null || !comboBox.IsEditable) return;

            // 矢印キーやEnterなどは無視
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape) return;

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textBox != null && textBox.IsFocused)
            {
                // 文字が入っていてドロップダウンが閉じていれば開く
                // ※上記のTextChangedで一瞬閉じられた場合も、キーを離した瞬間にここで再び綺麗に開きます！
                if (!string.IsNullOrEmpty(textBox.Text) && !comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
        }

        // 🌟 ③ 全選択バグを解除
        private void SectionComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox == null) return;

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            if (textBox != null && textBox.IsFocused)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (textBox.SelectionLength > 0)
                    {
                        textBox.SelectionLength = 0;
                        textBox.SelectionStart = textBox.Text.Length;
                    }
                }, DispatcherPriority.Input);
            }
        }
    }
}
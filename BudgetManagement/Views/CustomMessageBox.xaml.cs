using System.Windows;
using System.Windows.Media;

namespace BudgetManagement.Views
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string title, MessageBoxButton button, MessageBoxImage image)
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
            this.Title = title;
            MessageText.Text = message;

            if (button == MessageBoxButton.YesNo)
            {
                OkButton.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
            }
            else
            {
                OkButton.Visibility = Visibility.Visible;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
            }

            // ★追加：メッセージの種類によって色とアイコンを変更する
            SetTheme(image);
        }

        private void SetTheme(MessageBoxImage image)
        {
            // デフォルトは「情報」の青色とiマーク
            string iconData = "M12,2A10,10 0 1,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 1,1 4,12A8,8 0 0,1 12,4M11,7H13V9H11V7M11,11H13V17H11V11Z";
            string colorHex = "#3B82F6";

            if (image == MessageBoxImage.Warning)
            {
                // 「警告」の場合はオレンジ色と三角の！マーク
                iconData = "M13,14H11V10H13M13,18H11V16H13M1,21H23L12,2L1,21Z";
                colorHex = "#F59E0B";
            }
            else if (image == MessageBoxImage.Error)
            {
                // 「エラー」の場合は赤色とバツマーク
                iconData = "M12,2C6.47,2 2,6.47 2,12C2,17.53 6.47,22 12,22C17.53,22 22,17.53 22,12C22,6.47 17.53,2 12,2M17,15.59L15.59,17L12,13.41L8.41,17L7,15.59L10.59,12L7,8.41L8.41,7L12,10.59L15.59,7L17,8.41L13.41,12L17,15.59Z";
                colorHex = "#EF4444";
            }

            IconPath.Data = Geometry.Parse(iconData);
            IconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            TopColorBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            this.DialogResult = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            this.DialogResult = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            this.DialogResult = false;
        }

        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage image)
        {
            var dialog = new CustomMessageBox(message, title, button, image);
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}